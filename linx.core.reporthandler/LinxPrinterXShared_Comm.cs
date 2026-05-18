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
    public partial class LinxPrinterXShared
    {
        #region Thread
        protected ManualResetEvent _ShutdownEvent;
        ACThread _PollThread;
        private void Poll()
        {
            string jobInfo = "";
            try
            {
                while (!_ShutdownEvent.WaitOne(2000, false))
                {
                    _PollThread.StartReportingExeTime();

                    ILinxPrintJob linxPrintJob = null;
                    //LinxPrintJob linxPrintJob = LinxPrintJobs.Where(c => c.State == PrintJobStateEnum.New).OrderBy(c => c.InsertDate).FirstOrDefault();
                    using (ACMonitor.Lock(PrintServer.LockPort))
                    {
                        if (LinxPrintJobs.Any())
                        {
                            linxPrintJob = LinxPrintJobs.Dequeue();
                        }
                        if (linxPrintJob != null)
                        {
                            jobInfo = linxPrintJob.GetJobInfo();
                            ProcessJob(linxPrintJob);
                            PrintServer.Messages.LogInfo(PrintServerBase.GetACUrl(), $"{nameof(LinxPrinterX)}.{nameof(Poll)}(10)", $"Job {jobInfo} processed!");
                        }
                    }
                    _PollThread.StopReportingExeTime();
                }
            }
            catch (ThreadAbortException ec)
            {
                string message = $"Exception processing job {jobInfo}! Exception: {ec.Message}";
                PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                if (PrintServerBase.IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), message) == null)
                {
                    PrintServer.Messages.LogException(PrintServerBase.GetACUrl(), $"{nameof(LinxPrinterX)}.{nameof(Poll)}(20)", ec);
                }
                PrintServerBase.OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, message, true);
            }
        }
        #endregion

        #region Communication -> Open / Close port

        public bool OpenPort()
        {
            bool success = false;
            if (!IsEnabledOpenPort())
            {
                UpdateIsConnectedState();
                return PrintServerBase.IsConnected.ValueT;
            }
            if (PrintServer.TCPCommEnabled)
            {
                try
                {
                    using (ACMonitor.Lock(PrintServer.LockPort))
                    {
                        if (PrintServer._TcpClientField == null)
                            PrintServer._TcpClientField = new TcpClient();
                        if (PrintServer.WriteTimeout > 0)
                            PrintServer._TcpClientField.SendTimeout = PrintServer.WriteTimeout;
                        if (PrintServer.ReadTimeout > 0)
                            PrintServer._TcpClientField.ReceiveTimeout = PrintServer.ReadTimeout;
                        if (!PrintServer._TcpClientField.Connected)
                        {
                            if (!String.IsNullOrEmpty(PrintServerBase.IPAddress))
                            {
                                IPAddress ipAddress = System.Net.IPAddress.Parse(PrintServerBase.IPAddress);
                                PrintServer._TcpClientField.Connect(ipAddress, PrintServerBase.Port);
                            }
                            else
                            {
                                PrintServer._TcpClientField.Connect(PrintServer.ServerDNSName, PrintServerBase.Port);
                            }
                        }
                    }
                    success = PrintServer._TcpClientField.Connected;
                }
                catch (Exception e)
                {
                    PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                    if ((PrintServer as PAClassAlarmingBase).IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), e.Message) == null)
                    {
                        PrintServer.Messages.LogException(PrintServerBase.GetACUrl(), $"{nameof(ILinxPrinter)}.{nameof(OpenPort)}(05)", e);
                    }
                    (PrintServer as PAClassAlarmingBase).OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, e.Message, true);
                    ClosePort();
                }
                UpdateIsConnectedState();
            }
            else if (PrintServer.SerialCommEnabled)
            {
                try
                {
                    using (ACMonitor.Lock(PrintServer.LockPort))
                    {
                        //_serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
                        PrintServer._SerialPortField = new SerialPort(PrintServer.PortName, PrintServer.BaudRate);
                        if (PrintServer.ReadTimeout > 0)
                            PrintServer._SerialPortField.ReadTimeout = PrintServer.ReadTimeout;
                        else
                            PrintServer._SerialPortField.ReadTimeout = 5000;
                        if (PrintServer.WriteTimeout > 0)
                            PrintServer._SerialPortField.WriteTimeout = PrintServer.WriteTimeout;
                        if (PrintServer.RtsEnable == true)
                            PrintServer._SerialPortField.RtsEnable = true;
                        if (PrintServer.Handshake != System.IO.Ports.Handshake.None)
                            PrintServer._SerialPortField.Handshake = PrintServer.Handshake;
                        if (!PrintServer._SerialPortField.IsOpen)
                        {
                            PrintServer._SerialPortField.Open();
                        }
                    }
                    success = PrintServer._SerialPortField.IsOpen;
                }
                catch (Exception e)
                {
                    PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                    if ((PrintServer as PAClassAlarmingBase).IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), e.Message) == null)
                    {
                        PrintServer.Messages.LogException(PrintServerBase.GetACUrl(), $"{nameof(ILinxPrinter)}.{nameof(OpenPort)}(30)", e);
                    }
                    (PrintServer as PAClassAlarmingBase).OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, e.Message, true);
                }
                UpdateIsConnectedState();
            }
            return success;
        }

        public bool IsEnabledOpenPort()
        {
            if ((!PrintServer.TCPCommEnabled && !PrintServer.SerialCommEnabled)
                || (PrintServerBase.ACOperationMode != ACOperationModes.Live))
                return false;
            if (PrintServer.TCPCommEnabled)
            {
                var client = PrintServer.TcpClient;
                if (client == null)
                    return !String.IsNullOrEmpty(PrintServerBase.IPAddress) || !String.IsNullOrEmpty(PrintServer.ServerDNSName);
                return !client.Connected;
            }
            else if (PrintServer.SerialCommEnabled)
            {
                var port = PrintServer.SerialPort;
                if (port == null)
                    return !String.IsNullOrEmpty(PrintServer.PortName);
                return !port.IsOpen;
            }
            return false;
        }

        public void ClosePort()
        {
            if (!IsEnabledClosePort())
                return;
            using (ACMonitor.Lock(PrintServer.LockPort))
            {
                if (PrintServer._TcpClientField != null)
                {
                    if (PrintServer._TcpClientField.Connected)
                        PrintServer._TcpClientField.Close();
                    PrintServer._TcpClientField.Dispose();
                    PrintServer._TcpClientField = null;
                }
                if (PrintServer._SerialPortField != null)
                {
                    if (PrintServer._SerialPortField.IsOpen)
                        PrintServer._SerialPortField.Close();
                    PrintServer._SerialPortField.Dispose();
                    PrintServer._SerialPortField = null;
                }
            }
            PrintServerBase.IsConnected.ValueT = false;
        }

        public bool IsEnabledClosePort()
        {
            var client = PrintServer.TcpClient;
            if (client != null)
                return true;
            var port = PrintServer.SerialPort;
            if (port != null)
                return port.IsOpen;
            return false;
        }

        public void UpdateIsConnectedState()
        {
            var client = PrintServer.TcpClient;
            if (client != null)
            {
                PrintServerBase.IsConnected.ValueT = client.Connected;
                return;
            }
            else
            {
                var port = PrintServer.SerialPort;
                if (port != null)
                {
                    PrintServerBase.IsConnected.ValueT = port.IsOpen;
                    return;
                }
            }
            PrintServerBase.IsConnected.ValueT = false;
        }

        #endregion

        #region Send / Receive

        public bool Request(Telegram telegram)
        {
            bool success = OpenPort();

            try
            {
                if (success)
                {
                    if (PrintServer.TCPCommEnabled)
                    {
                        if (PrintServer.TcpClient == null || !PrintServer.TcpClient.Connected)
                            return false;
                        NetworkStream stream = PrintServer.TcpClient.GetStream();
                        if (stream != null && stream.CanWrite)
                        {
                            stream.Write(telegram.Packet, 0, telegram.Packet.Length);
                            success = true;
                        }
                    }
                    else if (PrintServer.SerialCommEnabled)
                    {
                        if (PrintServer.SerialPort.IsOpen)
                        {
                            PrintServer.SerialPort.Write(telegram.Packet, 0, telegram.Packet.Length);
                            success = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                if ((PrintServer as PAClassAlarmingBase).IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), ex.Message) == null)
                {
                    PrintServer.Messages.LogException(PrintServerBase.GetACUrl(), $"{nameof(ILinxPrinter)}.{nameof(Request)}(40)", ex);
                }
                (PrintServer as PAClassAlarmingBase).OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, ex.Message, true);
            }

            return success;
        }

        public (bool, byte[]) Response(ILinxPrintJob linxPrintJob, Telegram telegram)
        {
            bool success = false;
            List<byte> result = new List<byte>();
            OpenPort();
            if (PrintServer.TCPCommEnabled)
            {
                using (ACMonitor.Lock(PrintServer.LockPort))
                {
                    if (PrintServer.TcpClient != null || PrintServer.TcpClient.Connected)
                    {
                        try
                        {
                            NetworkStream stream = PrintServer.TcpClient.GetStream();
                            if (stream != null)
                            {
                                if (stream.CanRead) // && stream.DataAvailable)
                                {
                                    byte[] myReadBuffer = new byte[1024];
                                    int numberOfBytesRead = 0;
                                    do
                                    {
                                        numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                                        if (numberOfBytesRead > 0)
                                            result.AddRange(myReadBuffer.Take(numberOfBytesRead));
                                    }
                                    while (stream.DataAvailable);

                                    success = result != null && result.Any();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                            if ((PrintServer as PAClassAlarmingBase).IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), ex.Message) == null)
                            {
                                PrintServer.Messages.LogException(PrintServerBase.GetACUrl(), $"{nameof(ILinxPrinter)}.{nameof(Response)}(50)", ex);
                            }
                            (PrintServer as PAClassAlarmingBase).OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, ex.Message, true);
                        }

                    }
                }
            }
            else if (PrintServer.SerialCommEnabled)
            {
                if (PrintServer.SerialPort != null && PrintServer.SerialPort.IsOpen)
                {
                    using (ACMonitor.Lock(PrintServer.LockPort))
                    {
                        byte[] myReadBuffer = new byte[1024];
                        int numberOfBytesRead = 0;
                        try
                        {
                            numberOfBytesRead = PrintServer.SerialPort.Read(myReadBuffer, 0, 1024);
                            result.AddRange(myReadBuffer.Take(numberOfBytesRead));

                            success = result != null && result.Any();
                        }
                        catch (Exception ex)
                        {
                            PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                            if ((PrintServer as PAClassAlarmingBase).IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), ex.Message) == null)
                            {
                                PrintServer.Messages.LogException(PrintServerBase.GetACUrl(), $"{nameof(ILinxPrinter)}.{nameof(Response)}(60)", ex);
                            }
                            (PrintServer as PAClassAlarmingBase).OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, ex.Message, true);
                        }
                    }
                }
            }

            if (success)
            {
                if (telegram.LinxPrintJobType != LinxPrintJobTypeEnum.RasterData
                    && telegram.LinxPrintJobType != LinxPrintJobTypeEnum.DeleteReport)
                    success = LinxHelper.ValidateChecksum(result.ToArray());
                if (!success)
                {
                    string message = $"Bad checksum for response: {linxPrintJob.GetJobInfo()}";
                    PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                    PrintServer.Messages.LogError(PrintServerBase.GetACUrl(), $"{nameof(ILinxPrinter)}.{nameof(Request)}(70)", message);
                    (PrintServer as PAClassAlarmingBase).OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, message, true);
                }
            }
            return (success, result.ToArray());
        }

        #endregion
    }
}
