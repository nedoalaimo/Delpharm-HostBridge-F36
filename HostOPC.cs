//********************************************************************
// Filename: HostOPC.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.0
// Status       : c         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 01.05.16
// ___________________________________________________________________
// Description  : OPC interface handling 
//                 
//_____________________________________________________________________
// Changes      :
// Date     Author      Description
//
//********************************************************************
using Siemens.Opc.Da;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HostBridge
{
    public static class HostOPC
    {
        public static Server m_Server;
        public static Subscription m_SubscriptionAck = null;
        public static Subscription m_SubscriptionMessage = null;

        public static void InitOPC()
        {
            try
            {
                m_Server = new Server();
                m_Server.Connect("opcda://localhost/OPC.SimaticNET");
            }
            catch(Exception ex)
            {
                // Error on InitOPC
            }
        }
        
        public static void StartSubscription(Subscription sSubscription, string sSubscriptionName,
            string sSubscriptionItem, int cHandleItem, DataChange dCallback)
        {
            try
            {
                if (sSubscription == null)
                {
                    sSubscription = m_Server.CreateSubscription(sSubscriptionName, dCallback);
                }

                sSubscription.AddItem(sSubscriptionItem, (int)cHandleItem);
            }
            catch (Exception ex)
            {
                // Error on StartSubscription
            }
        }
        
        public static void StopSubscription(Subscription sSubscription)
        {
            if (sSubscription != null)
            {
                try
                {
                    m_Server.DeleteSubscription(sSubscription);
                    sSubscription = null;
                }
                catch (Exception ex)
                {
                    // Error on StopSubscription
                }
            }
        }
        
        public static void SendMessage(byte[] rawValue)
        {
            try
            {
                if (rawValue == null)
                {
                    return;
                }

                m_Server.Write("SR:[AMS->MHM]send", rawValue);
            }
            catch (Exception ex)
            {
                // Error on SendMessage
            }
        }
        
        public static void SendAck(byte[] rawValue)
        {
            try
            {
                byte[] ackValue = new byte[10];
                Array.Copy(rawValue, ackValue, 10);
                if (rawValue == null)
                {
                    return;
                }

                m_Server.Write("SR:[AMS<-MHM]send", ackValue);
            }
            catch (Exception ex)
            {
                // Error on SendAck
            }
        }
    }
}
