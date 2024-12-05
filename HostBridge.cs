//********************************************************************
// Filename: HostBridge.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.1
// Status       : ok         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 08.08.16 
// ___________________________________________________________________
// Description  : This program handle all the messages from and to 
//                Host
//  Il la funzione di HostBridge è di collegare AgvCtl con Host, svolge_
//  anche funzioni integrative per la formulazione di messaggi o la
//  creazione di ordini di trasporto.
//  La comunicazione con Host avviene tramite 2 porte UDP (in test)
//  o in OPC/Sinec H1 in RUN
//  La comunicazione con AgvCtl avviene tramite code mq
//
//  HostReceive     - Per la ricezione di messaggi verso AgvCtl
//  HostSend        - Per l'invio di messaggi inviati da AgvCtl
//_____________________________________________________________________
// Changes      :
// Date     Author      Description
// 170817   ral         Sostituzione di PSAVxx con AVCGxx e AVCFxx
// 170817   ral         gestisce l'inibizione lettura codice a barre
// 171220   ral         gestione telegramma IdSup al pick completato
//********************************************************************

#undef DEBUG                                       // Soltanto per ambiente di debug

using System;
using System.Data;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MhcsLib;
using Oracle.DataAccess.Client;
using System.Messaging;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using Siemens.Opc.Da;

namespace HostBridge
{
    partial class HostBridge : MhcsLib.SvcApp
    {
        enum HostBridgeState
        {
            OpenConnection,
            Connected,
            CloseConnection,
            Waiting
        }

