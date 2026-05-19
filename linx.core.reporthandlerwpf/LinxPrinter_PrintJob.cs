using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.layoutengine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using linx.core.reporthandler;
using System.IO;

namespace linx.core.reporthandlerwpf
{
    public partial class LinxPrinter
    {
        #region Methods
        public void EnqueueJob(ILinxPrintJob linxPrintJob, string name)
        {
            _shared.EnqueueJob(linxPrintJob, name);
        }


        public override gip.core.reporthandler.PrintJob GetPrintJob(string reportName, FlowDocument flowDocument)
        {
            Encoding encoder = Encoding.Unicode;
            VBFlowDocument vBFlowDocument = flowDocument as VBFlowDocument;

            int? codePage = null;

            if (vBFlowDocument != null && vBFlowDocument.CodePage > 0)
            {
                codePage = vBFlowDocument.CodePage;
            }
            else if (CodePage > 0)
            {
                codePage = CodePage;
            }


            if (codePage != null)
            {
                try
                {
                    encoder = Encoding.GetEncoding(codePage.Value);
                }
                catch (Exception ex)
                {
                    Messages.LogException(GetACUrl(), nameof(GetPrintJob), ex);
                }
            }

            LinxPrintJob linxPrintJob = new LinxPrintJob();
            linxPrintJob.FlowDocument = flowDocument;
            linxPrintJob.Name = string.IsNullOrEmpty(flowDocument.Name) ? reportName : flowDocument.Name;
            linxPrintJob.Encoding = encoder;
            linxPrintJob.ColumnMultiplier = 1;
            linxPrintJob.ColumnDivisor = 1;
            OnRenderFlowDocument(linxPrintJob, linxPrintJob.FlowDocument);
            return linxPrintJob;
        }

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


        public bool ProcessJob(LinxPrintJob linxPrintJob)
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
