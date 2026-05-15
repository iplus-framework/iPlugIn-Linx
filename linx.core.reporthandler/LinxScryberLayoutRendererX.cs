using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Scryber;
using Scryber.Components;
using Scryber.PDF;
using Scryber.PDF.Layout;
using ScryberDocument = Scryber.Components.Document;

namespace linx.core.reporthandler
{
    /// <summary>
    /// First-pass Scryber layout renderer for LINX output.
    /// It extracts line text and keeps best-effort metadata for aggregate-group and position.
    /// </summary>
    public sealed class LinxScryberLayoutRendererX : IDocumentLayoutRenderer
    {
        private readonly Encoding _encoding;
        private readonly List<LinxScryberRenderedLineX> _lines = new List<LinxScryberRenderedLineX>();
        private LinxScryberJobMetadataX _jobMetadata = new LinxScryberJobMetadataX();

        public IReadOnlyList<LinxScryberRenderedLineX> Lines => _lines;
        public LinxScryberJobMetadataX JobMetadata => _jobMetadata;

        public LinxScryberLayoutRendererX(Encoding encoding)
        {
            _encoding = encoding ?? Encoding.ASCII;
        }

        public void Render(ScryberDocument document, PDFLayoutDocument layout, PDFLayoutContext layoutContext, Stream output)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            _lines.Clear();
            _jobMetadata = ExtractJobMetadata(document);

            using (MemoryStream buffer = new MemoryStream())
            {
                for (int i = 0; i < layout.AllPages.Count; i++)
                {
                    PDFLayoutPage page = layout.AllPages[i];
                    if (page == null)
                        continue;

                    if (i > 0)
                        WriteLineBreak(buffer);

                    WriteBlock(buffer, page.HeaderBlock);
                    WriteBlock(buffer, page.ContentBlock);
                    WriteBlock(buffer, page.FooterBlock);
                }

                buffer.Position = 0;
                buffer.CopyTo(output);
            }
        }

        private void WriteBlock(Stream output, PDFLayoutBlock block)
        {
            if (block == null)
                return;

            if (block.Columns != null)
            {
                foreach (PDFLayoutRegion column in block.Columns)
                    WriteRegion(output, column);
            }

            if (block.HasPositionedRegions && block.PositionedRegions != null)
            {
                foreach (PDFLayoutRegion positioned in block.PositionedRegions)
                    WriteRegion(output, positioned);
            }
        }

        private void WriteRegion(Stream output, PDFLayoutRegion region)
        {
            if (region == null || region.Contents == null)
                return;

            foreach (PDFLayoutItem item in region.Contents)
            {
                if (item is PDFLayoutLine line)
                {
                    WriteLine(output, line);
                }
                else if (item is PDFLayoutBlock block)
                {
                    WriteBlock(output, block);
                }
            }
        }

        private void WriteLine(Stream output, PDFLayoutLine line)
        {
            if (line?.Runs == null || line.Runs.Count == 0)
                return;

            foreach (LinxScryberRenderedLineX rendered in ExtractRenderedLines(line))
            {
                if (string.IsNullOrWhiteSpace(rendered.Text))
                    continue;

                _lines.Add(rendered);

                byte[] bytes = _encoding.GetBytes(rendered.Text.TrimEnd());
                output.Write(bytes, 0, bytes.Length);
                WriteLineBreak(output);
            }
        }

