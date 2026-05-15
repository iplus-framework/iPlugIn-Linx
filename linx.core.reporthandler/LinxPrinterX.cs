using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.reporthandler;
using System;
using System.Text;

namespace linx.core.reporthandler
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'LinxPrinterX'}de{'LinxPrinterX'}", Global.ACKinds.TPABGModule, Global.ACStorableTypes.Required, false, false)]
    public class LinxPrinterX : ACPrintServerBase
    {
        private ACPropertyConfigValue<bool> _UseScryberLayoutRenderer;

        public LinxPrinterX(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _UseScryberLayoutRenderer = new ACPropertyConfigValue<bool>(this, nameof(UseScryberLayoutRenderer), true);
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            if (!base.ACInit(startChildMode))
                return false;

            _ = UseScryberLayoutRenderer;
            return true;
        }

        [ACPropertyInfo(true, 200, DefaultValue = false)]
        public bool UseRemoteReport { get; set; }

        [ACPropertyConfig("en{'Use Scryber layout renderer'}de{'Scryber-Layout-Renderer verwenden'}")]
        public bool UseScryberLayoutRenderer
        {
            get => _UseScryberLayoutRenderer.ValueT;
            set => _UseScryberLayoutRenderer.ValueT = value;
        }

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
                LinxScryberLayoutRendererX renderer = new LinxScryberLayoutRendererX(encoding);
                byte[] payload = ScryberReportEngine.RenderWithLayoutRenderer(template, reportData, renderer);
                if (payload == null || payload.Length == 0)
                    return null;

                return new PrintJob
                {
                    Name = aCClassDesign.ACIdentifier,
                    Main = payload,
                    Encoding = encoding,
                    ColumnMultiplier = 1,
                    ColumnDivisor = 1,
                };
            }
            catch (Exception ex)
            {
                Messages.LogException(GetACUrl(), nameof(TryCreateScryberCustomPrintJob), ex);
                return null;
            }
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

        protected override PrintJob OnDoPrint(ACClassDesign aCClassDesign, int codePage, ReportData reportData)
        {
            return null;
        }
    }
}
