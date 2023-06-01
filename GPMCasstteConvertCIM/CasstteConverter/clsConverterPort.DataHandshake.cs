﻿using GPMCasstteConvertCIM.Alarm;
using GPMCasstteConvertCIM.CasstteConverter.Data;
using GPMCasstteConvertCIM.Devices;
using GPMCasstteConvertCIM.GPM_SECS;
using GPMCasstteConvertCIM.Utilities;
using Secs4Net;
using Secs4Net.Sml;
using System.Diagnostics;
using static GPMCasstteConvertCIM.CasstteConverter.Data.clsMemoryAddress;
using static GPMCasstteConvertCIM.CasstteConverter.Enums;
using static GPMCasstteConvertCIM.GPM_SECS.SECSMessageHelper;

namespace GPMCasstteConvertCIM.CasstteConverter
{
    public partial class clsConverterPort
    {
        private SECSBase MCS => DevicesManager.secs_host_for_mcs;
        private bool CarrierWaitIn_Reply = false;
        private bool CarrierWaitIn_Accept = false;
        private bool Carrier_TransferCompletedFlag = false;

        /// <summary>
        /// MCS ->CIM : ModeChangeReques
        /// </summary>
        /// <param name="portUnitType"></param>
        /// <returns></returns>
        internal async Task<bool> ModeChangeRequestHandshake(PortUnitType portUnitType)
        {

            Utilities.Utility.SystemLogger.Info($"MCS Request [{Properties.PortID}] Change Port Type To {portUnitType}");

            bool plc_accept = false;
            string port_type_data_address_name = PortCIMWordAddress[PROPERTY.Port_Type_Status];
            string cim_2_eq_port_mode_change_req_address_name = PortCIMBitAddress[PROPERTY.Port_Mode_Change_Request];
            string eq_2_cim_port_mode_change_accept_address_name = PortEQBitAddress[PROPERTY.Port_Mode_Change_Accept];
            string eq_2_cim_port_mode_change_refuse_address_name = PortEQBitAddress[PROPERTY.Port_Mode_Changed_Refuse];

            var plc_accept_address = EQParent.LinkBitMap.First(ad => ad.Address == eq_2_cim_port_mode_change_accept_address_name);
            var plc_refuse_address = EQParent.LinkBitMap.First(ad => ad.Address == eq_2_cim_port_mode_change_refuse_address_name);

            //write porttype data to word memory
            VirtualMemoryTable.WriteBinary(port_type_data_address_name, (int)portUnitType);
            await Task.Delay(1000);
            //On CIM Bit
            VirtualMemoryTable.WriteOneBit(cim_2_eq_port_mode_change_req_address_name, true);
            //wait EQ Bit on

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool timeout = false;
            while (!(bool)plc_accept_address.Value && !(bool)plc_refuse_address.Value)
            {
                await Task.Delay(10);
                if (cts.IsCancellationRequested)
                {
                    VirtualMemoryTable.WriteOneBit(cim_2_eq_port_mode_change_req_address_name, false);
                    VirtualMemoryTable.WriteBinary(port_type_data_address_name, 0);
                    AlarmManager.AddAlarm(ALARM_CODES.PortTypeChangeRequest_HS_EQ_Timeout, Properties.PortID);
                    Utilities.Utility.SystemLogger.Warning($"ModeChangeRequestHandshake EQ Timeout");
                    return false;
                }
            }
            plc_accept = (bool)plc_accept_address.Value;
            Utilities.Utility.SystemLogger.Info($"PLC Reply {plc_accept} ,[{Properties.PortID}] Change Port Type To {portUnitType}");

            VirtualMemoryTable.WriteOneBit(cim_2_eq_port_mode_change_req_address_name, false);
            cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            while ((bool)plc_accept_address.Value | (bool)plc_refuse_address.Value)
            {
                await Task.Delay(10);
                if (cts.IsCancellationRequested)
                {
                    VirtualMemoryTable.WriteBinary(port_type_data_address_name, 0);
                    return false;
                }
            }
            VirtualMemoryTable.WriteBinary(port_type_data_address_name, 0);
            return plc_accept;
        }



