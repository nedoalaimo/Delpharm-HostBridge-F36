//********************************************************************
// Filename: HostGlobals.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.0
// Status       : c         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 01.05.16
// ___________________________________________________________________
// Description  : Variabili globali utilizzate in HostBridge 
//  
//                #define MacchinaLocale e MacchinaRoche, permettono di 
//                utilizzare il codice su pc di test               
//__________________________________________________________________
// Changes      :
// Date     Author      Description
//
//********************************************************************
#undef MacchinaLocale                  //Ambiente di sviluppo
#define MacchinaRoche                  //macchina di produzione

using System.Messaging;
using System.Collections;
using Oracle.DataAccess.Client;
using System.Runtime.InteropServices;
using System.Text;

namespace HostBridge
{
    partial class HostBridge : MhcsLib.SvcApp
    {
        public const int MaxIcCounter   = 32766;  // Massimo valore per il contatore Ic
        public const int MaxPsCounter   = 32766;  // Massimo valore per il contatore Ps di invio
        public const int MaxAlarms      = 10000;  // Massimo numero di allarmi nella tabella Alamrs

        //stato ricezione messaggio Host
        public const int VDATA = 0;             // Messaggio ricevuto e elaborato senza problemi
        public const int IDATA = 1;             // Messaggio ricevuto con dati non validi o non congruenti
        public const int BFULL = 2;             // Il messaggio non può essere memorizzato
        public const int NORUN = 3;             // Non è possibile processare il messaggio perche applicazione OFF

        //Parametri di configurazione delle ripetizioni di trasmissione
        public const int NUM_SHORT_RETRY    = 3;    // Definisce il numero di tentativi di ritrasmissione a intervali brevi
        public const int SHORT_DELAY        = 50;   // Intervallo breve di 5 secondi
        public const int LONG_DELAY         = 600;  // Intervallo lungo di 60 secondi

        // Stato esecuzione trasporto
        public const int TRP_ACTIVE = 1;        // Il trasporto è attivo
        public const int TRP_COMPL  = 2;        // Il trasporto è stato completato
        public const int TRP_NOPICK = 6;        // Il trasporto è stato completato senza alcun prelievo
        public const int TRP_ERRTDK = 258;      // Il trasporto è stato completato con errore di tracking
        public const int TRP_NODROP = 10;       // Il trasporto è stato completato senza alcun deposito


        #if MacchinaLocale
            public const string DATAFORMAT  = "DD/MM/YY hh24:mi:ss";       // Configurazione per la macchina locale
            public const bool TESTMODE      = true;
        #else
            public const string DATAFORMAT  = "MM/DD/YYYY HH:MI:SS AM";    // Configurazione per la macchina in produzione
            public const bool TESTMODE      = false;
        #endif

        long RetryCounter               = 0;        // Numero di tentativi di invio
        static long retrytimerinterval  = 20;       // Intervallo di tempo tra ripezioni, di default settato a 20

        //int LastHostPsReceived = 0;
        //int LastHostIcReceived = 0;
        //int PsCounter = 0;
        //int PaCounter = 0;
        //int IcCounter = 0;

        int FromHostAckPending;                 // HostBridge è in attesa di un ack da parte di Host

    }
}
