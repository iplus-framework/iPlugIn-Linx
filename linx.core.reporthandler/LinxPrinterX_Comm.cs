using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace linx.core.reporthandler
{
    public partial class LinxPrinterX
    {
        #region Serial Communication

        protected readonly ACMonitorObject _61000_LockPort = new ACMonitorObject(61000);
        public ACMonitorObject LockPort => _61000_LockPort;

        [ACPropertyInfo(true, 411, "Config", "en{'Serial-Communication on'}de{'Serial-Kommunikation ein'}", DefaultValue = false)]
        public bool SerialCommEnabled { get; set; }


        protected SerialPort _serialPort = null;
        [ACPropertyInfo(9999)]
        public SerialPort SerialPort
        {
            get
            {
                using (ACMonitor.Lock(_61000_LockPort))
                {
                    return _serialPort;
                }
            }
        }

        public SerialPort _SerialPortField
        {
            get
            {
                return _serialPort;
            }
            set
            {
                _serialPort = value;
            }
        } 


        /// <summary>
        /// PortName
        /// </summary>
        [ACPropertyInfo(true, 412, DefaultValue = "COM1")]
        public string PortName { get; set; }

        /// <summary>
        /// BaudRate
        /// </summary>
        [ACPropertyInfo(true, 413, DefaultValue = 9600)]
        public int BaudRate { get; set; }

        /// <summary>
        /// Parity
        /// </summary>
        [ACPropertyInfo(true, 414)]
        public Parity Parity { get; set; }

        /// <summary>
        /// DataBits
        /// </summary>
        [ACPropertyInfo(true, 415, DefaultValue = 8)]
        public int DataBits { get; set; }

        /// <summary>
        /// StopBits
        /// </summary>
        [ACPropertyInfo(true, 416)]
        public StopBits StopBits { get; set; }


        /// <summary>
        /// RtsEnable
        /// </summary>
        [ACPropertyInfo(true, 417, DefaultValue = false)]
        public bool RtsEnable { get; set; }

        /// <summary>
        /// Handshake
        /// </summary>
        [ACPropertyInfo(true, 418)]
        public Handshake Handshake { get; set; }

        #endregion

        #region TCP-Communication

        [ACPropertyInfo(true, 401, "Config", "en{'TCP-Communication on'}de{'TCP- Kommunikation ein'}", DefaultValue = false)]
        public bool TCPCommEnabled { get; set; }

        protected TcpClient _tcpClient = null;
        [ACPropertyInfo(9999)]
        public TcpClient TcpClient
        {
            get
            {
                using (ACMonitor.Lock(_61000_LockPort))
                {
                    return _tcpClient;
                }
            }
        }

        public TcpClient _TcpClientField
        {
            get
            {
                return _tcpClient;
            }
            set
            {
                _tcpClient = value;
            }
        }

        [ACPropertyInfo(true, 403, "Config", "en{'DNS-Name [ms]'}de{'DNS-Name [ms]'}")]
        public string ServerDNSName { get; set; }

        [ACPropertyInfo(false, 405, "Config", "en{'Trace read values'}de{'Ausgabe gelesene Werte'}", DefaultValue = false)]
        public bool TraceValues { get; set; }

        [ACPropertyInfo(false, 406, "Config", "en{'IgnoreInvalidTeleLength}de{'IgnoreInvalidTeleLength'}", DefaultValue = false)]
        public bool IgnoreInvalidTeleLength { get; set; }

        #endregion

        #region Common configuration
        [ACPropertyInfo(true, 400, "Config", "en{'Polling [ms]'}de{'Abfragezyklus [ms]'}", DefaultValue = 200)]
        public int PollingInterval { get; set; }

        /// <summary>
        /// ReadTimeout
        /// </summary>
        [ACPropertyInfo(true, 405, "Config", "en{'Read-Timeout [ms]'}de{'Lese-Timeout [ms]'}", DefaultValue = 5000)]
        public int ReadTimeout { get; set; }

        /// <summary>
        /// WriteTimeout
        /// </summary>
        [ACPropertyInfo(true, 406, "Config", "en{'Write-Timeout [ms]'}de{'Schreibe-Timeout [ms]'}", DefaultValue = 2000)]
        public int WriteTimeout { get; set; }

        #endregion

        #region Printer
        [ACPropertyBindingSource(9999, "Error", "en{'Linx printer alarm'}de{'Linx Drucker Alarm'}", "", false, false)]
        public IACContainerTNet<PANotifyState> LinxPrinterAlarm { get; set; }        
        #endregion

        #region Communication -> Open / Close port

        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Open Connection'}de{'Öffne Verbindung'}", 200, true)]
        public bool OpenPort()
        {
            return _shared.OpenPort();
        }

        public bool IsEnabledOpenPort()
        {
            return _shared.IsEnabledOpenPort();
        }

        [ACMethodInteraction(nameof(LinxPrinterX), "en{'Close Connection'}de{'Schliesse Verbindung'}", 201, true)]
        public void ClosePort()
        {
            _shared.ClosePort();
        }

        public bool IsEnabledClosePort()
        {
            return _shared.IsEnabledClosePort();
        }

        protected void UpdateIsConnectedState()
        {
            _shared.UpdateIsConnectedState();
        }

        #endregion

        #region Send / Receive

        public bool Request(Telegram telegram)
        {
            return _shared.Request(telegram);
        }

        public (bool, byte[]) Response(ILinxPrintJob linxPrintJob, Telegram telegram)
        {
            return _shared.Response(linxPrintJob, telegram);
        }

        #endregion
    }
}
