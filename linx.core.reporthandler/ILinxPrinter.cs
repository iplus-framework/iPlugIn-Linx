using gip.core.autocomponent;
using gip.core.datamodel;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace linx.core.reporthandler
{
    public interface ILinxPrinter : IACComponent
    {
        bool UseRemoteReport { get; set; }

        IACContainerTNet<LinxPrinterCompleteStatusResponse> PrinterCompleteStatus
        {
            get;
            set;
        }

        List<LinxDataSetData> LoadDataSets();
        List<LinxDataSetData> DataSets { get; set; }


        IACContainerTNet<PANotifyState> LinxPrinterAlarm { get; set; }   


        ACMonitorObject LockPort { get; }

        #region Serial Communication
        bool SerialCommEnabled { get; set; }

        SerialPort SerialPort { get; }

        SerialPort _SerialPortField { get; set;}

        string PortName { get; set; }

        int BaudRate { get; set; }

        Parity Parity { get; set; }

        int DataBits { get; set; }

        StopBits StopBits { get; set; }

        bool RtsEnable { get; set; }

        Handshake Handshake { get; set; }

        #endregion

        #region TCP-Communication

        [ACPropertyInfo(true, 401, "Config", "en{'TCP-Communication on'}de{'TCP- Kommunikation ein'}", DefaultValue = false)]
        bool TCPCommEnabled { get; set; }

        TcpClient TcpClient { get; }

        TcpClient _TcpClientField { get; set; }


        string ServerDNSName { get; set; }

        bool TraceValues { get; set; }

        bool IgnoreInvalidTeleLength { get; set; }

        #endregion

        #region Common configuration
        int PollingInterval { get; set; }

        int ReadTimeout { get; set; }

        int WriteTimeout { get; set; }
        #endregion        
    }
}