        private static List<LinxScryberRenderedLineX> ExtractRenderedLines(PDFLayoutLine line)
        {
            List<LinxScryberRenderedLineX> results = new List<LinxScryberRenderedLineX>();

            int xPos = Math.Max(0, (int)Math.Round(line.OffsetX.PointsValue));
            int yPos = Math.Max(0, (int)Math.Round(line.OffsetY.PointsValue));

            StringBuilder builder = new StringBuilder();
            string currentGroup = null;

            foreach (PDFLayoutRun run in line.Runs)
            {
                string runText = ExtractRunText(run);
                if (string.IsNullOrEmpty(runText))
                    continue;

                string runGroup = ExtractAggregateGroup(run);

                if (builder.Length == 0)
                {
                    currentGroup = runGroup;
                }
                else if (!string.Equals(currentGroup ?? string.Empty, runGroup ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    AddRenderedLine(results, builder.ToString(), currentGroup, xPos, yPos);
                    builder.Clear();
                    currentGroup = runGroup;
                }

                builder.Append(runText);
            }

            if (builder.Length > 0)
                AddRenderedLine(results, builder.ToString(), currentGroup, xPos, yPos);

            return results;
        }

        private static string ExtractRunText(PDFLayoutRun run)
        {
            if (run is PDFTextRunCharacter full)
                return full.Characters ?? string.Empty;

            if (run is PDFTextRunPartialCharacter partial)
            {
                if (string.IsNullOrEmpty(partial.Characters))
                    return string.Empty;

                int start = Math.Max(0, partial.StartOffset);
                int count = Math.Max(0, partial.CharacterCount);
                if (start >= partial.Characters.Length || count == 0)
                    return string.Empty;

                if ((start + count) > partial.Characters.Length)
                    count = partial.Characters.Length - start;

                return partial.Characters.Substring(start, count);
            }

            return string.Empty;
        }

        private static string ExtractAggregateGroup(PDFLayoutRun run)
        {
            if (!(run?.Owner is Component component))
                return null;

            string metadataGroup;
            if (TryGetComponentMetadata(component, "linx-aggregate-group", out metadataGroup) ||
                TryGetComponentMetadata(component, "aggregate-group", out metadataGroup))
                return metadataGroup;

            string classes = component.StyleClass;
            if (string.IsNullOrWhiteSpace(classes))
                return null;

            string[] tokens = classes.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string ag = tokens.FirstOrDefault(t => t.StartsWith("ag-", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(ag))
                return DecodeAggregateGroupToken(ag.Substring(3));

            string linxAg = tokens.FirstOrDefault(t => t.StartsWith("linx-ag-", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(linxAg))
                return DecodeAggregateGroupToken(linxAg.Substring("linx-ag-".Length));

            return null;
        }

        private static string DecodeAggregateGroupToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            return token.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        private static bool TryGetComponentMetadata(Component component, string key, out string value)
        {
            value = null;
            if (component == null || string.IsNullOrWhiteSpace(key))
                return false;

            if (!component.TryGetMetadata(key, out value))
                return false;

            value = value?.Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryGetIntMetadata(Component component, out int value, params string[] keys)
        {
            value = 0;
            if (component == null || keys == null)
                return false;

            foreach (string key in keys)
            {
                string raw;
                if (!TryGetComponentMetadata(component, key, out raw))
                    continue;

                int parsed;
                if (int.TryParse(raw, out parsed))
                {
                    value = parsed;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetBoolMetadata(Component component, out bool value, params string[] keys)
        {
            value = false;
            if (component == null || keys == null)
                return false;

            foreach (string key in keys)
            {
                string raw;
                if (!TryGetComponentMetadata(component, key, out raw))
                    continue;

                bool parsedBool;
                if (bool.TryParse(raw, out parsedBool))
                {
                    value = parsedBool;
                    return true;
                }

                if (string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(raw, "oneline", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(raw, "no", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetStringMetadata(Component component, out string value, params string[] keys)
        {
            value = null;
            if (component == null || keys == null)
                return false;

            foreach (string key in keys)
            {
                if (TryGetComponentMetadata(component, key, out value))
                    return true;
            }

            return false;
        }

        private LinxScryberJobMetadataX ExtractJobMetadata(ScryberDocument document)
        {
            LinxScryberJobMetadataX result = new LinxScryberJobMetadataX();
            if (document == null)
                return result;

            ApplyJobMetadataFromComponent(document, result);
            ApplyJobMetadataById(document, "linx-root", result);
            ApplyJobMetadataById(document, "linx-settings", result);

            return result;
        }

        private static void ApplyJobMetadataById(ScryberDocument document, string id, LinxScryberJobMetadataX target)
        {
            if (document == null || string.IsNullOrWhiteSpace(id) || target == null)
                return;

            Component component = document.FindAComponentById(id) as Component;
            if (component == null)
                return;

            ApplyJobMetadataFromComponent(component, target);
        }

        private static void ApplyJobMetadataFromComponent(Component component, LinxScryberJobMetadataX target)
        {
            if (component == null || target == null)
                return;

            int intValue;
            bool boolValue;
            string stringValue;

            if (TryGetIntMetadata(component, out intValue, "linx-character-width", "character-width"))
                target.CharacterWidth = intValue;

            if (TryGetIntMetadata(component, out intValue, "linx-inter-char-space", "inter-char-space"))
                target.InterCharSpace = intValue;

            if (TryGetIntMetadata(component, out intValue, "linx-field-height-drop", "field-height-drop"))
                target.FieldHeightDrop = intValue;

            if (TryGetStringMetadata(component, out stringValue, "linx-raster-name", "raster-name"))
                target.RasterName = stringValue;

            if (TryGetBoolMetadata(component, out boolValue, "linx-one-line", "one-line"))
                target.IsOneLine = boolValue;
        }

        private static void AddRenderedLine(List<LinxScryberRenderedLineX> results, string text, string aggregateGroup, int xPos, int yPos)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            string trimmed = text.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            string markerGroup;
            if (TryReadGroupMarker(ref trimmed, out markerGroup))
                aggregateGroup = markerGroup;

            results.Add(new LinxScryberRenderedLineX
            {
                Text = trimmed,
                AggregateGroup = aggregateGroup,
                XPos = xPos,
                YPos = yPos,
            });
        }

        private static bool TryReadGroupMarker(ref string text, out string aggregateGroup)
        {
            aggregateGroup = null;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string value = text.TrimStart();

            if (value.StartsWith("[ag:", StringComparison.OrdinalIgnoreCase))
            {
                int end = value.IndexOf(']');
                if (end > 4)
                {
                    aggregateGroup = value.Substring(4, end - 4).Trim();
                    text = value.Substring(end + 1).TrimStart();
                    return !string.IsNullOrWhiteSpace(aggregateGroup);
                }
            }

            if (value.StartsWith("{{ag:", StringComparison.OrdinalIgnoreCase))
            {
                int end = value.IndexOf("}}", StringComparison.Ordinal);
                if (end > 5)
                {
                    aggregateGroup = value.Substring(5, end - 5).Trim();
                    text = value.Substring(end + 2).TrimStart();
                    return !string.IsNullOrWhiteSpace(aggregateGroup);
                }
            }

            return false;
        }

        private static void WriteLineBreak(Stream output)
        {
            byte[] bytes = new byte[] { 0x0A };
            output.Write(bytes, 0, bytes.Length);
        }
    }

    public sealed class LinxScryberRenderedLineX
    {
        public string Text { get; set; }
        public string AggregateGroup { get; set; }
        public int XPos { get; set; }
        public int YPos { get; set; }
    }

    public sealed class LinxScryberJobMetadataX
    {
        public int? CharacterWidth { get; set; }
        public int? InterCharSpace { get; set; }
        public int? FieldHeightDrop { get; set; }
        public string RasterName { get; set; }
        public bool? IsOneLine { get; set; }
    }
}
