## linx.core.reporthandlerwpf

This plugin supports two LINX print paths with Scryber templates:

1. Remote report mode (`UseRemoteReport = true`)
2. Direct rendering mode (`UseRemoteReport = false`)

In direct rendering mode, LINX font/dataset selection (previously driven by FlowDoc `AggregateGroup`) must be provided explicitly in Scryber output.

## FlowDoc XAML -> Scryber HTML/Handlebars

### Mapping overview

FlowDoc inline values:

- `InlineDocumentValue VBContent="..."` -> `{{vb.Get('...')}}`
- `InlineACMethodValue VBContent="..."` -> `{{vb.Get('...')}}` (or another handlebars expression)
- `AggregateGroup="9 High Caps"` -> Scryber class token with aggregate marker:
	- `class="linx-ag-9-high-caps"` or `class="ag-9-high-caps"`
- `XPos="150"` -> CSS positioning (for example `left: 150pt;` on positioned element)

Notes:

- Aggregate tokens are decoded by replacing `-`/`_` with spaces and matching dataset names case-insensitively.
- If no aggregate group is found, the first LINX dataset is used as fallback.

## Your example conversion

Original FlowDoc snippet:

```xml
<?xml version="1.0" encoding="utf-16"?>
<VBFlowDocument CodePage="20127" Custom01="35 STD LIN" Custom02="OneLine" CustomInt01="10" CustomInt02="2" CustomInt03="23" FontFamily="Calibri" ColumnWidth="367" PageWidth="420" PageHeight="700" PagePadding="20,20,20,20" AllowDrop="True" av:NumberSubstitution.CultureSource="User" xmlns="http://www.iplus-framework.com/xaml" xmlns:av="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:vbr="http://www.iplus-framework.com/report/xaml" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
	<av:Paragraph TextAlignment="Left">
		<vbr:InlineDocumentValue VBContent="CurrentFacilityCharge\Material\Comment" AggregateGroup="9 High Caps" FontWeight="Bold" FontSize="12" xml:space="preserve"></vbr:InlineDocumentValue>
		<vbr:InlineDocumentValue VBContent="CurrentFacilityCharge\FacilityLot\LotNo" AggregateGroup="9 High Caps" XPos="150" FontWeight="Bold" FontSize="12" xml:space="preserve"></vbr:InlineDocumentValue>
	</av:Paragraph>
</VBFlowDocument>
```

Equivalent Scryber template:

```html
<!doctype html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
	<meta charset="utf-8" />
	<style>
		@page {
			size: 420pt 700pt;
			margin: 20pt;
		}

		body {
			font-family: Calibri, Arial, sans-serif;
			font-size: 12pt;
			margin: 0;
		}

		.line {
			position: relative;
			height: 16pt;
			width: 367pt;
		}

		.linx-ag-9-high-caps {
			font-weight: bold;
			font-size: 12pt;
		}

		.comment {
			position: absolute;
			left: 0pt;
			top: 0pt;
		}

		.lot {
			position: absolute;
			left: 150pt;
			top: 0pt;
		}
	</style>
</head>
<body>
	<div class="line">
		<span class="linx-ag-9-high-caps comment">{{vb.Get('CurrentFacilityCharge/Material/Comment')}}</span>
		<span class="linx-ag-9-high-caps lot">{{vb.Get('CurrentFacilityCharge/FacilityLot/LotNo')}}</span>
	</div>
</body>
</html>
```

## Aggregate group conventions for direct rendering

Preferred (first-class metadata):

```html
<span data-linx-aggregate-group="9 High Caps">{{vb.Get('path/to/value')}}</span>
```

Preferred (CSS class driven):

```html
<span class="linx-ag-9-high-caps">{{vb.Get('path/to/value')}}</span>
```

Also supported (text marker fallback):

```html
<span>[ag:9 High Caps]{{vb.Get('path/to/value')}}</span>
<span>{{ag:9 High Caps}}{{vb.Get('path/to/value')}}</span>
```

Markers are stripped from printed text and only used for dataset/font selection.

## Job-level metadata (Scryber data-* attributes)

You can define LINX job settings directly in the template with `data-*` attributes. Place them on the root document element or a component with `id="linx-root"` / `id="linx-settings"`.

```html
<body id="linx-root"
			data-linx-raster-name="35 STD LIN"
			data-linx-one-line="true"
			data-linx-character-width="10"
			data-linx-inter-char-space="2"
			data-linx-field-height-drop="23">
		...
</body>
```

Supported keys:

- `data-linx-raster-name`
- `data-linx-one-line`
- `data-linx-character-width`
- `data-linx-inter-char-space`
- `data-linx-field-height-drop`
- `data-linx-aggregate-group` (per element)

## Important differences vs FlowDoc

- FlowDoc-only attributes like `Custom01`, `Custom02`, `CustomInt01`, `CustomInt02`, `CustomInt03` are not read from Scryber HTML automatically, but equivalent values can now be supplied with LINX `data-*` metadata attributes.
- For Scryber mode, keep printer/runtime configuration (codepage, communication setup, remote/direct mode) in LINX printer settings.
- Use `data-linx-aggregate-group` (or class tokens / fallback markers) to control LINX dataset/font selection per rendered segment.
