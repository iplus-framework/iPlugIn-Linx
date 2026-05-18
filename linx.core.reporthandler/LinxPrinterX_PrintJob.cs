using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace linx.core.reporthandler
{
    public partial class LinxPrinterX
    {

        #region Methods
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
