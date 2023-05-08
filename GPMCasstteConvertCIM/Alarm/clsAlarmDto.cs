﻿using GPMCasstteConvertCIM.CasstteConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace GPMCasstteConvertCIM.Alarm
{
    public class clsAlarmDto
    {
        public ALARM_LEVEL Level { get; set; }
        public DateTime Time { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Classify { get; set; } = string.Empty;
        public string EQPName { get; set; } = string.Empty;
        public ALARM_CODES Code { get; set; }
        public int Code_int
        {
            get => (int)Code;
            set
            {
                Code = Enum.GetValues(typeof(ALARM_CODES)).Cast<ALARM_CODES>().FirstOrDefault(c => (int)c == value);
            }
        }
        public double Duration { get; set; }
        public clsAlarmDto(ALARM_CODES code, string classify, string description)
        {
            Code = code;
            Classify = classify;
            Description = description;
        }
        public clsAlarmDto()
        {

        }
    }


    public enum ALARM_CODES
    {
        CONNECTION_ERROR_CONVERT = 4100,
        CONNECTION_ERROR_MCS,
        CONNECTION_ERROR_AGVS,
        HANDSHAKE_ERROR_CARRIER_WAIT_IN,
        HANDSHAKE_ERROR_CARRIER_WAIT_OUT,
        HANDSHAKE_ERROR_PORT_TYPE_CHANGE,
        ALIVE_CLOCK_EQP_DOWN,
        MCS_PORT_IN_SERVICE_REPORT_FAIL,
        MCS_PORT_OUT_SERVICE_REPORT_FAIL,
        MCS_CARRIER_WAITIN_REPORT_FAIL,
        MCS_CARRIER_WAITOUT_REPORT_FAIL,
        MCS_CARRIER_REMOVED_COMPLETED_REPORT_FAIL,
        CARRIER_WAIT_IN_BUT_MCS_DISCONNECT,
        CARRIER_WAIT_IN_BUT_MCS_REJECT,
        CarrierWaitIn_HS_EQ_Timeout,
        CarrierWaitOut_HS_EQ_Timeout,
        CarrierRemovedCompolete_HS_EQ_Timeout,
        TRANSFER_MCS_MSG_TO_AGVS_BUT_AGVS_NO_REPLY,
        AGVS_REPLY_MCS_MSG_BUT_ERROR_WHEN_REPLY_TO_MCS,
        CODE_EXCEPTION_WHEN_TRANSFER_MSG_TO_AGVS,
        EVENT_REPORT_COMPLETED_BUT_ACK_IS_SYSTEM_ERROR_65,
        MXCompHanler_Entitize_Fail,
        MXCompHanler_Open_Fail,
        SYNCMEMDATA_FUNCTION_CODE_ERROR,
    }
}
