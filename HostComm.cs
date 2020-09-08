//********************************************************************
// Filename: HostComm.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.0
// Status       : c         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 01.05.16
// ___________________________________________________________________
// Description  : Tools di comunicazione con l'esterno 
//                 
//_____________________________________________________________________
// Changes      :
// Date     Author      Description
//
//********************************************************************

using System;
using MhcsLib;
using System.Messaging;
using System.Collections;
using Oracle.DataAccess.Client;
using System.Runtime.InteropServices;
using System.Text;

namespace HostBridge
{
    partial class HostBridge : MhcsLib.SvcApp
    {
        /// <summary>
        /// <para>Invia un messaggio ad AgvCtl</para>
        /// </summary>
        /// <param name="message"></param>
        public void ExecuteSendAgvCtlMessage(string message)
        {
            System.Messaging.Message msg ;

            msg = new System.Messaging.Message();
            lock (hostsendq)
            {
                hostsendq.Send(msg, message.Trim());
            }
        }
        /// <summary>
        /// Invia un messaggio inByte Array a Host, utilizzando Sinec OPC/SinecH1
        /// </summary>
        /// <param name="sendbuffer">Byte Array</param>
        /// <returns></returns>
        public int ExecuteSendHostMessage(byte[] sendbuffer)
        {
            HostOPC.SendMessage(sendbuffer);
            return (0);
        }
        /// <summary>
        /// Invia un messaggio inByte Array a Host, utilizzando Sinec OPC/SinecH1
        /// </summary>
        /// <param name="sendbuffer">Byte Array</param>
        /// <returns></returns>
        public int ExecuteSendHostAck(byte[] sendbuffer)
        {
            HostOPC.SendAck(sendbuffer);
            return (0);
        }
    }
}
