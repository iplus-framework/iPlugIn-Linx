using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.reporthandler;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace linx.core.reporthandler
{
    public partial class LinxPrinterXShared
    {
        private readonly ILinxPrinter _printer;
        public ACPrintServerBase PrintServerBase => _printer as ACPrintServerBase;
        public ILinxPrinter PrintServer => _printer;

        public LinxPrinterXShared(ILinxPrinter printer)
        {
            _printer = printer;
        }

        public void ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            _printer.DataSets = _printer.LoadDataSets();
        }

        public void ACPostInit()
        {
            _ShutdownEvent = new ManualResetEvent(false);
            _PollThread = new ACThread(Poll);
            _PollThread.Name = "ACUrl:" + PrintServerBase.GetACUrl() + ";Poll();";
            //_PollThread.ApartmentState = ApartmentState.STA;
            _PollThread.Start();

        }

        
        public void ACDeInit(bool deleteACClassTask = false)
        {
            if (_PollThread != null)
            {
                if (_ShutdownEvent != null && _ShutdownEvent.SafeWaitHandle != null && !_ShutdownEvent.SafeWaitHandle.IsClosed)
                    _ShutdownEvent.Set();
                if (!_PollThread.Join(5000))
                    _PollThread.Abort();
                _PollThread = null;
            }
        }        
    }
}
