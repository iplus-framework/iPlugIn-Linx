using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace linx.core.reporthandler
{
    public partial class LinxPrinterX
    {

        #region Methods
        public override bool SendDataToPrinter(gip.core.reporthandler.PrintJob printJob)
        {
            bool success = false;

            if (printJob != null)
            {
                ILinxPrintJob linxPrintJob = (ILinxPrintJob)printJob;

                if (linxPrintJob != null)
                {
                    if (DumpToTempFolder)
                    {
                       linxPrintJob.WriteTelegramsToFiles(Path.GetTempPath());
                    }
                    using (ACMonitor.Lock(_61000_LockPort))
                    {
                       Messages.LogMessage(eMsgLevel.Info, GetACUrl(), nameof(SendDataToPrinter) + "(100)", $"Add LinxPrintJob:{linxPrintJob.PrintJobID} to queue...");
                       _shared.EnqueueJob(linxPrintJob, linxPrintJob.Name);
                    }
                }
            }

            return success;
        }
                
        public void EnqueueJob(ILinxPrintJob linxPrintJob, string name)
        {
            _shared.EnqueueJob(linxPrintJob, name);
        }

        public bool ProcessJob(ILinxPrintJob linxPrintJob)
        {
            return _shared.ProcessJob(linxPrintJob);
        }


        protected bool ValidateMessage(MsgWithDetails msgWithDetails)
        {
            return _shared.ValidateMessage(msgWithDetails);
        }
        #endregion

    }
}
