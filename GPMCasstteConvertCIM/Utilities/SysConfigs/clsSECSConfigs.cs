﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPMCasstteConvertCIM.Utilities.SysConfigs
{
    public class clsSECSConfigs
    {
        /// <summary>
        /// Configure the timeout interval in milliseconds between the primary message sent till to receive the secondary message.
        /// Default value is 45000 milliseconds.
        /// </summary>
        public int T3 { get; set; } = 45000;

        /// <summary>
        /// Configure the timeout interval in milliseconds between the connection state transition from <see cref="ConnectionState.Connecting"/> to <see cref="ConnectionState.Connected"/>.
        /// Default value is 10000 milliseconds.
        /// </summary>
        public int T5 { get; set; } = 10000;

        /// <summary>
        /// Configure the timeout interval in milliseconds between the control message sent till to receive the reply message.
        /// Default value is 5000 milliseconds.
        /// </summary>
        public int T6 { get; set; } = 5000;

        /// <summary>
        /// Configure the timeout interval in milliseconds between the connection state transition from <see cref="ConnectionState.Connected"/> to <see cref="ConnectionState.Selected"/>.
        /// Default value is 10000 milliseconds.
        /// </summary>
        public int T7 { get; set; } = 10000;

        /// <summary>
        /// Configure the timeout interval in milliseconds between the chunk received to next chunk during decoding a <see cref="SecsMessage"/>.
        /// Default value is 5000 milliseconds.
        /// </summary>
        public int T8 { get; set; } = 5000;

        /// <summary>
        /// Configure the timer interval in milliseconds between each <see cref="MessageType.LinkTestRequest"/> request.
        /// Default value is 60000.
        /// </summary>
        public int LinkTestInterval { get; set; } = 60000;

        public int SocketRecieveBufferSize { get; set; } = 8192;
    }
}