        #region <HosBridge Log Message Definitions>
        //I HOSTBRIDGE_MSGSENT "Message sent" Data    
        static public MhcsLib.MessageDef HOSTBRIDGE_MSG_SENT = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_SENT", "Message sent", MhcsLib.MsgArg.Data);
        //I HOSTBRIDGE_ACK_RCVD "Message received" Data  
        static public MhcsLib.MessageDef HOSTBRIDGE_ACK_SENT = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_SENT", "Ack sent", MhcsLib.MsgArg.Data);
        //I HOSTBRIDGE_RCVD "Message received" Data
        static public MhcsLib.MessageDef HOSTBRIDGE_MSG_RCVD = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_RCVD", "Message received", MhcsLib.MsgArg.Data);
        //I HOSTBRIDGE_RCVD "Ack received" Data
        static public MhcsLib.MessageDef HOSTBRIDGE_ACK_RCVD = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_RCVD", "Ack received", MhcsLib.MsgArg.Data);
        //E HOSTBRIDGE_ERR_CREATE_QUEUE "Error creating host queue" QueuePath Error   
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_CREATE_QUEUE = new MhcsLib.MessageDef(MhcsLib.MsgSev.Error, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_CREATE_QUEUE", "Error creating queue", MhcsLib.MsgArg.QueuePath, MhcsLib.MsgArg.Error);
        //E HOSTBRIDGE_ERR_RECEIVE_QUEUE "Error receiving from host queue" Error    
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_RECEIVE_QUEUE = new MhcsLib.MessageDef(MhcsLib.MsgSev.Error, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_RECEIVE_QUEUE", "Error receiving from queue", MhcsLib.MsgArg.Error);
        //E HOSTBRIDGE_ERR_SEND_QUEUE "Error sending to host queue" Error    
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_SEND_QUEUE = new MhcsLib.MessageDef(MhcsLib.MsgSev.Error, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_SEND_QUEUE", "Error sending to queue", MhcsLib.MsgArg.Error);
        //E  PTIBRIDGE_ERR_MESSAGE "An error was encountered processing message" Error    
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_MESSAGE = new MhcsLib.MessageDef(MhcsLib.MsgSev.Error, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_MESSAGE", "An error was encountered processing message", MhcsLib.MsgArg.Error);
        //E HOSTBRIDGE_ERR_REG_CHANGE "Error registering database change notification"     
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_REG_CHANGE = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_REG_CHANGE", "Error registering database change notification", MhcsLib.MsgArg.CommandSource, MhcsLib.MsgArg.Error);
        //I HOSTBRIDGE_ORDER_RECV "Host order received" Data    
        static public MhcsLib.MessageDef HOSTBRIDGE_ORDER_RECV = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ORDER_RECV", "Host order received", MhcsLib.MsgArg.Data);
        //E HOSTBRIDGE_ERR_COMMAND "Error executing database command"     
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_COMMAND = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_COMMAND", "Error executing database command", MhcsLib.MsgArg.CommandSource, MhcsLib.MsgArg.Error);
        //E HOSTBRIDGE_ORACLE_EXCEPTION "Oracle Exception"     
        static public MhcsLib.MessageDef HOSTBRIDGE_ORACLE_EXCEPTION = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ORACLE_EXCEPTION", "Error executing ORACLE command", MhcsLib.MsgArg.CommandSource, MhcsLib.MsgArg.Error);
        //E HOSTBRIDGE_ERR_INVALID_MESSAGE "Invalid message received"     
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_INVALID_MESSAGE = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_INVALID_MESSAGE", "Invalid message received", MhcsLib.MsgArg.Error);
        //E HOSTBRIDGE_ERR_STATUS_CODE "Invalid status code redceived"     
        static public MhcsLib.MessageDef HOSTBRIDGE_ERR_STATUS_CODE = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_ERR_STATUS_CODE", "Invalid status code received", MhcsLib.MsgArg.Error);
        //I HOSTBRIDGE_CONN_CLOSED "Host connection closed"     
        static public MhcsLib.MessageDef HOSTBRIDGE_CONN_CLOSED = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_CONN_CLOSED", "HostBridge connection closed");
        //D HOSTBRIDGE_DBG_LOG "Debugging Log"     
        //static public MhcsLib.MessageDef HOSTBRIDGE_DBG_LOG = new MhcsLib.MessageDef(MhcsLib.MsgSev.Trace, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_DBG_LOG", "Data sent", MhcsLib.MsgArg.AGV, MhcsLib.MsgArg.Data);
        static public MhcsLib.MessageDef HOSTBRIDGE_LOG = new MhcsLib.MessageDef(MhcsLib.MsgSev.Information, MhcsLib.MsgFac.HostBridge, "HOSTBRIDGE_TRC", "Tracing", MhcsLib.MsgArg.Data);

        #endregion
        #region <Lock objects definitions>
        static public readonly object LockObject = new object();
        //static public readonly object LocToSend = new object();
        static public readonly object CountersLock = new object();
        #endregion
        #region <Definizione contatori e temporizzatori>

        static long timercounter = 1;            // Intervallo general purpose
        static long retrytimercounter = 1;            // Intervallo per la ripetzione dell'invio
        static long retrycounter = 0;            // Conta le ripetizioni
        static long wchdogcount = 1;            // Intervallo per la gestione del wotchdog
        static long shorttimerinterval = 3;            // Intervallo breve di ripezione
        static long longtimerinterval = 20;           // Intervallo lungo di ripetizione 
        int numshortdelay = 3;            // Numero di tentativi di ripetizioni, inattesa di ack, a intervalli brevi


        #endregion
        #region <Definizioni per uso code di messaggi>
        string servicequeuepathroot, qpath;
        System.Messaging.ReceiveCompletedEventHandler hostsendhandler;          // Handler per la gestione della coda di messaggi da AgvCtl verso HostBridge
        System.Messaging.ReceiveCompletedEventHandler sh1receivehandler;        // Handler per coda messaggi da Host verso HostBridge
        System.Messaging.ReceiveCompletedEventHandler sh1ackreceivehandler;     // Handler per coda ack da Host verso HostBridge
        System.Messaging.ReceiveCompletedEventHandler loghandler;          	    // Handler per la gestione della coda dei log
        System.Messaging.MessageQueue hostsendq, hostrecvq;                     // Code messaggi tra HostBridge e AgvCtl
        System.Messaging.MessageQueue logrecvq, logsendq;                     									// Code dei log
        System.Messaging.MessageQueue sh1recvq, sh1sendq;                       // Code messaggi tra HostBridge e Host
        System.Messaging.MessageQueue sh1recvackq, sh1sendackq;                 // Code ack tra HostBridge e Host
        string hostrecvqname, hostsendqname;                                    // Identificativo coda privata
        string logrecvqname, logsendqname;                                      // Per la ricezione e l'invio di allarmi 
#if false
            string agvctlrecvqname, agvctlsendqname; 
#endif
        string sh1recvqname, sh1sendqname;                                      //Code usate a scopo di test
        string sh1recvackqname, sh1sendackqname;                                //Code usate a scopo di test

        UdpClient msgclient;
        UdpClient ackclient;
        IPEndPoint LocalIpMsgEndPoint;
        IPEndPoint LocalIpAckEndPoint;

        #endregion

        int isSent;
        int psSent;
        int hosttimerinterval;
        int numretrydelay;
        int wchdogtimeout;

        OracleDataReader reader;
        string hostlogconnectstring;
        Oracle.DataAccess.Client.OracleConnection hostlogconnection;

        HostBridge()
            : base()
        {
            ServiceName = "HostBridge";
        }

        static void Main(string[] args)
        {
            HostBridge hostbridge;
            System.ServiceProcess.ServiceBase[] servicestorun;

            // Instantiate the service process
            hostbridge = new HostBridge();
            servicestorun = new System.ServiceProcess.ServiceBase[] { hostbridge };

            // Start the application
            if (args.GetLength(0) == 0)
            {
                System.ServiceProcess.ServiceBase.Run(servicestorun);  // If no arguments, start as a service applications
            }
            else
            {
                hostbridge.StartAsConsole(args);                       // If argument, start as a console process
            }
        }
        #region Start Sistema

        /// <summary>
        /// Attivasione del processo
        /// </summary>
        protected override void AppStartup()
        {
            base.AppStartup();                      // Definitions and base Application startup

            #region Inizializza temporizzatori

            int retrydelay = 0;
            int numshortdelay = 3;            // Numero massimo di ripetizioni a intervali brevi
            int connectionwaittime = 600;
            int timerinterval;

            timerinterval = System.Convert.ToInt32(MhcsLib.App.GetSetting("TimerInterval"));
            //retrytimerinterval    = System.Convert.ToInt32(MhcsLib.App.GetSetting("RetryTimerInterval"));  
            retrydelay = System.Convert.ToInt32(MhcsLib.App.GetSetting("RetryDelay"));
            numshortdelay = System.Convert.ToInt32(MhcsLib.App.GetSetting("NumShortDelay"));
            connectionwaittime = System.Convert.ToInt32(MhcsLib.App.GetSetting("ConnectionWaitTime"));
            wchdogtimeout = 5;    //System.Convert.ToInt32(MhcsLib.App.GetSetting("WchDogTimeout"));

            #endregion
            #region Inizializzzazione connessione HOST/OPC e AgvCtl

            MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "AppStartup(), Preparazione communication event handler");
            HostOPC.InitOPC();
            HostOPC.StartSubscription(HostOPC.m_SubscriptionMessage, "MessageMHM",
                        "SR:[AMS<-MHM]receive", 1, OnOPCDataChange);    // 1 -> Messaggio MHM
            HostOPC.StartSubscription(HostOPC.m_SubscriptionAck, "AckMHM",
                        "SR:[AMS->MHM]receive", 2, OnOPCDataChange);    // 2 -> Ack MHM 

            AgvCtlStartReceive();   // Start Thread handling messages coming from AgvCtl

            #endregion
            #region Connessione con Oracle DB

            //connectstate          = HostBridgeState.OpenConnection;
            hostlogconnectstring = MhcsLib.App.GetSetting("HostConnectString");
            hostlogconnection = null;
            hostlogconnection = new OracleConnection(hostlogconnectstring);

            #endregion
            #region Attiva il logging per la gestione degli allarmi
            LogStartReceive();
            #endregion
#if DEBUG
                SendAlarm("HostBridge", "Information", "Ripartenza del sistema NODE in modalità  DEBUG");
#else
            SendAlarm("HostBridge", "Information", "Ripartenza del sistema NODE");
#endif
            #region Attiva connettività con ambiente di test
            if (TESTMODE)               // Parte di codice usata soltanto i modalità TEST
            {
                //HostStartReceive();     // Start receive message from Host event handling
                //SimHostStartReceive();  // Start receive message from HostSimulator event handling via UDP  
                //justForTest();
            }
            #endregion

            GetHostControlBoard();          // Legge valori globali di sistema
            AppStartTimer(timerinterval);   // Fa partire il timer per il watchdogging
        }
        /// <summary>
        /// Application Shutdown
        /// </summary>
        protected override void AppShutdown()
        {
            if (hostlogconnection != null)
            {
                if (hostlogconnection.State == System.Data.ConnectionState.Open)
                {
                    hostlogconnection.Close();
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_CONN_CLOSED);
                }
                hostlogconnection.Dispose();
            }

            base.AppShutdown();
        }
        /// <summary>
        /// Routine activated each time the interval is espired
        /// In app.config TimeInterval è settato a 100, dove 100 sono il numero di tick per un secondo
        /// quindi se TimerInterval è settato a 100, questa app vien richiamata ogni secondo
        /// </summary>
        protected override void AppTimerExpired()
        {
            string[] tokens = new string[20];
            string msgsnd = "";
            Byte[] MsgBytebuffer = new byte[512];
            int ps;
            //MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "AppTimerExpired");

            base.AppTimerExpired();

            //Console.WriteLine(System.DateTime.Now.ToString("HH:mm:ss:ffffff")+"  Continuo");
            ++timercounter;
            ++wchdogcount;
            ++retrytimercounter;

            // La costante TimerInterval definisce l'unità di tempo, per es 100 = 100 tick = 1 secondo
            // dopo 1 decimo di secondo
            if (timercounter % 10 == 0)
            {
                //Console.WriteLine(System.DateTime.Now.ToString("HH:mm:ss:ffffff")+"  Ogni 10 tick");
            }

            // dopo un secondo
            if (timercounter % 100 == 0)
            {
                //Console.WriteLine(System.DateTime.Now.ToString("HH:mm:ss:ffffff") + "  Ogni 100 tick");
            }

            // Ogni 30 secondi, Node effettua alcuni controlli, sullo stato del sistema e sulle code di messaggi
            //-------------------------------------------------------------------------------------------------

            if (timercounter % 3000 == 0)
            {
                #region Parte dichiarativa

                int[] tokensInt;
                int pa = 0;
                int AckIc = 0;
                int Di = 0;
                string ackmsg = "";

                #endregion
                #region Ogni 10 secondi, verifica se il coda vi è un ack di Host non ancoda processato (non probabile)

                tokensInt = GetNodeAckToSend(0);                                  //Recupera un ack da inviare
                AckIc = tokensInt[0];
                pa = tokensInt[1];

                if (pa != 0)
                {
                    Di = tokensInt[3];
                    ackmsg = PrepareHostAck(AckIc, pa, 0, Di);                     //Prepara l'Ack da inviare (parametri numerici)
                    ExecuteSendHostAck(SwapByte(PackHostAck(ackmsg), 512));        //Invia l'Ack riordinando i byte
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG,
                                        "ACK SENT: ( " + ackmsg + " )");
                    //InserRecordInHostLog("Node", "Ack", ackmsg);
                    if (pa != 0) DequeueAckToHost(pa);
                }
                //Scoda l'ack inviato
                #endregion

                #region Verifica se in coda vi è un messaggio da Host non ancora processato (non probabile)

                tokens = GetOldestHostMessageReceived();                        //Recupera il più vecchio comando Host da eseguire
                if (tokens[2] == "1")
                {
                    ps = System.Convert.ToInt32(tokens[1]);
                    ProcessCommandFromHost(tokens);                            //Processa il messaggio 
                    DequeueMessageFromHost(ps);                                //Cancella dalla coda il messaggio processato
                }

                #endregion
            }

            // Check for wotchdog timeout
            // Ogni volta che arriva un evento da gestire, il wchdog viene azzerato
            // questo timer serve a face qualcosa nel caso in cui non arrivasse nulla da Host per un tempo stabilito wchsogtimeout
            // Verifica se quacosa può essere inviato a Host
            // Invia i telegrammi più vecchi nella tabella dei telegrammi verso Host
            if (wchdogcount % wchdogtimeout == 0)
            {
                lock (LockObject)
                {
                    GetHostControlBoard();                                          // Recupera i dati dei contatori dall'Host Control Board
                                                                                    //if (FromHostAckPending == 0 && GetSystemStatus() == "Run")      // Soltanto se il sistema è in Run e no Ack pending
                    if (FromHostAckPending == 0)      // Soltanto se il sistema è in Run e no Ack pending
                    {
                        tokens = GetOldestNodeMessageToSend();                      // Controlla se in coda, vi sono telegrammi da inviare a Host
                        if (tokens[2] == "1")                                       // Corrisponde al campo Ra, che deve essere sempre a 1
                        {
                            MsgBytebuffer = PackHostMessage(tokens);
                            MsgBytebuffer = SwapByte(MsgBytebuffer, 512);     // Ripristina il msg originale
                            ExecuteSendHostMessage(MsgBytebuffer);                  // Invia il messaggio
                            msgsnd = tokens[0];
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                if (tokens[i] != null) msgsnd += "," + tokens[i];
                            }
                            MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG SENT: ( " + msgsnd + " )");
                            //InserRecordInHostLog("Node", "Msg", msgsnd);
                            FromHostAckPending = 1;                                // In attesa di Ack da Host
                            SetHostAckPending(1);
                            RetryCounter = 1;
                        }
                    }
                    wchdogcount = 0;
                }
            }
            // Verifica se il cronometro per le ritrasmissioni RetryCounter, ha raggiunto la soglia di tempo attesa
            // se FromHostAckPending é 0, questa sezione non viene eseguita, significa che non vi sono ritrasmissioni attive
            // Ogni volta che viene inviato un messaggio a Host, RetryCounter viene settato a 1
            // Ogni volta che viene ricevuto un ack da Host il RetryCunter viene resettato
            // se >0 < minimo numero di retry, le ritrasmissioni hanno luogo a intervalli brevi
            // se > min numero di retry, le ritrasmissioni hanno luogo a intervalli lunghi
            // Il contatore delle ritrasmissioni viene resettato soltanto al ricevitmento dell'Ack da Host

            //Console.WriteLine(System.DateTime.Now.ToString("HH:mm:ss:ffffff") + "  RetryCounter="+RetryCounter);
            if (FromHostAckPending != 0)                              // Il ciclo delle ritrasmissioni è stato attivato       
            {
                // Se il flag di telegrammi da Host è a 1, viene ritrasmesso l'ultimo telegamma a Host soltanto 
                // ma soltanto se la specifica soglia di ripetizione è stata raggiunta

                if (RetryCounter <= NUM_SHORT_RETRY && retrytimercounter % SHORT_DELAY == 0 ||
                    RetryCounter > NUM_SHORT_RETRY && retrytimercounter % LONG_DELAY == 0)                      // Il sistema è in attesa di un ack da host
                {
                    lock (LockObject)
                    {
                        if (RetryCounter > NUM_SHORT_RETRY) SetHostCommStatus("DOWN");                          // Dopo tre tentativi la comunicazione con
                                                                                                                // Host, viene considerata Down
                                                                                                                // Verrà ripristinata alla ricezione del 
                                                                                                                // Primo Ack
                        RetryCounter++;
                        if (RetryCounter > MaxIcCounter) RetryCounter = NUM_SHORT_RETRY + 1;

                        //SendMsgToHost();
                        tokens = GetOldestNodeMessageToSend();              // Controlla se in coda, vi sono telegrammi da inviare a Host
                        if (tokens[2] == "1")                               // Corrisponde al campo Ra, che deve essere sempre a 1
                        {
                            MsgBytebuffer = PackHostMessage(tokens);
                            MsgBytebuffer = SwapByte(MsgBytebuffer, 512);                   // Ripristina il msg originale
                            ExecuteSendHostMessage(MsgBytebuffer);                          // Invia il messaggio
                            msgsnd = tokens[0];
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                if (tokens[i] != null) msgsnd += "," + tokens[i];
                            }
                            MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG SENT: ( " + msgsnd + " )");
                           // InserRecordInHostLog("Node", "Msg", msgsnd);
                        }
                    }
                }
            }
        }
        #endregion
        #region Gestione dei messaggi da e verso OPC/Host

