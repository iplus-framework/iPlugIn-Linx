using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.reporthandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace linx.core.reporthandler
{
    public partial class LinxPrinterX
    {
        protected override PrintJob TryCreateScryberCustomPrintJob(ACClassDesign aCClassDesign, ReportData reportData)
        {
            if (!UseScryberLayoutRenderer || aCClassDesign == null || reportData == null)
                return null;

            string template = GetScryberTemplate(aCClassDesign);
            if (string.IsNullOrWhiteSpace(template))
                return null;

            try
            {
                Encoding encoding = ResolveEncoding();
                LinxScryberLayoutRendererX renderer = new LinxScryberLayoutRendererX(encoding, writePayload: false);
                _ = ScryberReportEngine.RenderWithLayoutRenderer(template, reportData, renderer);
                if (renderer.Lines == null || renderer.Lines.Count == 0)
                {
                    return null;
                }

                LinxPrintJobX linxPrintJob = new LinxPrintJobX
                {
                    Name = aCClassDesign.ACIdentifier,
                    Encoding = encoding,
                    ColumnMultiplier = 1,
                    ColumnDivisor = 1,
                };

                ApplyScryberJobMetadata(linxPrintJob, renderer.JobMetadata);

                if (UseRemoteReport)
                {
                    BuildScryberRemoteJob(linxPrintJob, renderer);
                }
                else
                {
                    BuildScryberDirectJob(linxPrintJob, renderer);
                }

                return linxPrintJob;
            }
            catch (Exception ex)
            {
                Messages.LogException(GetACUrl(), nameof(TryCreateScryberCustomPrintJob), ex);
                return null;
            }
        }


        private void BuildScryberRemoteJob(LinxPrintJobX linxPrintJob, LinxScryberLayoutRendererX renderer)
        {
            AddPrintMessageToJob(linxPrintJob);

            List<string> lines = GetScryberTextLines(renderer);
            if (lines.Count == 0)
                lines.Add(string.Empty);

            for (int i = 0; i < lines.Count; i++)
            {
                AddTextValueForRemoteField(linxPrintJob, $"ScryberLine{i + 1}", lines[i]);
            }

            int dataLength = linxPrintJob.RemoteFieldValues.Sum(c => c.Length);
            byte[] dataLengthBy = BitConverter.GetBytes(dataLength);
            List<byte[]> dataArr = linxPrintJob.RemoteFieldValues.ToList();
            dataArr.Insert(0, dataLengthBy);
            byte[] inputData = LinxHelper.Combine(dataArr);

            byte[] data = GetData(LinxASCIControlCharacterEnum.GS, inputData);
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.PrintRemote, data));

            AddPrintCommandToJob(linxPrintJob);
        }

        private void BuildScryberDirectJob(LinxPrintJobX linxPrintJob, LinxScryberLayoutRendererX renderer)
        {
            AddDeleteReportToJob(linxPrintJob);

            List<LinxScryberRenderedLineX> lines = renderer.Lines?.ToList() ?? new List<LinxScryberRenderedLineX>();
            string defaultAggregateGroup = renderer.JobMetadata?.DefaultAggregateGroup;

            int fallbackY = 0;
            foreach (LinxScryberRenderedLineX line in lines)
            {
                if (string.IsNullOrWhiteSpace(line?.Text))
                    continue;

                string aggregateGroup = ResolveAggregateGroup(line.AggregateGroup, defaultAggregateGroup);
                LinxDataSetData dataSet = ResolveDataSet(aggregateGroup);

                LinxFieldRenderOptionsX fieldOptions = new LinxFieldRenderOptionsX
                {
                    AggregateGroup = aggregateGroup,
                    XPos = Math.Max(0, line.XPos),
                    YPos = line.YPos > 0 ? line.YPos : fallbackY,
                    CustomInt01 = linxPrintJob.CharacterWidth,
                    CustomInt02 = linxPrintJob.InterCharSpace,
                    CustomInt03 = linxPrintJob.FieldHeightDrop,
                };

                if (line.IsBarcode)
                    AddBarcodeValueToPrintMessage(linxPrintJob, fieldOptions, aggregateGroup, line.Text);
                else
                    AddTextValueToPrintMessage(linxPrintJob, fieldOptions, aggregateGroup, line.Text);

                int lineStep = Math.Max(1, dataSet?.Height ?? (fieldOptions.CustomInt03 > 0 ? fieldOptions.CustomInt03 : 10));
                fallbackY = fieldOptions.YPos + lineStep;
            }

            int msgLengthInBytes = linxPrintJob.LinxFields.Sum(c => BitConverter.ToInt16(c.Header.FieldLengthInBytes, 0)) + LinxMessageHeader.DefaultHeaderLength;
            int msgLengthInRasters = linxPrintJob.LinxFields.Sum(c => BitConverter.ToInt16(c.Header.FieldLengthInRasters, 0)) + LinxMessageHeader.DefaultHeaderLength;
            LinxMessageHeader linxMessageHeader = GetLinxMessageHeader(linxPrintJob.Name, linxPrintJob.RasterName, 1, (short)msgLengthInBytes, (short)msgLengthInRasters);
            List<byte[]> headerBytes = linxMessageHeader.GetBytes();

            List<byte[]> fieldData = new List<byte[]>();
            foreach (LinxField linxField in linxPrintJob.LinxFields)
            {
                List<byte[]> fieldBytes = linxField.GetBytes();
                fieldData.AddRange(fieldBytes);
            }

            List<byte[]> downloadData = new List<byte[]>();
            downloadData.AddRange(headerBytes);
            downloadData.AddRange(fieldData);

            byte[] data = GetData(LinxASCIControlCharacterEnum.EM, downloadData.ToArray().SelectMany(c => c).ToArray());
            linxPrintJob.PacketsForPrint.Add(new Telegram(LinxPrintJobTypeEnum.DownloadReport, data));

            AddPrintMessageToJob(linxPrintJob);
            AddPrintCommandToJob(linxPrintJob);
        }

        private List<string> GetScryberTextLines(LinxScryberLayoutRendererX renderer)
        {
            return renderer.Lines?
                .Select(c => c?.Text?.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList() ?? new List<string>();
        }

        private string ResolveAggregateGroup(string aggregateGroup, string defaultAggregateGroup)
        {
            if (!string.IsNullOrWhiteSpace(aggregateGroup))
                return aggregateGroup;

            if (!string.IsNullOrWhiteSpace(defaultAggregateGroup))
                return defaultAggregateGroup;

            LinxDataSetData first = DataSets?.FirstOrDefault();
            return first?.DataSetName;
        }

        private LinxDataSetData ResolveDataSet(string aggregateGroup)
        {
            if (DataSets == null || DataSets.Count == 0)
                return null;

            LinxDataSetData dataSet = DataSets.FirstOrDefault(c => string.Equals(c.DataSetName, aggregateGroup, StringComparison.OrdinalIgnoreCase));
            return dataSet ?? DataSets.FirstOrDefault();
        }

        private Encoding ResolveEncoding()
        {
            Encoding encoder = Encoding.Unicode;
            if (CodePage <= 0)
                return encoder;

            try
            {
                return Encoding.GetEncoding(CodePage);
            }
            catch (Exception ex)
            {
                Messages.LogException(GetACUrl(), nameof(ResolveEncoding), ex);
                return encoder;
            }
        }

        private static void ApplyScryberJobMetadata(LinxPrintJobX linxPrintJob, LinxScryberJobMetadataX metadata)
        {
            if (linxPrintJob == null || metadata == null)
                return;

            if (metadata.CharacterWidth.HasValue)
                linxPrintJob.CharacterWidthOverride = metadata.CharacterWidth.Value;

            if (metadata.InterCharSpace.HasValue)
                linxPrintJob.InterCharSpaceOverride = metadata.InterCharSpace.Value;

            if (metadata.FieldHeightDrop.HasValue)
                linxPrintJob.FieldHeightDropOverride = metadata.FieldHeightDrop.Value;

            if (metadata.IsOneLine.HasValue)
                linxPrintJob.IsOneLineOverride = metadata.IsOneLine.Value;

            if (!string.IsNullOrWhiteSpace(metadata.RasterName))
                linxPrintJob.RasterNameOverride = metadata.RasterName;
        }
    }
}
