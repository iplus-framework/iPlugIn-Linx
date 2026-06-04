using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.reporthandlerwpf;
using System.Threading;
using System.Threading.Tasks;
using linx.core.reporthandler;

namespace linx.core.reporthandlerwpf
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'LinxPrinter'}de{'LinxPrinter'}", Global.ACKinds.TPABGModule, Global.ACStorableTypes.Required, false, false)]
    public partial class LinxPrinter : ACPrintServerBaseWPF, ILinxPrinter
    {
        private LinxPrinterXShared _shared;

        #region ctor's
        public LinxPrinter(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
           : base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _shared = new LinxPrinterXShared(this);
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            if (!base.ACInit(startChildMode))
                return false;

            _ = IPAddress;
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

        [ACMethodInteraction(nameof(LinxPrinter), "en{'Check status'}de{'Status überprüfen'}", 200, true)]
        public void CheckStatus()
        {
            LinxPrintJob linxPrintJob = new LinxPrintJob();
            AddCheckStatusToJob(linxPrintJob);
            EnqueueJob(linxPrintJob, nameof(StartPrint));
        }

        public bool IsEnabledCheckStatus()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinter), "en{'Start Print'}de{'Drucker Start'}", 201, true)]
        public void StartPrint()
        {
            LinxPrintJob linxPrintJob = new LinxPrintJob();
            AddPrintCommandToJob(linxPrintJob);
            EnqueueJob(linxPrintJob, nameof(StartPrint));
        }

        public bool IsEnabledStartPrint()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinter), "en{'Stop Print'}de{'Drucker Stopp'}", 202, true)]
        public void StopPrint()
        {
            LinxPrintJob linxPrintJob = new LinxPrintJob();
            AddPrintCommandToJob(linxPrintJob, true);
            EnqueueJob(linxPrintJob, nameof(StopPrint));
        }

        public bool IsEnabledStopPrint()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinter), "en{'Switch on printer'}de{'Drucker einschalten'}", 203, true)]
        public void StartJet()
        {
            LinxPrintJob linxPrintJob = new LinxPrintJob();
            AddJetCommandToJob(linxPrintJob);
            EnqueueJob(linxPrintJob, nameof(StartJet));
        }

        public bool IsEnabledStartJet()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinter), "en{'Switch of printer'}de{'Drucker ausschalten'}", 204, true)]
        public void StopJet()
        {
            LinxPrintJob linxPrintJob = new LinxPrintJob();
            AddJetCommandToJob(linxPrintJob, true);
            EnqueueJob(linxPrintJob, nameof(StopJet));

        }

        public bool IsEnabledStopJet()
        {
            return IsConnected.ValueT || IsEnabledOpenPort() || IsEnabledClosePort();
        }


        [ACMethodInteraction(nameof(LinxPrinter), "en{'Read Raster data'}de{'Lese Rasterdaten'}", 205, true)]
        public void GetRasterData()
        {
            LinxPrintJob linxPrintJob = new LinxPrintJob();
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
    }
}
