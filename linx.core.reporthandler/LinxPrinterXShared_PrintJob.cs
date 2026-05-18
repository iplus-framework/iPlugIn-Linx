using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace linx.core.reporthandler
{
    public partial class LinxPrinterXShared
    {

        #region Properties
        private Queue<ILinxPrintJob> _LinxPrintJobs;
        public Queue<ILinxPrintJob> LinxPrintJobs
        {
            get
            {
                if (_LinxPrintJobs == null)
                {
                    _LinxPrintJobs = new Queue<ILinxPrintJob>();
                }
                return _LinxPrintJobs;
            }
        }
        #endregion

        #region Methods
        public void EnqueueJob(ILinxPrintJob linxPrintJob, string name)
        {
            using (ACMonitor.Lock(PrintServer.LockPort))
            {
                PrintServer.Messages.LogMessage(eMsgLevel.Info, PrintServer.GetACUrl(), nameof(EnqueueJob), $"Add LinxPrintJob:{linxPrintJob.PrintJobID} to queue...");
                LinxPrintJobs.Enqueue(linxPrintJob);
            }
        }

        public bool ProcessJob(ILinxPrintJob linxPrintJob)
        {
            bool success = false;
            try
            {
                if (linxPrintJob.State == gip.core.reporthandler.PrintJobStateEnum.New)
                {
                    using (ACMonitor.Lock(PrintServer.LockPort))
                    {
                        linxPrintJob.State = gip.core.reporthandler.PrintJobStateEnum.InProcess;
                    }

                    foreach (Telegram telegram in linxPrintJob.PacketsForPrint)
                    {
                        bool requestSuccess = Request(telegram);
                        if (requestSuccess)
                        {
                            Thread.Sleep(PrintServerBase.ReceiveTimeout);
                            (bool responseSuccess, byte[] responseData) = Response(linxPrintJob, telegram);
                            // TODO: Linxmapping according Type of Telegram!! Temporary workaround for DeleteReport
                            if (telegram.LinxPrintJobType != LinxPrintJobTypeEnum.DeleteReport
                                && responseSuccess
                                && responseData != null)
                            {
                                if (telegram.LinxPrintJobType == LinxPrintJobTypeEnum.CheckStatus)
                                {
                                    (MsgWithDetails msgWithDetails, LinxPrinterCompleteStatusResponse response) = LinxMapping<LinxPrinterCompleteStatusResponse>.Map(responseData);
                                    if (ValidateMessage(msgWithDetails) && response != null)
                                        PrintServer.PrinterCompleteStatus.ValueT = response;
                                }
                                else if (telegram.LinxPrintJobType == LinxPrintJobTypeEnum.RasterData)
                                {
                                    (MsgWithDetails msgWithDetails, LinxPrinterRasterDataResponse response) = LinxMapping<LinxPrinterRasterDataResponse>.Map(responseData);
                                    if (ValidateMessage(msgWithDetails) && response != null)
                                    {
                                        response.ParseData(responseData);
                                        string dumpedResult = response.ToString();
                                        PrintServer.Messages.LogInfo(PrintServer.GetACUrl(), $"{nameof(LinxPrinterX)}.{nameof(ProcessJob)}(120)", dumpedResult);
                                        PrintServerBase.OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, dumpedResult, true);
                                    }
                                }
                                else
                                {
                                    (MsgWithDetails msgWithDetails, LinxPrinterStatusResponse response) = LinxMapping<LinxPrinterStatusResponse>.Map(responseData);
                                    if (ValidateMessage(msgWithDetails) && response != null)
                                    {
                                        if (response.P_Status > 0 || response.C_Status > 0)
                                        {
                                            using (ACMonitor.Lock(PrintServer.LockPort))
                                            {
                                                linxPrintJob.State = gip.core.reporthandler.PrintJobStateEnum.InAlarm;
                                            }

                                            string message = $"JobID: {linxPrintJob.PrintJobID}| Printer return P_Status: {response.P_Status}; C_Status: {response.C_Status}";

                                            if (response.P_Status > 0)
                                            {
                                                LinxPrintErrorEnum printErrorEnum = LinxPrintErrorEnum.Remote_alarm;
                                                if (Enum.TryParse<LinxPrintErrorEnum>(response.P_Status.ToString(), out printErrorEnum))
                                                {
                                                    message += System.Environment.NewLine;
                                                    message += "Printer error code: " + printErrorEnum.ToString();
                                                }
                                            }

                                            if (response.C_Status > 0)
                                            {
                                                LinxCommandStatusCodeEnum commandStatusCode = LinxCommandStatusCodeEnum.Data_overrun;
                                                if (Enum.TryParse<LinxCommandStatusCodeEnum>(response.C_Status.ToString(), out commandStatusCode))
                                                {
                                                    message += System.Environment.NewLine;
                                                    message += "Command status code: " + commandStatusCode.ToString();
                                                }
                                            }

                                            PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                                            if (PrintServerBase.IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), message) == null)
                                            {
                                                PrintServer.Messages.LogError(PrintServer.GetACUrl(), $"{nameof(LinxPrinterX)}.{nameof(ProcessJob)}(140)", message);
                                            }
                                            PrintServerBase.OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, message, true);
                                            if (PrintServer.PrinterCompleteStatus.ValueT != null)
                                            {
                                                PrintServer.PrinterCompleteStatus.ValueT.P_Status = response.P_Status;
                                                PrintServer.PrinterCompleteStatus.ValueT.C_Status = response.C_Status;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (ACMonitor.Lock(PrintServer.LockPort))
                            {
                                linxPrintJob.State = gip.core.reporthandler.PrintJobStateEnum.InAlarm;
                            }
                            break;
                        }
                    }


                    using (ACMonitor.Lock(PrintServer.LockPort))
                    {
                        linxPrintJob.State = gip.core.reporthandler.PrintJobStateEnum.Done;
                    }
                }
            }
            catch (Exception ex)
            {
                PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                if (PrintServerBase.IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), ex.Message) == null)
                {
                    PrintServer.Messages.LogException(PrintServer.GetACUrl(), $"{nameof(LinxPrinterX)}.{nameof(ProcessJob)}(70)", ex);
                }
                PrintServerBase.OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, ex.Message, true);
            }

            ClosePort();
            return success;
        }


        public bool ValidateMessage(MsgWithDetails msgWithDetails)
        {
            if (msgWithDetails == null)
                return false;
            if (!msgWithDetails.IsSucceded())
            {
                PrintServer.LinxPrinterAlarm.ValueT = PANotifyState.AlarmOrFault;
                if (PrintServerBase.IsAlarmActive(nameof(PrintServer.LinxPrinterAlarm), msgWithDetails.DetailsAsText) == null)
                {
                    PrintServer.Messages.LogError(PrintServer.GetACUrl(), $"{nameof(LinxPrinterX)}.{nameof(ProcessJob)}(120)", msgWithDetails.DetailsAsText);
                }
                PrintServerBase.OnNewAlarmOccurred(PrintServer.LinxPrinterAlarm, msgWithDetails.DetailsAsText, true);
                return false;
            }
            return true;
        }
        #endregion

    }
}
