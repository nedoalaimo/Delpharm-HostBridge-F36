//********************************************************************
// Filename: HostTest.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.0
// Status       : c         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 01.05.16
// ___________________________________________________________________
// Description  : Raccolta di test da effettuare in fase di coding
//                Bisogna inserire la funzione justForTest in AppStart 
//                E Ricordarsi di toglierla in delivery
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
        public void justForTest()
        {
            //testvar = NodeToSh1("msg", "9439,9439,1,SM,58504,17000   ,3031PS31,PSRC32 ");
            //CreateOrderInMaster(1, "12345", "stn2", "stn2", 1, Connection);
            //StartTrOrderInMaster(1, 12, Connection);
            //PickDoneInMaster(1, 12, "Pick", Connection);
            //DropDoneInMaster(1, 12, "Drop", Connection);
            //CompletedTrOrderInMaster(1, 12, 1,3, "Completed", Connection);
            //DeleteOrderInMaster(1, Connection);

            //Test del contatore di Ic, veridica che raggiunto il valore massimo ritorna a 1
            //for (int i = 0; i < 80000; i++)
            //{
            //    int j = GetNextIc(Connection);
            //    if (j == 1)
            //    {
            //        int k = 0;
            //    }
            //}

            //Test del contatore di Ps, veridica che raggiunto il valore massimo ritorna a 1
            //for (int i = 0; i < 80000; i++)
            //{
            //    int j = GetNextPs(Connection);
            //    if (j == 1)
            //    {
            //        int k = 0;
            //    }
            //}
            //SetHostAckPending(0,Connection);
            //int i = GetHostAckPending(Connection);
            //SetHostAckPending(1, Connection);
            //int j = GetHostAckPending(Connection);

            //string statosistema = GestSystemStatus();   //Ritorna lo stato del sistema Run o Shutdown
            //bool i = IsValidStation("3031PS99", "Pick");
            //    i = IsValidStation("3031PS99", "Drop");
            //    i = IsValidMsgCode("MM");
            //    i = IsValidMsgCode("RM");
            //    i = IsValidMsgCode("RS");
            //    i = IsValidMsgCode("RI");
            //    i = IsValidMsgCode("XX");
            //int AgvNo = GetNodeAgvNo("PSAV01");
            //    AgvNo = GetNodeAgvNo("PSAV02");
            //    AgvNo = GetNodeAgvNo("PSAV03");
            //    AgvNo = GetNodeAgvNo("PSAV04");
            //    AgvNo = GetNodeAgvNo("PSAV05");
            //string      AgvName = GetHostAgvName(1);
            //            AgvName = GetHostAgvName(2);
            //            AgvName = GetHostAgvName(3);
            //            AgvName = GetHostAgvName(4);
            //            AgvName = GetHostAgvName(5);

            //SaveHostMsg(500, 700, 1, "Ciao Pippo1", 500);
            //SaveHostMsg(500, 700, 1, "Ciao Pippo2", 500);
            //SaveHostMsg(500, 700, 1, "Ciao Pippo3", 500);
            //SaveHostMsg(500, 700, 1, "Ciao Pippo4", 500);

            //string stringtosend = PrepareHostAck(215, 4000, 1, 0);

            //string[] inputdati = {"123","333","1","RM","234  ","PSAV01  ","3031FR07","1","123"};
            //int risultato = IsValidMessageRecord(inputdati);

            //string[] tokens = GetNodeAckToSendS();

            //UInt16[] tokensd = GetNodeAckToSend();
            //string ackmsg = PrepareHostAck(tokensd[0], tokensd[1], tokensd[2], tokensd[3]);
            //DeleteHostAck(tokensd[1]);
            //int result = SaveHostMsg("15214", "2", "1", "MM", "15214");
            //result = SaveHostMsg("15215", "3", "1", "MM", "15215");
            //result = SaveHostMsg("15216", "4", "1", "MM", "15216");
            //result = SaveHostMsg("15217", "5", "1", "MM", "15217");
            //result = SaveHostMsg("15218", "6", "1", "MM", "15218");
            //result = SaveHostMsg("15219", "7", "1", "MM", "15219");
            //string[] tokens = GetHostMessageReceived();
            //bool duplicato = IsDuplicatedMessage(1, 2);

            //SaveAckToHost("550", "440", "0", "0", "550");
            //SaveAckToHost("551", "441", "0", "0", "551");
            //SaveAckToHost("552", "442", "0", "0", "552");
            //SaveAckToHost("553", "443", "0", "0", "553");
            //SaveAckToHost("554", "444", "0", "0", "554");
            //SaveAckToHost("555", "445", "0", "0", "555");
            //DeleteHostAck(444);
            //UInt16[] tokensd = GetNodeAckToSend(441);
            //string result = PrepareHostAck(1, 2, 0, 1);
            //string[] inputdati = {"123","333","1","RM","234  ","PSAV01  ","3031FR07","1","123"};
            //ProcessCommandFromHost(inputdati);
            //string[] inputdati = {"123","333","1","RM","234  ","PSAV01  ","3031FR07","1","123"};
            //int result = SaveHostMsg("123", "333", "1", "RM", "234  ", "0925    ","PSAV01  ", "3031FR07", "1", "123");

            //string Udc = ""; string dst = "";
            //GetTransportData(58648, ref  Udc, ref  dst);
            //SetHostAckPending(0);
            //int result = GetHostAckPending();
            //SetHostAckPending(1);
            //result = GetHostAckPending();
            //SetHostAckPending(0);
            //result = GetHostAckPending();
            //DeleteMessageFromHost(8479);

            //string[] tokens = GetOldestHostMessageReceived();                       //Recupera il più vecchio comando Host da eseguire
            //string msg = tokens[3];
            //string strps = tokens[1];
            //int ps = System.Convert.ToUInt16(strps);
            //ProcessCommandFromHost(tokens);                                     //Processa il messaggio 
            //DequeueMessageFromHost(ps);                                          //Cancella dalla coda il messaggio processato
            //tokens = GetOldestHostMessageReceived();                       //Recupera il più vecchio comando Host da eseguire
            //msg = tokens[3];
            //strps = tokens[1];
            //ps = System.Convert.ToUInt16(strps);
            //ProcessCommandFromHost(tokens);                                     //Processa il messaggio 
            //DequeueMessageFromHost(ps);                                          //Cancella dalla coda il messaggio processato
            //tokens = GetOldestHostMessageReceived();                       //Recupera il più vecchio comando Host da eseguire
            //msg = tokens[3];
            //strps = tokens[1];
            //ps = System.Convert.ToUInt16(strps);
            //ProcessCommandFromHost(tokens);                                     //Processa il messaggio 
            //DequeueMessageFromHost(ps);                                          //Cancella dalla coda il messaggio processato

            //string[] tokens;

            //QueueMsgFromHost("9001", "5001", "1", "RS", "9001");
            //QueueMsgFromHost("9002", "5002", "1", "MM", "9002");
            //QueueMsgFromHost("9003", "5003", "1", "RI", "9003");
            //QueueMsgFromHost("9004","5004","1","RM","58648","22006","PSRC03","3031PS02","1","9004");
            //tokens = GetOldestHostMessageReceived();
            //ProcessCommandFromHost(tokens);
            //DequeueMessageFromHost(System.Convert.ToUInt16(tokens[1]));
            //tokens = GetOldestHostMessageReceived();
            //ProcessCommandFromHost(tokens);
            //DequeueMessageFromHost(System.Convert.ToUInt16(tokens[1]));
            //tokens = GetOldestHostMessageReceived();
            //ProcessCommandFromHost(tokens);
            //DequeueMessageFromHost(System.Convert.ToUInt16(tokens[1]));
            //tokens = GetOldestHostMessageReceived();
            //ProcessCommandFromHost(tokens);
            //DequeueMessageFromHost(System.Convert.ToUInt16(tokens[1]));

            //QueueAckFromHost("16511", "3", "0", "0", "16511"); 
            //QueueAckFromHost("16512", "4", "0", "0", "16512");
            //QueueAckFromHost("16513", "5", "0", "0", "16513");
            //QueueAckFromHost("16514", "6", "0", "0", "16514");
            //------------------------------------------
            //creo i trasporti completati
            //accodo i messaggi di stato missione 
            //ricevo gli ack da MhM
            //trasporti e messaggi relativi dovrebbero essere cancellati

            
            //CreateOrderInMaster("1", "2395", "PSRC03", "3031PS02", "1", "3");
            //CreateOrderInMaster("2", "2396", "PSRC03", "3031PS03", "1", "3");
            //CreateOrderInMaster("3", "2397", "PSRC03", "3031PS04", "1", "3");

            //QueueMsgToHost("1,1,1,SM,1, 2395, , PSRC03, 3031PS02, 3, 1");
            //QueueMsgToHost("2,2,1,SM,2, 2396, , PSRC03, 3031PS03, 3, 2");
            //QueueMsgToHost("3,3,1,SM,3, 2397, , PSRC03, 3031PS04, 3, 3");

            //string[] tokens1 = { "4", "3", "0", "0", "4" };
            //ProcessAckFromHost(tokens1);
            //string[] tokens2 = { "5", "1", "0", "0", "5" };
            //ProcessAckFromHost(tokens2);
            //string[] tokens3 = { "6", "2", "0", "0", "6" };
            //ProcessAckFromHost(tokens3);

            //Ciclo completo
            //1) MhM    -manda una RS
            //2) Node   -da un ack a MhM
            //3) Node   -genera un trasporto stato 0
            //4) Node   -Manda il trasporto a AgvCtl
            //5) AgvCtl -dice che ha completato
            //6) Node   -Aggiorna lo stato del trasporto a 3
            //7) Node   -Manda un messaggio di SM  MhM con stato 3 (Completato)
            //8) MhM    -Risponde con un ack
            //9) Node   -Aggiorna il log
            //10)Node   -Cancella il trasporto
            //11)Node   -Resetta i contatori
            //12)Node   -Cncella il messaggio

            //CreateOrderInMaster("1", "2395", "PSRC03", "3031PS02", "1", "3");
            //CreateOrderInMaster("2", "2396", "PSRC03", "3031PS03", "1", "3");
            //CreateOrderInMaster("3", "2397", "PSRC03", "3031PS04", "1", "3");

            //QueueMsgToHost("1,1,1,SM,1, 2395, , PSRC03, 3031PS02, 3, 1");
            //QueueMsgToHost("2,2,1,SM,2, 2396, , PSRC03, 3031PS03, 3, 2");
            //QueueMsgToHost("3,3,1,SM,3, 2397, , PSRC03, 3031PS04, 3, 3");

            //string[] tokens1 = { "4", "3", "0", "0", "4" };
            //ProcessAckFromHost(tokens1);
            //string[] tokens2 = { "5", "1", "0", "0", "5" };
            //ProcessAckFromHost(tokens2);
            //string[] tokens3 = { "6", "2", "0", "0", "6" };
            //ProcessAckFromHost(tokens3);

            //InserRecordInHostLog("Node", "Ack", "Questo è un messaggio di prova 1");

            //----------------------------------------------------------------------------
            //Accoda i messaggi
            //QueueSMMsgToHost("1,1,1,SM,1, 2395, , PSRC03, 3031PS02, 3, 1");
            //QueueSMMsgToHost("2,2,1,SM,2, 2396, , PSRC03, 3031PS03, 3, 2");
            //QueueSMMsgToHost("3,3,1,SM,3, 2397, , PSRC03, 3031PS04, 3, 3");
            ////Preleva il messaggio più vecchio
            //string[] tokens = GetOldestNodeMessageToSend();
            //string stringa = PrepareToQueueSMMessage(tokens[3]);

            //string sndmessage = PrepareSSMessage();
            //QueueSMMsgToHost(sndmessage); 
  
            //string valore = PrepareSIMessage("PSAV01", "5402");

            //string valore = PrepareSIMessage("PSAV01", "");                //Raccoglie tutti i dati necessari al messaggio per Host
            //QueueSIMsgToHost(valore);                               //Salva il messaggio nel DB
            //valore = PrepareSIMessage("PSAV02", "");
            //QueueSIMsgToHost(valore);
            //valore = PrepareSIMessage("PSAV03", "");
            //QueueSIMsgToHost(valore);
            //valore = PrepareSIMessage("PSAV04", "");
            //QueueSIMsgToHost(valore);
            //valore = PrepareSIMessage("", "00000000");
            //QueueSIMsgToHost(valore);

            //string[] ppp = GetOldestHostMessageReceived();
            //ppp = GetOldestHostMessageReceived();
            //ppp = GetOldestHostMessageReceived(); 
            //ppp = GetOldestHostMessageReceived(); 
            //ppp = GetOldestHostMessageReceived();
            ///string[] ppp = GetOldestHostMessageReceived();

        }
    }
}