        /// <summary>
        /// EQ->CIM : Port Type Changed Report
        /// </summary>
        public async void PortTypeChangedReportHandshake()
        {
            _ = Task.Factory.StartNew(() =>
            {
                clsMemoryAddress eq_to_cim_report_adress = EQParent.LinkBitMap.First(i => i.EOwner == OWNER.EQP && i.EScope.ToString() == portNoName && i.EProperty == PROPERTY.Port_Mode_Changed_Report);
                string cim_2_eq_reply_address = PortCIMBitAddress[PROPERTY.Port_Mode_Changed_Report_Reply];
                //ON CIM BIT
                EQParent.CIMMemOptions.memoryTable.WriteOneBit(cim_2_eq_reply_address, true);
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                bool timeout = false;
                //等待EQ OFF BIT
                while ((bool)eq_to_cim_report_adress.Value)
                {
                    Thread.Sleep(1);
                    if (cts.IsCancellationRequested)
                    {
                        timeout = true;//T3 Timeout
                        break;
                    }
                }
                //OFF CIM BIT
                EQParent.CIMMemOptions.memoryTable.WriteOneBit(cim_2_eq_reply_address, false);
                cts.Dispose();

            });
        }


        /// <summary>
        /// CIM-> MCS : Port Out of service report
        /// </summary>
        public async void PortOutOfServiceReport()
        {
            Task tk = new Task(async () =>
            {
                SecsMessage? replyMsg = await MCS.SendMsg(EventsMsg.PortService(Properties.PortID, false));
                if (replyMsg.IsS9F7())
                    AlarmManager.AddWarning(ALARM_CODES.MCS_PORT_OUT_SERVICE_REPORT_FAIL, Properties.PortID);
                else
                    Utilities.Utility.SystemLogger.Info($"PortOutOfServiceReport Done. \r\n MCS Reply \r\n{replyMsg.ToSml()}");
                await Task.Delay(1000);

            });
            tk.Start();

        }

        /// <summary>
        /// CIM-> MCS : Port In service report
        /// </summary>
        public async void PortInServiceReport()
        {
            Task tk = new Task(async () =>
            {
                SecsMessage? replyMsg = await MCS.SendMsg(EventsMsg.PortService(Properties.PortID, true));
                if (replyMsg.IsS9F7())
                    AlarmManager.AddWarning(ALARM_CODES.MCS_PORT_IN_SERVICE_REPORT_FAIL, Properties.PortID);
                else
                    Utility.SystemLogger.Info($"PortInServiceReport Done. \r\n MCS Reply \r\n {replyMsg.ToSml()}");
            });
            tk.Start();
        }

        /// <summary>
        /// CIM->MCS : Port Type Input Report
        /// </summary>
        public async void PortTypeInputReport()
        {
            Task tk = new Task(async () =>
            {
                var replyMsg = await MCS.SendMsg(EventsMsg.PortType(Properties.PortID, PortUnitType.Input));
                if (replyMsg.IsS9F7())
                    AlarmManager.AddWarning(ALARM_CODES.MCS_PORT_TYPE_INPUT_REPORT_FAIL, Properties.PortID);
                else
                    Utility.SystemLogger.Info($"PortTypeInputReport Done. \r\n MCS Reply \r\n {replyMsg.ToSml()}");

            });
            tk.Start();
        }

        /// <summary>
        /// CIM->MCS : Port Type Output Report
        /// </summary>
        public async void PortTypeOutputReport()
        {
            Task tk = new Task(async () =>
             {
                 var replyMsg = await MCS.SendMsg(EventsMsg.PortType(Properties.PortID, PortUnitType.Output));
                 if (replyMsg.IsS9F7())
                     AlarmManager.AddWarning(ALARM_CODES.MCS_PORT_TYPE_OUTPUT_REPORT_FAIL, Properties.PortID);
                 else
                     Utilities.Utility.SystemLogger.Info($"PortTypeInputReport Done. \r\n MCS Reply \r\n {replyMsg.ToSml()}");
             });
            tk.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task CarrierRemovedCompletedReply()
        {
            var carrier_removed_com_reply_address = PortCIMBitAddress[PROPERTY.Carrier_Removed_Completed_Report_Reply];
            EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_removed_com_reply_address, true);
            CancellationTokenSource cst = new CancellationTokenSource(TimeSpan.FromSeconds(5000));
            while (CarrierRemovedCompletedReport)
            {
                if (cst.IsCancellationRequested)
                {
                    AlarmManager.AddWarning(ALARM_CODES.CarrierRemovedCompolete_HS_EQ_Timeout, Properties.PortID);
                    break;
                }
                await Task.Delay(1);
            }
            EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_removed_com_reply_address, false);
        }

