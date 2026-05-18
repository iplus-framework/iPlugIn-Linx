using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.reporthandler;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace linx.core.reporthandler
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'LinxPrinterX'}de{'LinxPrinterX'}", Global.ACKinds.TPABGModule, Global.ACStorableTypes.Required, false, false)]
    public partial class LinxPrinterX : ACPrintServerBase, ILinxPrinter
    {
        private ACPropertyConfigValue<bool> _UseScryberLayoutRenderer;
        private LinxPrinterXShared _shared;

        #region ctor's
        public LinxPrinterX(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _UseScryberLayoutRenderer = new ACPropertyConfigValue<bool>(this, nameof(UseScryberLayoutRenderer), true);
            _shared = new LinxPrinterXShared(this);
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            if (!base.ACInit(startChildMode))
                return false;

            _ = IPAddress;
            _ = UseScryberLayoutRenderer;
            _shared.ACInit(startChildMode);

            return true;
        }

        public override bool ACPostInit()
        {
            bool basePostInit = base.ACPostInit();
            _shared.ACPostInit();
            return basePostInit;
        }

        public override async Task<bool> ACDeInit(bool deleteACClassTask = false)
        {
            bool acDeinit = await base.ACDeInit(deleteACClassTask);
            _shared.ACDeInit(deleteACClassTask);
            return acDeinit;
        }
        #endregion

        #region Settings

        [ACPropertyInfo(true, 200, DefaultValue = false)]
        public bool UseRemoteReport { get; set; }

        [ACPropertyConfig("en{'Use Scryber layout renderer'}de{'Scryber-Layout-Renderer verwenden'}")]
        public bool UseScryberLayoutRenderer
        {
            get => _UseScryberLayoutRenderer.ValueT;
            set => _UseScryberLayoutRenderer.ValueT = value;
        }

        #endregion

        
        #region Broadcast-Properties

        [ACPropertyBindingSource(730, "Error", "en{'Printer (complete) status'}de{'Druckerstatus (abgeschlossen).'}", "", false, false)]
        public IACContainerTNet<LinxPrinterCompleteStatusResponse> PrinterCompleteStatus
        {
            get;
            set;
        }

        #endregion

        #region Interaction Methods

        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Check status'}de{'Status überprüfen'}", 200, true)]
        public void CheckStatus()
        {
            LinxPrintJobX linxPrintJob = new LinxPrintJobX();
            AddCheckStatusToJob(linxPrintJob);
            EnqueueJob(linxPrintJob, nameof(StartPrint));
        }

        public bool IsEnabledCheckStatus()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Start Print'}de{'Drucker Start'}", 201, true)]
        public void StartPrint()
        {
            LinxPrintJobX linxPrintJob = new LinxPrintJobX();
            AddPrintCommandToJob(linxPrintJob);
            EnqueueJob(linxPrintJob, nameof(StartPrint));
        }

        public bool IsEnabledStartPrint()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Stop Print'}de{'Drucker Stopp'}", 202, true)]
        public void StopPrint()
        {
            LinxPrintJobX linxPrintJob = new LinxPrintJobX();
            AddPrintCommandToJob(linxPrintJob, true);
            EnqueueJob(linxPrintJob, nameof(StopPrint));
        }

        public bool IsEnabledStopPrint()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Switch on printer'}de{'Drucker einschalten'}", 203, true)]
        public void StartJet()
        {
            LinxPrintJobX linxPrintJob = new LinxPrintJobX();
            AddJetCommandToJob(linxPrintJob);
            EnqueueJob(linxPrintJob, nameof(StartJet));
        }

        public bool IsEnabledStartJet()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Switch of printer'}de{'Drucker ausschalten'}", 204, true)]
        public void StopJet()
        {
            LinxPrintJobX linxPrintJob = new LinxPrintJobX();
            AddJetCommandToJob(linxPrintJob, true);
            EnqueueJob(linxPrintJob, nameof(StopJet));

        }

        public bool IsEnabledStopJet()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }

        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Read Raster data'}de{'Lese Rasterdaten'}", 205, true)]
        public void GetRasterData()
        {
            LinxPrintJobX linxPrintJob = new LinxPrintJobX();
            AddGetRasterDataToJob(linxPrintJob);
            EnqueueJob(linxPrintJob, nameof(StopJet));
        }

        public bool IsEnabledGetRasterData()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }

        #endregion

        #region Execute-Helper
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case nameof(CheckStatus):
                    CheckStatus();
                    return true;
                case nameof(IsEnabledCheckStatus):
                    result = IsEnabledCheckStatus();
                    return true;
                case nameof(StartPrint):
                    StartPrint();
                    return true;
                case nameof(IsEnabledStartPrint):
                    result = IsEnabledStartPrint();
                    return true;
                case nameof(StopPrint):
                    StopPrint();
                    return true;
                case nameof(IsEnabledStopPrint):
                    result = IsEnabledStopPrint();
                    return true;
                case nameof(StartJet):
                    StartJet();
                    return true;
                case nameof(IsEnabledStartJet):
                    result = IsEnabledStartJet();
                    return true;
                case nameof(StopJet):
                    StopJet();
                    return true;
                case nameof(IsEnabledStopJet):
                    result = IsEnabledStopJet();
                    return true;
                case nameof(GetRasterData):
                    GetRasterData();
                    return true;
                case nameof(IsEnabledGetRasterData):
                    result = IsEnabledGetRasterData();
                    return true;
            }
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }
        #endregion


        protected override PrintJob OnDoPrint(ACClassDesign aCClassDesign, int codePage, ReportData reportData)
        {
            return null;
        }

    }
}