        /// <summary>
        /// Attiva il thread per la ricezione da HostBridge sulla coda HostReceive
        /// </summary>
        void HostStartReceive()
        {
            servicequeuepathroot = MhcsLib.App.GetSetting("ServiceQueuePathRoot");
            qpath = "";

            try
            {
                // Queue used to send messages to Host (Emulating Sinec H1) (Sh1Send)
                //---------------------------------------------------------------------------
                sh1sendqname = "";
                try
                {
                    sh1sendqname = Tags.GetString("HostBridge" + ".Settings.Sh1SendQueue");
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1sendqname + ", Definita");
                }
                catch
                {
                }
                if (sh1sendqname != "" && sh1sendqname != null)
                {
                    sh1sendq = new System.Messaging.MessageQueue();
                    qpath = servicequeuepathroot + sh1sendqname;
                    sh1sendq.Path = qpath;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1sendqname + ", Attivata");
                }

                // Queue used to send Ack to Host (Emulating Sinec H1) (Sh1AckSend)
                //---------------------------------------------------------------------------
                sh1sendackqname = "";
                try
                {
                    sh1sendackqname = Tags.GetString("HostBridge" + ".Settings.Sh1SendAckQueue");
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1sendackqname + ", Definita");
                }
                catch
                {
                }
                if (sh1sendackqname != "" && sh1sendackqname != null)
                {
                    sh1sendackq = new System.Messaging.MessageQueue();
                    qpath = servicequeuepathroot + sh1sendackqname;
                    sh1sendackq.Path = qpath;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1sendackqname + ", Attivata");
                }

                // Queue used to receive messages from Host (Emulating Sinec H1) (Sh1Receive)
                //----------------------------------------------------------------------
                sh1recvqname = "";
                try
                {
                    sh1recvqname = Tags.GetString("HostBridge" + ".Settings.Sh1RecvQueue");
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1recvqname + ", Definita");
                }
                catch
                {
                }
                if (sh1recvqname != "" && sh1recvqname != null)
                {
                    sh1recvq = new System.Messaging.MessageQueue();
                    qpath = servicequeuepathroot + sh1recvqname;
                    sh1recvq.Path = qpath;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1recvqname + ", Attivata");
                }

                // Queue used to receive messages from Host (Emulating Sinec H1) (Sh1Receive)
                //----------------------------------------------------------------------
                sh1recvackqname = "";
                try
                {
                    sh1recvackqname = Tags.GetString("HostBridge" + ".Settings.Sh1RecvAckQueue");
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1recvackqname + ", Definita");
                }
                catch
                {
                }
                if (sh1recvackqname != "" && sh1recvackqname != null)
                {
                    sh1recvackq = new System.Messaging.MessageQueue();
                    qpath = servicequeuepathroot + sh1recvackqname;
                    sh1recvackq.Path = qpath;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + sh1recvackqname + ", Attivata");
                }
            }
            catch (System.Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "HostStartReceive", ex.Message);
                throw /*(new MhcsException(AGVCTL_ERR_CREATE_HOST_QUEUE, qpath, ex.Message))*/;
            }

            if (sh1recvqname != "" && sh1recvqname != null)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Thread OnHostMessageReceived su " + sh1recvqname + ", Creato");
                sh1receivehandler = new System.Messaging.ReceiveCompletedEventHandler(OnHostMessageReceived);
                sh1recvq.ReceiveCompleted += sh1receivehandler;
                sh1recvq.BeginReceive();

                //Log(AMSBRG_STARTUP_PROGRESS, "AgvCtl queue receive started");
            }
            if (sh1recvackqname != "" && sh1recvackqname != null)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Thread OnHostAckReceived su " + sh1recvackqname + ", Creato");
                sh1ackreceivehandler = new System.Messaging.ReceiveCompletedEventHandler(OnHostAckReceived);
                sh1recvackq.ReceiveCompleted += sh1ackreceivehandler;
                sh1recvackq.BeginReceive();

                //Log(AMSBRG_STARTUP_PROGRESS, "AgvCtl queue receive started");
            }
        }
        /// <summary>
        /// Legga gli eventi di dati pronti da leggere in OPC alle chiamate delle funzione
        /// per la gestione di messaggi e ack
        /// </summary>
        /// <param name="DataValues"></param>
        public void OnOPCDataChange(IList<DataValue> DataValues)
        {
            try
            {
                foreach (DataValue value in DataValues)
                {
                    if (value.Error != 0)
                    {
                        // ERRORE!!!
                        return;
                    }
                    if (value.Value.GetType() != typeof(byte[]))
                    {
                        // Tipo non corrispondente
                        return;
                    }
                    if (((byte[])value.Value).Length != 512 && value.ClientHandle == 1)
                    {
                        // Lunghezza errata
                        return;
                    }
                    switch (value.ClientHandle)
                    {
                        case 1: // MHM Message
                            HandleHostMessage((byte[])value.Value);
                            break;
                        case 2: // Ack
                            HandleHostAck((byte[])value.Value);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Error OnOPCDataChange
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "OnOPCDataChange", ex.Message);
            }
        }
        /// <summary>
        /// Processa il messaggio ricevuto da Host
        /// </summary>
        /// <param name="message"></param>
        /// 
        public void HandleHostMessage(byte[] MsgBytebuffer)
        {
            #region Parte dichiarativa
            int ps;             //Progressivo di send
            int pa;             //Progressivo di ack
            int Di;             //Diagnostico (0 = ok)
            int AckIc = 0;
            string stric;
            string strpa;
            string strdi;
            string ackmsg;
            string[] tokens = new string[20];
            int[] tokensInt;
            int TokenLenght = 0;
            #endregion

            #region Parsifica e accoda il messaggio

            MsgBytebuffer = SwapByte(MsgBytebuffer, 512);                       //Riordina Byte in word
            tokens = UnpackHostMessage(MsgBytebuffer).Split(',');               //Trasforma il messaggio in token (Più gestibile!!)
            TokenLenght = tokens.Length;                                        //Calcola il numero dei token nel messaggio
            Di = IsHostMessageValid(tokens, TokenLenght);                       //Verifica il messaggio e restituisce il diagnostico in Di

            if (Di == VDATA)                                                    //Se il messaggio va bene, può essere accodato per l'elaborazione
            {
                if (TokenLenght != 5)                                           //Tutti i messaggi tranne RM hanno 5 token
                {
                    //MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG RECEIVED: ( " + tokens[0].PadLeft(5, '0') + "," + tokens[1].PadLeft(5, '0') + "," + tokens[2] + "," + tokens[3] + "," +
                    //    tokens[4] + "," + tokens[5] + "," + tokens[6] + "," + tokens[7] + "," + tokens[8] + "," +
                    //    tokens[9].Trim() + "," + tokens[10].PadLeft(5, '0') + " )");

                    Di = QueueMsgFromHost(tokens[0], tokens[1], tokens[2], tokens[3], tokens[4],
                                          tokens[5], tokens[6], tokens[7], tokens[8], tokens[9], tokens[10]);
                }
                else                                                            //Si tratta di un messaggio di tipo "RI" "MM" o "RS"
                {
                    //MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG RECEIVED: ( " + tokens[0].PadLeft(5, '0') + "," + tokens[1].PadLeft(5, '0') + "," + tokens[2] + "," +
                    //    tokens[3].Trim() + "," + tokens[4].PadLeft(5, '0') + " )");

                    Di = QueueMsgFromHost(tokens[0], tokens[1], tokens[2], tokens[3], tokens[4]);
                }
            }
            #endregion
            #region Prepara l'Ack al messaggio ricevuto e lo accoda

            AckIc = 0;//GetNextNodeIc();                                          //Calcola l'Ic per l'Ack da accodare
            strpa = System.Convert.ToString(tokens[1]);
            pa = System.Convert.ToInt32(strpa);                                //Recupera il Pa dal Ps del messaggio ricevuto
                                                                               //if (IsValidNodeIc(AckIc))                                             //L'Ic è valido se non esiste già in un messaggio accodato
                                                                               //{
            stric = System.Convert.ToString(0);
            strdi = System.Convert.ToString(Di);
            QueueAckToHost(stric, strpa, "0", strdi, stric);                    //Accoda l'Ack
            //}

            #endregion
            #region Invia l'Ack più vecchio in coda (LIFO)

            tokensInt = GetNodeAckToSend(0);                                  //Recupera un ack da inviare
            AckIc = tokensInt[0];
            pa = tokensInt[1];
            Di = tokensInt[3];
            ackmsg = PrepareHostAck(AckIc, pa, 0, Di);                     //Prepara l'Ack da inviare (parametri numerici)
            ExecuteSendHostAck(SwapByte(PackHostAck(ackmsg), 512));             //Invia l'Ack riordinando i byte
            MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG,
                              "ACK SENT: ( " + ackmsg + " )");
            //InserRecordInHostLog("Node", "Ack", ackmsg);
            if (pa != 0) DequeueAckToHost(pa);                                   //Scoda l'ack inviato
            #endregion 
            #region Processa il più vecchio messaggio Host in coda (LIFO)

            tokens = GetOldestHostMessageReceived();                            //Recupera il più vecchio comando Host da eseguire
            //msg         = tokens[3];
            //strps       = tokens[1];
            ps = System.Convert.ToInt32(tokens[1]);
            ProcessCommandFromHost(tokens);                                     //Processa il messaggio 
            DequeueMessageFromHost(ps);                                         //Cancella dalla coda il messaggio processato

            #endregion

        }
        /// <summary>
        /// Processa l'Ack ricevuto da Host
        /// </summary>
        /// <param name="message">"[Ic][PA][RA][DI][IC1]"</param>
        private void HandleHostAck(byte[] MsgBytebuffer)
        {
            #region Parte dichiarativa

            string[] tokens = new string[20];
            bool AckValid = true;

            #endregion
            #region Parsifica Ack
            SetHostCommStatus("UP");                                                    // La comunicazione con Host è Su
            MsgBytebuffer = SwapByte(MsgBytebuffer, 10);                                // Da il giusto verso ai byte nelle word del messaggio
            tokens = GetHostAck(MsgBytebuffer).Split(',');                              // Trasforma il messaggio in token (Più gestibile)
            //MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "ACK RECEIVED: ( " + tokens[0] + "," + tokens[1] + "," + tokens[2] + "," + tokens[3] + "," + tokens[4] + " )");
            //InserRecordInHostLog("MhM ", "Ack", tokens[0] + "," + tokens[1] + "," + tokens[2] + "," + tokens[3] + "," + tokens[4]);

            if (tokens[0] != tokens[4])                                                  // Controlla se è un messaggio completo
            {
                //MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG,
                //    "ACK RECEIVED: ( " + "Ack non valido: Ic != Ic1" + " )");
                AckValid = false;
            }
            else if (tokens[3] != "0")                                                  // Se non è un ack con errore
            {
                //MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG,
                //    "ACK RECEIVED: ( " + "Ack non valido: Di = " + tokens[3] + " )");
                AckValid = false;
            }
            #endregion
            #region Processa Ack
            if (AckValid) ProcessAckFromHost(tokens);                                  // Se tutto ok
            #endregion
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">"[Ic][PA][RA][DI][IC1]"</param>
        private void ProcessAckFromHost(string[] tokens)
        {
            string Udc = "";
            string Dst = "";
            long Tid = 0;

            try
            {

                FromHostAckPending = 0;                                             // Resetta il flag di attesa ack da Host
                SetHostAckPending(0);
                RetryCounter = 0;                                                   // Resetta il contatore di retry

                Tid = IsATransportAck(System.Convert.ToInt32(tokens[1]));           // Verifica se si tratta dell'Ack di un trasporto

                if (Tid != 0)
                {
                    GetTransportData(Tid, ref Udc, ref Dst);                        // Recupera i dati del trasporto
                    if (GetTransportPhase(Tid) == 3)                                // Se il trasporto è stato completato
                    {
                        DeleteOrderInMaster(Tid);                                   // Storicizza e cancella il trasporto
                    }
                }
                DequeueMessageToHost(System.Convert.ToInt32(tokens[1]));          // scoda il messaggi di Host  
            }
            catch (Exception ex)
            {
                // Error on process ack from Host
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "ProcessAckFromHost", ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokens"></param>
        private void ProcessCommandFromHost(string[] tokens)
        {
            OracleDataReader reader = null;

            string sndmessage = "";                     //Il messaggio da inviare a Host (senza testa e coda)
            string StrTid = "", Src = "", Dst = "", Udc = "", StrSts = "";
            int Tid = 0, Sts = 0;
            string[] ListOfSMMessages = new string[50];
            int SMMessageIdx = 0;
            int Priority = 0;

            switch (tokens[3])
            {
                case "RM":
                    {
                        Priority = Convert.ToInt32(tokens[8]);
                        Priority = (Priority % 2 == 0) ? Priority = 0 : Priority = 1;             // Priorita' indicata dal bit 0 della word id stato
                        tokens[8] = Convert.ToString(Priority);

                        //Crea il trasporto nel master dei trasporti
                        //                 [orderid], [loadid],   [source], [dest],    [prio],     [Phase] {Phase is 0 = Ready}
                        //-----------------------------------------------------------------------------------------------------
                        CreateOrderInMaster(tokens[4], tokens[5], tokens[6], tokens[7], tokens[8], "0");

                        //Invia il trasporto ad AgvCtl
                        //----------------------------
                        //                       [RM]    [OrdId]           [LoadId]          [Src]             [Dst]             [Ph]   [Pri]           
                        ExecuteSendAgvCtlMessage("RM," + tokens[4] + "," + tokens[5] + "," + tokens[6] + "," + tokens[7] + "," + "0," + tokens[8]);

                    }
                    break;
                case "RS":  //Prepara le informazioni relative a SS (Stato del sistema) e accoda il messaggio da inviare a Host
                    {
                        sndmessage = PrepareToQueueSSMessage();                     //Raccoglie tutti i dati necessari al messaggio per Host                                       
                        QueueSMMsgToHost(sndmessage);                               //Salva il messaggio nel DB
                    }
                    break;
                case "RI":  // ral 171201  changed
                    {
                        //Prepara le informazioni relative a IR (Identificativi supporto) e accoda il messaggio da inviare a Host 
                        sndmessage = PrepareToQueueSIMessage("AVCG01", "", "");         //Raccoglie tutti i dati necessari al messaggio per Host
                        QueueSMMsgToHost(sndmessage);                               //Salva il messaggio nel DB
                        sndmessage = PrepareToQueueSIMessage("AVCG02", "", "");
                        QueueSMMsgToHost(sndmessage);
                        sndmessage = PrepareToQueueSIMessage("AVCF01", "", "");
                        QueueSMMsgToHost(sndmessage);
                        sndmessage = PrepareToQueueSIMessage("AVCF02", "", "");
                        QueueSMMsgToHost(sndmessage);
                        sndmessage = PrepareToQueueSIMessage("", "00000000", "");       //Fine lista messaggi
                        QueueSMMsgToHost(sndmessage);
                    }
                    break;
                case "MM":
                    {
                        //Per ogni ordine di trasprto nella tabella master, Prepara le informazioni relative a SM (stato delle missioni) 
                        //e e accoda i messaggi da ivniare a Host 
                        //string message = "";
                        Oracle.DataAccess.Client.OracleCommand cmd;
                        System.DateTime TlgTime = System.DateTime.Now;
                        this.connection = Connection;
                        cmd = new Oracle.DataAccess.Client.OracleCommand();
                        cmd.Connection = Connection;

                        try
                        {
                            // Recupera il valore corrente di Ic e lo aggiorna o incrementadolo o resettandolo a 1
                            //------------------------------------------------------------------------------------
                            cmd.CommandText = @"SELECT *" +
                                                @" FROM ""TrOrderMaster""";
                            reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("TrOrderNr"))) Tid = (int)reader.GetDecimal(reader.GetOrdinal("TrOrderNr"));
                                if (!reader.IsDBNull(reader.GetOrdinal("UDC"))) Udc = reader.GetString(reader.GetOrdinal("UDC"));
                                if (!reader.IsDBNull(reader.GetOrdinal("SourceName"))) Src = reader.GetString(reader.GetOrdinal("SourceName"));
                                if (!reader.IsDBNull(reader.GetOrdinal("DestName"))) Dst = reader.GetString(reader.GetOrdinal("DestName"));
                                if (!reader.IsDBNull(reader.GetOrdinal("TrOrderState"))) Sts = (int)reader.GetDecimal(reader.GetOrdinal("TrOrderState"));
                                StrTid = System.Convert.ToString(Tid);
                                StrSts = System.Convert.ToString(Sts);

                                ListOfSMMessages[SMMessageIdx++] = StrTid + "," + Udc + "," + Src + "," + Dst + "," + StrSts;

                            }
                            reader.Close();
                            for (int i = 0; i < SMMessageIdx; i++)
                            {
                                sndmessage = PrepareToQueueSMMessage(ListOfSMMessages[i].Split(',')[0],
                                                                    ListOfSMMessages[i].Split(',')[1],
                                                                    ListOfSMMessages[i].Split(',')[2],
                                                                    ListOfSMMessages[i].Split(',')[3],
                                                                    ListOfSMMessages[i].Split(',')[4]);
                                QueueSMMsgToHost(sndmessage);
                            }
                            sndmessage = PrepareToQueueSMMessage("0", "", "", "", "0");          // End sequence
                            QueueSMMsgToHost(sndmessage);
                        }
                        catch (Exception ex)
                        {
                            // Error on process command from Host
                            MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                        }

                    }
                    break;
                default:
                    break;
            }
        }

        #endregion
        #region Gestione dei messaggi da e verso AgvCtl

        /// <summary>
        /// Attiva il thread per la ricezione di messaggi inviati da AgvCtl sulla coda privata HostSend
        /// </summary>
        void AgvCtlStartReceive()
        {
            servicequeuepathroot = MhcsLib.App.GetSetting("ServiceQueuePathRoot");
            qpath = "";

            try
            {
                // Queue used to receive messages from AgvCtl (HostReceive)
                //-------------------------------------------------------------------
                hostrecvqname = "";
                try
                {
                    hostrecvqname = Tags.GetString("AgvCtl" + ".Settings.HostReceiveQueue");
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + hostrecvqname + ", Definita");
                }
                catch
                {
                }
                if (hostrecvqname != "" && hostrecvqname != null)
                {
                    hostrecvq = new System.Messaging.MessageQueue();
                    qpath = servicequeuepathroot + hostrecvqname;
                    hostrecvq.Path = qpath;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + qpath + ", Attivata");
                }

                // Queue use to send messages to AgvCtl (HostReceive)
                //----------------------------------------------------------------------
                hostsendqname = "";
                try
                {
                    hostsendqname = Tags.GetString("AgvCtl" + ".Settings.HostSendQueue");
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + hostsendqname + ", Definita");
                }
                catch
                {
                }
                if (hostsendqname != "" && hostsendqname != null)
                {
                    hostsendq = new System.Messaging.MessageQueue();
                    qpath = servicequeuepathroot + hostsendqname;
                    hostsendq.Path = qpath;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + qpath + ", Attivata");
                }
            }
            catch (System.Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "AgvCtlStartReceive", ex.Message);
                //throw /*(new MhcsException(AGVCTL_ERR_CREATE_HOST_QUEUE, qpath, ex.Message))*/;
            }

            if (hostrecvqname != "" && hostrecvqname != null)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Thread OnAgvCtlMessageReceived su: " + hostrecvqname + ", Creato");
                hostsendhandler = new System.Messaging.ReceiveCompletedEventHandler(OnAgvCtlMessageReceived);
                hostrecvq.ReceiveCompleted += hostsendhandler;
                hostrecvq.BeginReceive();

                //Log(AMSBRG_STARTUP_PROGRESS, "AgvCtl queue receive started");
            }
        }
        /// <summary>
        /// Host Bridge è sempre in ricezione dalla coda privata HostReceive ed invia sulla coda HostSend
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ar"></param>
        public void OnAgvCtlMessageReceived(object e, ReceiveCompletedEventArgs ar)
        {
            string message;

            lock (LockObject)
            {
                System.Messaging.MessageQueue mq;
                System.Messaging.Message msg;
                try
                {
                    mq = (System.Messaging.MessageQueue)(e);
                    msg = mq.EndReceive(ar.AsyncResult);
                    message = msg.Label;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "OnAgvCtlMessageReceived attiva ProcessAgvCtlMessage(" + message + ")");
                    HandleAgvCtlMessage(message);
                }
                catch (HostException ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_INVALID_MESSAGE, ex.Message);
                }
                catch (OracleException ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, ex.Message);
                }
                catch (MessageQueueException ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_RECEIVE_QUEUE, ex.Message);
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_RECEIVE_QUEUE, ex.Message);
                }
                finally
                {
                    hostrecvq.BeginReceive();
                }
            }
        }
        /// <summary>
        /// Elabora i messaggi ricevuti da AgvCtl
        /// Il messaggio è nel formato di lista di token
        /// TS: [TS],[TID],[LID],[AGVNO],[Location],[phase],[description]
        /// </summary>
        /// <param name="message"></param>
        private void HandleAgvCtlMessage(string message)
        {
            string[] tokens = new string[10];

            MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "ProcessAgvCtlMessage ( " + message + " )");

            tokens = message.Split(',');
            lock (CountersLock)
            {
                switch (tokens[0])
                {
                    case "TS":
                        {
                            ProcessAgvCtlTlg(message);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        ///<para> Processa i telegrammi inviati da AgvCtl</para>
        /// <para>Per il Transport Status: TS</para>
        /// <para>    Phase = 0: Queued   "Transport order queued"</para>
        /// <para>            1: Started  "Transport order started"</para>
        /// <para>            2: Picked   "Load picked up"</para>
        /// <para>            3: Dropped  "Load dropped off"</para>
        /// <para>            Error code  "Descrizione dell'errore"</para>
        /// </summary>
        /// <param name="message">"[TS],[TID],[LID],[AGVNO],[Location],[phase],[description]"</param>
        private void ProcessAgvCtlTlg(string message)
        {
            string[] tokens = new string[8];
            byte[] MsgBytebuffer = new byte[512];
            //string TroRecord;
            string[] MasterTokens = new string[20];
            int tid;                    //Identificativo del trasporto
            int agvno;                  //Numero dell'Agv a cui il trasporto è stato assegnato
            string stragv;              //AgvString name - 171220
            //int tkidx =0;               //Indice del token
            //int msgic, msgps, msgar,cst;
            string sndmessage = "";      //Il messaggio da inviare a Host (senza testa e coda)

            //string StrIC, StrPS, StrAR, StrMSG, StrMISS,StrCST;
            //string StrCodiceSupportoOrig, StrCodiceSupportoChg, StrCodiceArrivo;
            //string StrCodiceDestinazione, StrSTS, StrIC1;

            tokens = message.Split(',');
            //In base alla phase aggiorna il master dei trasporti e manda un telegramma
            //se necessario, manda un telegramma a Host

            tid = System.Convert.ToInt32(tokens[1]);
            agvno = System.Convert.ToInt32(tokens[3]);

            if (agvno < 0)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "N. Agv Errato ricevuto da AgvCtl ( " + agvno + " )");
            }
            else
            {
                //agvno += 10;
                stragv = "AVC";
                if (agvno == 1) stragv += "G01";
                else if (agvno == 2) stragv += "G02";
                else if (agvno == 3) stragv += "F01";
                else if (agvno == 4) stragv += "F02";

                switch (tokens[5])
                    {
                        case "0":   //Il trasporto è stato ricevuto e accodato
                            break;
                        case "1":   //Il trasporto è stato assegnato a un AGV
                            {
                                StartTrOrderInMaster(tid, agvno, Connection);           //Aggiorna il master dei trasporto con trasporto assegnato
                            }
                            break;
                        case "2":   //E' stato effettuato il prelievo - ral 171201 changed
                            {
                                sndmessage = PrepareToQueueSIMessage(stragv, "", "01");          //Raccoglie tutti i dati necessari al messaggio per Host
                                PickDoneInMaster(tid, agvno, "0", Connection);          //Aggiorna il master dei trasporti con pick done
                                QueueSMMsgToHost(sndmessage);                           //Salva il messaggio nel DB
                            }
                            break;
                        case "4":
                        case "3":   //E' stato effettuato il deposito
                            {
                                sndmessage = PrepareToQueueSMMessage(message);          //Raccoglie tutti i dati necessari al messaggio per Host
                                DropDoneInMaster(sndmessage, agvno, Connection);        //Aggiorna il master dei trasporti con drop done                                        
                                QueueSMMsgToHost(sndmessage);                           //Salva il messaggio nel DB
                                                                                        //Questa parte va messa anche nel watchdog
                                                                                        //MsgBytebuffer = PrepareMsgStatoMissione(sndmessage);
                                                                                        //MsgBytebuffer = SwapByte(MsgBytebuffer, 512);           // Ripristina il msg originale
                                                                                        //ExecuteSendHostMessage(MsgBytebuffer);
                            }
                            break;
                        default:    //Il trasporto è in errore
                            break;
                    }
            }
        }

        #endregion
        #region Gestione dei messaggi di logging

        void LogStartReceive()
        {
            servicequeuepathroot = MhcsLib.App.GetSetting("ServiceQueuePathRoot");						// Legge il PathRouth dal file di configurazione App.config
            qpath = "";

            try
            {
                // Definizione della coda utilizzata per ricevere messaggi inviati da AgvCtl (LogReceive)
                //---------------------------------------------------------------------------------------
                logrecvqname = "";
                try
                {
                    logrecvqname = Tags.GetString("HostBridge" + ".Settings.LogReceiveQueue");			// Prende il valore dal tag "HostBridge.Settings.LogReciveQueue"
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, 						// Logga l'evento
                            "Coda " + logrecvqname + ", Definita");
                }
                catch
                {
                }
                if (logrecvqname != "" && logrecvqname != null)
                {
                    logrecvq = new System.Messaging.MessageQueue();										// Crea un'istanza della coda
                    qpath = servicequeuepathroot + logrecvqname;										// Inserisce in qpath il cammino della coda
                    logrecvq.Path = qpath;																// Ricava il nome della coda con il cammino
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "Coda " + qpath + ", Attivata");
                }

                // Definizione della coda utilizzata per l'invio di messaggi al logger (LogReceive)
                //---------------------------------------------------------------------------------
                logsendqname = "";                                                                      // Per HostBrige send e receive corrispondono
                try
                {
                    logsendqname = Tags.GetString("HostBridge" + ".Settings.LogReceiveQueue");			// Prende il valore dal tag "AgvCtl.Settings.HostSendQueue"
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, 						// Logga l'evento
                            "Coda " + logsendqname + ", Definita");
                }
                catch
                {
                }
                if (logsendqname != "" && logsendqname != null)
                {
                    logsendq = logrecvq;										                        // Usa la stessa coda della ricezione
                    qpath = servicequeuepathroot + logsendqname;										// Inserisce in qpath il cammino della coda
                    logsendq.Path = qpath;                                                             	// Ricava il nome della coda con il cammino
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, 						// Logga l'evento
                            "Coda " + qpath + ", Attivata");
                }
            }
            catch (System.Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "LogStartReceive", ex.Message);
                //throw /*(new MhcsException(AGVCTL_ERR_CREATE_HOST_QUEUE, qpath, ex.Message))*/;
            }

            if (logrecvqname != "" && logrecvqname != null)
            {
                loghandler = 																		    // Definisce cosa fare al ricevimento di un messaggio
                        new System.Messaging.ReceiveCompletedEventHandler(OnLogMessageReceived);		// OnLogMessageReceived, gestisce l'evento
                logrecvq.ReceiveCompleted += loghandler;											    //
                logrecvq.BeginReceive();																// Si mette in attesa dell'evento i ricezione completa
                // Va richiamato ognivolta che l'evento Ã¨ stato gestito
                // Cosi da mettersi nuovamente in attesa

                MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG,
                            "Thread OnLogMessageReceived su: " + hostrecvqname + ", Creato");
                //Log(AMSBRG_STARTUP_PROGRESS, "AgvCtl queue receive started");
            }
        }

        public void OnLogMessageReceived(object e, ReceiveCompletedEventArgs ar)
        {
            string message;

            lock (LockObject)
            {
                System.Messaging.MessageQueue mq;
                System.Messaging.Message msg;
                try
                {
                    mq = (System.Messaging.MessageQueue)(e);
                    msg = mq.EndReceive(ar.AsyncResult);
                    message = msg.Label;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "OnLogMessageReceived attiva ProcessLogMessage(" + message + ")");
                    ProcessLogMessage(message);
                }
                catch (HostException ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_INVALID_MESSAGE, ex.Message);
                }
                catch (System.Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_RECEIVE_QUEUE, ex.Message);
                }
                finally
                {
                    logrecvq.BeginReceive();
                }
            }
        }
        /// <summary>
        /// <para>Processa gli allari rivevuti e li storicizza nella tabella Alarms</para>
        /// <para>Gli allarmi devono avere il formato corretto per poter essere processati</para>
        /// <para>GLi allarmi provenienti da AgvCtl, contenenti info relative ai vecchi AGV saranno per esempio:</para>
        /// <para>[Source][Severity][Message]</para>
        /// <para>[Source]  = {"AgvCtl";"AgvUi";"HostBridge"}</para>
        /// <para>[Severity]= {"Info";"Warning";"Error"}</para>
        /// <para>[Message] = {"Il testo dell'allarme"}</para>
        /// <para>esempio ->     "AgvCtl:Warning:AED,Agvno,3,ErrCod2,ErrCod2,ErrCod3" -- Messaggio Agv Digitron</para>
        /// <para>esempio ->     "AgvCtl:Warnin":AEA,Agvno,3,ErrCod2,ErrCod2,ErrCod3" -- Messaggio Agv Agve</para>
        /// <para>esempio ->      "AgvUI:Warning:Utente,Gruppo,Messaggio"             -- Messaggio</para>
        /// <para>esempio -> "HostBridge:Warning:Messaggio"                           -- Messaggio da HostBridge</para>
        /// </summary>
        /// <param name="Message">"Sorgente:Importanza:Testo"</param>
        public void ProcessLogMessage(string Message)
        {
            string AlarmText = "";
            string[] AlarmMessages;
            string[] Tokens = new string[3];
            Tokens = Message.Split(':');
            switch (Tokens[0])
            {
                case "AgvCtl":
                    {
                        AlarmMessages = UnpackAgvCtlAlarm(Tokens[2]);
                        //AlarmText = UnpackDigAgvAlarm(Tokens[2]);
                        for (int i = 0; i < AlarmMessages.Length; i++)
                        {
                            if (AlarmMessages[i].Split(':').Length >= 2 &&
                                AlarmMessages[i].Split(':')[1] != "")
                                InserRecordInAlarms(Tokens[0],
                                                    AlarmMessages[i].Split(':')[0],
                                                    AlarmMessages[i].Split(':')[1]);
                        }
                    }
                    break;
                case "AgvUI":
                    {
                        AlarmText = UnpackAgvUiAlarm(Tokens[2]);
                        if (AlarmText != "") InserRecordInAlarms(Tokens[0], Tokens[1], AlarmText);
                    }
                    break;
                case "HostBridge":
                    {
                        AlarmText = UnpackHostBridgeAlarm(Tokens[2]);
                        if (AlarmText != "") InserRecordInAlarms(Tokens[0], Tokens[1], AlarmText);
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion
        #region Gestisce la simulazione di messagi da e verso Host

        /// <summary>
        /// Attiva la parte simulazione di messaggi da Host
        /// </summary>
        void SimHostStartReceive()
        {
            IPAddress LocalIpAddress;
            int LocalIpPortMsg = 3000;
            int LocalIpPortAck = 3001;

#if MacchinaRoche
            LocalIpAddress = IPAddress.Parse("141.167.177.93");
#else
            LocalIpAddress = IPAddress.Parse("192.168.10.5");
#endif
            LocalIpMsgEndPoint = new IPEndPoint(LocalIpAddress, LocalIpPortMsg);
            LocalIpAckEndPoint = new IPEndPoint(LocalIpAddress, LocalIpPortAck);

            msgclient = new UdpClient(LocalIpMsgEndPoint);
            msgclient.BeginReceive(new AsyncCallback(OnSimHostMessageReceived), null);

            ackclient = new UdpClient(LocalIpAckEndPoint);
            ackclient.BeginReceive(new AsyncCallback(OnSimHostAckReceived), null);
        }
        /// <summary>
        /// Funzione attivata alla ricezione, su porta UDP, di una comunicazione in binario
        /// Questo è il canale di cumuniazione per i messaggi MhM simulati
        /// </summary>
        /// <param name="ar"></param>
        public void OnSimHostMessageReceived(IAsyncResult ar)
        {
            byte[] recbytes = msgclient.EndReceive(ar, ref LocalIpMsgEndPoint);
            //string message = Encoding.ASCII.GetString(recbytes);

            HandleHostMessage(recbytes);

            msgclient.BeginReceive(new AsyncCallback(OnSimHostMessageReceived), null);  // Si rimette in attesa di un messaggio
        }
        /// <summary>
        /// Funzione attivata alla ricezione, su porta UDP, di una comunicazione in binario
        /// Questo è il canale di comunicazione per gli ack
        /// </summary>
        /// <param name="ar"></param>
        public void OnSimHostAckReceived(IAsyncResult ar)
        {
            byte[] recbytes = ackclient.EndReceive(ar, ref LocalIpAckEndPoint);
            //string message = Encoding.ASCII.GetString(recbytes);
            HandleHostAck(recbytes);

            ackclient.BeginReceive(new AsyncCallback(OnSimHostAckReceived), null);  // Si rimette in attesa di un Ack
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ar"></param>
        public void OnHostMessageReceived(object e, ReceiveCompletedEventArgs ar)
        {
            string message;

            lock (LockObject)
            {
                System.Messaging.MessageQueue mq;
                System.Messaging.Message msg;
                try
                {
                    mq = (System.Messaging.MessageQueue)(e);
                    msg = mq.EndReceive(ar.AsyncResult);
                    message = msg.Label;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "OnHostMessageReceived attiva ProcessHostBridgeMessage(" + message + ")");
                    //ProcessHostMessage(message);
                }
                catch (HostException ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_INVALID_MESSAGE, ex.Message);
                }
                catch (System.Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_RECEIVE_QUEUE, ex.Message);
                }
                finally
                {
                    sh1recvq.BeginReceive();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ar"></param>
        public void OnHostAckReceived(object e, ReceiveCompletedEventArgs ar)
        {
            string message;

            lock (LockObject)
            {
                System.Messaging.MessageQueue mq;
                System.Messaging.Message msg;
                try
                {
                    mq = (System.Messaging.MessageQueue)(e);
                    msg = mq.EndReceive(ar.AsyncResult);
                    message = msg.Label;
                    MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "OnHostAckReceived attiva ProcessHostBridgeAck(" + message + ")");
                    //ProcessHostAck(message);
                }
                catch (HostException ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_INVALID_MESSAGE, ex.Message);
                }
                catch (System.Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_RECEIVE_QUEUE, ex.Message);
                }
                finally
                {
                    sh1recvq.BeginReceive();
                }
            }
        }

        #endregion
    }
}