        internal void ReportCarrierRemovedCompToMCS()
        {
            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    Utility.SystemLogger.Info($"ReportCarrierRemovedCompToMCS");
                    var BCRIDExist = await WaitBCRReadCompleted();

                    if (!BCRIDExist)
                    {
                        AlarmManager.AddWarning(ALARM_CODES.Try_CarrierRemovedCompletedReport_But_BCRID_Not_Exist, Properties.PortID);
                        return;
                    }

                    var response = await MCS.SendMsg(EventsMsg.CarrierRemovedCompleted(Previous_WIPINFO_BCR_ID, Properties.PortID, EPortAutoStatus == AUTO_MANUAL_MODE.AUTO)); //TODO Zone Name ?
                    if (response.IsS9F7())
                        AlarmManager.AddWarning(ALARM_CODES.MCS_CARRIER_REMOVED_COMPLETED_REPORT_FAIL, Properties.PortID);
                }
                catch (Exception ex)
                {
                    AlarmManager.AddWarning(ALARM_CODES.MCS_CARRIER_REMOVED_COMPLETED_REPORT_FAIL, Properties.PortID);
                }
            });
        }


        /// <summary>
        ///  EQ->CIM->MCS : Carrier Wait out 
        /// </summary>
        /// <param name="EQ_T_timeout"></param>
        /// <returns></returns>
        public async Task<bool> CarrierWaitOutReply(int EQ_T_timeout = 5000)
        {
            if (PortCIMBitAddress.TryGetValue(PROPERTY.Carrier_WaitOut_System_Reply, out string carrier_wait_out_reply_address))
            {
                EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_wait_out_reply_address, true);

                Utility.SystemLogger.Info($"Carrier Wait Out HS Start_ {carrier_wait_out_reply_address} ON");
                CancellationTokenSource cst = new CancellationTokenSource(TimeSpan.FromMilliseconds(EQ_T_timeout));
                while (CarrierWaitOUTSystemRequest)
                {
                    if (cst.IsCancellationRequested)
                    {
                        Utility.SystemLogger.Info($"Carrier Wait Out HS => EQ Timeout");
                        AlarmManager.AddWarning(ALARM_CODES.CarrierWaitOut_HS_EQ_Timeout, Properties.PortID);
                        break;
                    }
                    await Task.Delay(1);
                }
                EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_wait_out_reply_address, false);


                Utility.SystemLogger.Info($"Carrier Wait Out HS Done");

                return true;
            }
            else
            {
                return false;
            }
        }
        private async void ReportCarrierInstalledToMCS()
        {
            bool BCRIDExist = await WaitBCRReadCompleted();
            if (!BCRIDExist)
            {
                AlarmManager.AddWarning(ALARM_CODES.Try_CarrierInstalledReport_But_BCRID_Not_Exist, Properties.PortID);
                return;
            }
            Utility.SystemLogger.Info($"Carrier Installed Report to MCS");
            SecsMessage install_response = await MCS.SendMsg(EventsMsg.CarrierInstalled(WIPINFO_BCR_ID, Properties.PortID, EPortAutoStatus == AUTO_MANUAL_MODE.AUTO));

        }

        private async Task<bool> WaitBCRReadCompleted()
        {
            Utility.SystemLogger.Info($"Wait BCR Read Completed");

            CancellationTokenSource cst = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (WIPINFO_BCR_ID == "")
            {
                if (cst.IsCancellationRequested)
                    return false;
                await Task.Delay(1);
            }
            Utility.SystemLogger.Info($"BCR Read Completed : {WIPINFO_BCR_ID}");

            return true;

        }

        private void WaitOutSECSReport()
        {
            _ = Task.Factory.StartNew(async () =>
            {
                //確保Wait Out Event Report在 Transfer Completed 之後
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                while (!Carrier_TransferCompletedFlag)
                {
                    if (cts.IsCancellationRequested)
                        break;
                    await Task.Delay(1);
                }
                Carrier_TransferCompletedFlag = false;
                try
                {
                    SecsMessage response = await MCS.SendMsg(EventsMsg.CarrierWaitOut(WIPINFO_BCR_ID, Properties.PortID, ""));//TODO Zone Name ?
                    if (response.IsS9F7())
                        AlarmManager.AddWarning(ALARM_CODES.MCS_CARRIER_WAITOUT_REPORT_FAIL, Properties.PortID);
                }
                catch (Exception ex)
                {
                    AlarmManager.AddWarning(ALARM_CODES.MCS_CARRIER_WAITOUT_REPORT_FAIL, Properties.PortID);
                    Utility.SystemLogger.Info($"Carrier Wait Out Report to MCS - {ALARM_CODES.MCS_CARRIER_WAITOUT_REPORT_FAIL},{ex.Message}");
                }

            });
        }

        public async Task<(bool confirm, ALARM_CODES alarm_code)> CarrierWaitInReply(bool wait_in_accept, int EQ_T_timeout = 5000)
        {

            Utility.SystemLogger.Info($"等待MCS Accept Carrier Wait IN Request..");
            bool timeout = false;
            PROPERTY wait_in_ = wait_in_accept ? PROPERTY.Carrier_WaitIn_System_Accept : PROPERTY.Carrier_WaitIn_System_Refuse;
            string? carrier_wait_in_result_flag_address = PortCIMBitAddress[wait_in_];
            string? carrier_wait_in_reply_address = PortCIMBitAddress[PROPERTY.Carrier_WaitIn_System_Reply];
            EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_wait_in_result_flag_address, true);
            EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_wait_in_reply_address, true);
            Stopwatch sw = Stopwatch.StartNew();
            while (CarrierWaitINSystemRequest)
            {
                if (sw.ElapsedMilliseconds > EQ_T_timeout)
                {
                    timeout = true;
                    break;
                }
                await Task.Delay(1);
            }

            EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_wait_in_reply_address, false);
            EQParent.CIMMemOptions.memoryTable.WriteOneBit(carrier_wait_in_result_flag_address, false);

            return (!timeout, timeout ? ALARM_CODES.CarrierWaitIn_HS_EQ_Timeout : ALARM_CODES.None);

        }

        internal async Task<bool> WaitLoadUnloadRequestON()
        {
            Utility.SystemLogger.Info("Wait_Load/Unload Request ON...");
            CancellationTokenSource cancelWaitCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!LoadRequest && !UnloadRequest)
            {
                if (cancelWaitCts.IsCancellationRequested)
                {
                    Utility.SystemLogger.Warning("Wait Load/Unload Request Bit ON TIMEOUT (20)  when MCS Transfer command downloaded.");
                    AlarmManager.AddAlarm(ALARM_CODES.WAIT_Load_Unload_Request_Bit_ON_When_MCS_Transfering, Properties.PortID);
                    return false;
                }
                await Task.Delay(1);
            }
            Utility.SystemLogger.Info("Load/Unload Request Bit ON.. contiune");
            return true;
        }

        private bool NoTransferNotifyFlag = false;
        private bool CurrentCSTHasTransferTaskFlag = false;

        /// <summary>
        ///等待MCS有下Transfer任務給AGVS取當前Carrier。
        /// </summary>
        /// <param name="cst_id"></param>
        /// <returns></returns>
        private async Task<bool> WaitTransferTaskDownloaded()
        {
            CancellationTokenSource cancelwait = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            while (!CurrentCSTHasTransferTaskFlag)
            {
                if (cancelwait.IsCancellationRequested)
                {
                    Utility.SystemLogger.Warning($"{Properties.PortID} _ Carrier- {WIPINFO_BCR_ID} No body known where to go . No AGV To Transfer....");
                    return false;
                }
                if (NoTransferNotifyFlag)
                {
                    Utility.SystemLogger.Warning($"{Properties.PortID} _ Carrier- {WIPINFO_BCR_ID} MCS NO Transfer Notify. No AGV To Transfer...");
                    NoTransferNotifyFlag = false; //reset flag
                    return false;
                }
                await Task.Delay(1);
            }
            Utility.SystemLogger.Warning($"{Properties.PortID} _ Carrier- {WIPINFO_BCR_ID} AGV Will Transfer this carrier later.");
            CurrentCSTHasTransferTaskFlag = false; //reset flag
            return true;
        }

        internal void CstTransferInvoke()
        {
            CurrentCSTHasTransferTaskFlag = true;
        }

        internal void NoTransferNotifyInovke(string carrier_id, string cstid)
        {
            NoTransferNotifyFlag = true;
            OnMCSNoTransferNotify?.Invoke(this, new Tuple<string, string>(carrier_id, cstid));
        }

        internal void TransferCompletedInvoke(string carrier_id)
        {
            if (WIPINFO_BCR_ID == carrier_id)
                Carrier_TransferCompletedFlag = true;
        }
    }
}
