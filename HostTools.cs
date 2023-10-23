//********************************************************************
// Filename: HostTools.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.0
// Status       : c         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 01.05.16
// ___________________________________________________________________
// Description  : Database update 
//                 
//_____________________________________________________________________
// Changes      :
// Date     Author      Description
// 170817   ral         ReadFlag handling for barcode at F36 ground floor
// 170817   ral         UnpackHostMessage changed
// 170817   ral         Enable/disable barcode reading at ground elev.
// 171201   ral         Added IdSup telegram at picking, handling
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
    #region Definizioni globali
    [StructLayout(LayoutKind.Explicit)]
    struct Int32Converter
    {
        [FieldOffset(0)]
        public int Value;
        [FieldOffset(0)]
        public byte Byte1;
        [FieldOffset(1)]
        public byte Byte2;
        [FieldOffset(2)]
        public byte Byte3;
        [FieldOffset(3)]
        public byte Byte4;

        public Int32Converter(int value)
        {
            Byte1 = Byte2 = Byte3 = Byte4 = 0;
            Value = value;
        }
        public static implicit operator Int32(Int32Converter value)
        {
            return value.Value;
        }
        public static implicit operator Int32Converter(int value)
        {
            return new Int32Converter(value);
        }
    }
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct UInt16Converter
    {
        [FieldOffset(0)]
        public ushort Value;
        [FieldOffset(0)]
        public byte Byte1;
        [FieldOffset(1)]
        public byte Byte2;

        public UInt16Converter(UInt16 value)
        {
            Byte1 = Byte2 = 0;
            Value = value;
        }
        public static implicit operator UInt16(UInt16Converter value)
        {
            return value.Value;
        }
        public static implicit operator UInt16Converter(UInt16 value)
        {
            return new UInt16Converter(value);
        }
    }
    
    #endregion
    partial class HostBridge : MhcsLib.SvcApp
    {
        private string GetDate()
        {
            return (System.DateTime.Now.ToString("yyyyMMddHHmmss"));
        }
        /// <summary>
        /// Ritorna il prossimo Ic per i messaggi
        /// Se il valore supera il massimo valore consentito, viene settato a 1
        /// La risorsa va lockata esternamente
        /// </summary>
        /// <returns></returns>
        private int GetNextNodeIc()
        {
            int rows;
            int progressive = 0;
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
                                    @" FROM ""HostControlBoard""" +
                                    @" WHERE ""ItemName"" = 'IcSent'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    progressive = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                    break;
                }
                reader.Close();


                if (++progressive > MaxIcCounter) progressive = 1;

                // Aggiorna la tabella di controllo con il nuovo valore Ic
                //--------------------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + progressive + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'IcSent'";
                rows = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            return progressive;
        }
        /// <summary>
        /// Ritorna il prossimo Ic per gli Ack
        /// Se il valore supera il massimo valore consentito, viene settato a 1
        /// La risorsa va lockata esternamente
        /// </summary>
        /// <returns></returns>
        private int GetNextNodeAckIc()
        {
            int rows;
            int progressive = 0;
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
                                    @" FROM ""HostControlBoard""" +
                                    @" WHERE ""ItemName"" = 'IcAckSent'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    progressive = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                    break;
                }
                reader.Close();


                if (++progressive > MaxIcCounter) progressive = 1;

                // Aggiorna la tabella di controllo con il nuovo valore Ic
                //--------------------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + progressive + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'IcAckSent'";
                rows = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                reader.Dispose();
                reader.Close();
            }
            return progressive;
        }
        /// <summary>
        /// Ritorna il prossimo Ps
        /// Se il valore supera il massimo vaore consentito, vine settato a 1
        /// La risorsa va lockata esternamente
        /// </summary>
        /// <returns></returns>
        private int GetNextNodePs()
        {
            int rows;
            int progressive = 0;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                // Recupera il valore corrente di Ps e lo aggiorna o incrementadolo o resettandolo a 1
                //------------------------------------------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""HostControlBoard""" +
                                    @" WHERE ""ItemName"" = 'PsSent'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    progressive = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                    break;
                }
                reader.Close();


                if (++progressive > MaxIcCounter) progressive = 1;

                // Aggiorna la tabella di controllo con il nuovo valore Ic
                //--------------------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + progressive + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'PsSent'";
                rows = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                reader.Close();
            }
            return progressive;
        }
#if false
        /// <summary>
        /// Converte un messaggio proveniente da Host (Sinec H1) nel formato conprensibile a Node (Ascii)
        /// </summary>
        private string Sh1ToNode(byte[] message)
        {
            return "";
        }
        /// <summary>
        /// RIceve un messaggio da AgvCtl, in formato Asii e ne cera uno in ByteArray pronto da inviare a Host
        /// </summary>
        /// <param name="msgtype">msg = Message; ack = Acknowledge</param>
        /// <param name="AsciiMsg">il messaggio proveniente da AgvCtl, con token separati da virgola</param>
        /// <returns></returns>
        private byte[] NodeToSh1(string msgtype, string AsciiMsg)
        {
            UInt16Converter ICI, PSPA, AR, Number;
            byte[] ByteVar = new byte[8];
            string[] tokens;
            byte[] ByteMsg = new byte[512];

            tokens = AsciiMsg.Split(',');                           // Split the Ascii message in tokens
            ICI = Convert.ToUInt16(tokens[0]);
            PSPA = Convert.ToUInt16(tokens[1]);
            AR = Convert.ToUInt16(tokens[2]);
            ByteMsg[1] = ICI.Byte1; ByteMsg[0] = ICI.Byte2;       // IC o CI
            ByteMsg[3] = PSPA.Byte1; ByteMsg[2] = PSPA.Byte2;       // PS o PA
            ByteMsg[5] = AR.Byte1; ByteMsg[4] = AR.Byte2;           // AR

            if (msgtype == "msg")
            {                                 // Si tratta di un messaggio
                ByteVar = StringToByteArray(tokens[3]);             // "SM" o "SS" o "SI"
                ByteMsg[6] = ByteVar[0]; ByteMsg[7] = ByteVar[1];   //                
                switch (tokens[3])
                {
                    case "SM":                                      // Stato Missione
                        Number = Convert.ToUInt16(tokens[4]);       // Numero Trasporto
                        ByteMsg[9] = Number.Byte1; ByteMsg[8] = Number.Byte2;

                        ByteVar = StringToByteArray(tokens[5]);     // Codice supporto

                        ByteMsg[10] = (ByteVar.Length > 0 ? ByteVar[0] : (byte)20);
                        ByteMsg[11] = (ByteVar.Length > 1 ? ByteVar[1] : (byte)20);
                        ByteMsg[12] = (ByteVar.Length > 2 ? ByteVar[2] : (byte)20);
                        ByteMsg[13] = (ByteVar.Length > 3 ? ByteVar[3] : (byte)20);
                        ByteMsg[14] = (ByteVar.Length > 4 ? ByteVar[4] : (byte)20);
                        ByteMsg[15] = (ByteVar.Length > 5 ? ByteVar[5] : (byte)20);
                        ByteMsg[16] = (ByteVar.Length > 6 ? ByteVar[6] : (byte)20);
                        ByteMsg[17] = (ByteVar.Length > 7 ? ByteVar[7] : (byte)20);

                        ByteMsg[18] = ByteMsg[19] = ByteMsg[20] = ByteMsg[21] = ByteMsg[22] = ByteMsg[23] = ByteMsg[24] = ByteMsg[25] = 0;

                        ByteVar = StringToByteArray(tokens[6]);     // Codice stazione prelievo
                        ByteMsg[26] = (ByteVar.Length > 0 ? ByteVar[0] : (byte)20);
                        ByteMsg[27] = (ByteVar.Length > 1 ? ByteVar[1] : (byte)20);
                        ByteMsg[28] = (ByteVar.Length > 2 ? ByteVar[2] : (byte)20);
                        ByteMsg[29] = (ByteVar.Length > 3 ? ByteVar[3] : (byte)20);
                        ByteMsg[30] = (ByteVar.Length > 4 ? ByteVar[4] : (byte)20);
                        ByteMsg[31] = (ByteVar.Length > 5 ? ByteVar[5] : (byte)20);
                        ByteMsg[32] = (ByteVar.Length > 6 ? ByteVar[6] : (byte)20);
                        ByteMsg[33] = (ByteVar.Length > 7 ? ByteVar[7] : (byte)20);

                        ByteVar = StringToByteArray(tokens[7]);     // Codice stazione destinazione
                        ByteMsg[34] = (ByteVar.Length > 0 ? ByteVar[0] : (byte)20);
                        ByteMsg[35] = (ByteVar.Length > 1 ? ByteVar[1] : (byte)20);
                        ByteMsg[36] = (ByteVar.Length > 2 ? ByteVar[2] : (byte)20);
                        ByteMsg[37] = (ByteVar.Length > 3 ? ByteVar[3] : (byte)20);
                        ByteMsg[38] = (ByteVar.Length > 4 ? ByteVar[4] : (byte)20);
                        ByteMsg[39] = (ByteVar.Length > 5 ? ByteVar[5] : (byte)20);
                        ByteMsg[40] = (ByteVar.Length > 6 ? ByteVar[6] : (byte)20);
                        ByteMsg[41] = (ByteVar.Length > 7 ? ByteVar[7] : (byte)20);
                        break;
                    case "SS":
                        break;
                    case "SI":
                        break;
                }
                ByteMsg[511] = ICI.Byte1; ByteMsg[510] = ICI.Byte2;   // IC'
            }
            else
            {                                                  // Si tratta di un Ack
                ByteMsg[7] = ICI.Byte1; ByteMsg[6] = ICI.Byte2;       // IC'

            }

            return (ByteMsg);
        } 
#endif
        /// <summary>
        /// Converte una stringa nel corrispondente byte array dei valori ascii costituenti
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] StringToByteArray(string value)
        {
            char[] charArr = value.ToCharArray();
            byte[] bytes = new byte[charArr.Length];
            for (int i = 0; i < charArr.Length; i++)
            {
                byte current = Convert.ToByte(charArr[i]);
                bytes[i] = current;
            }
            return bytes;
        }
        /// <summary>
        /// Legge un telegramma dalla coda di invio e lo spedisce a Host
        /// </summary>
        public void SendMsgToHost()
        {
            int retstatus;
            int rows;
            int icstart = 0;
            int ps = 0;
            int ra = 0;
            string message = "";
            int icstop = 0;
            System.DateTime createtime;
            bool msgfound = false;
            Oracle.DataAccess.Client.OracleCommand cmd = null;
            OracleTransaction HostTrans = null;
            System.DateTime TlgTime = System.DateTime.Now;

            try
            {
                cmd = new Oracle.DataAccess.Client.OracleCommand();
                cmd.Connection = connection;
                HostTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // Legge il valore
                //----------------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                    @" FROM ""SendMsgToHost""" +
                    @" WHERE ""CreateTime"" =(SELECT UNIQUE MIN(""CreateTime"") FROM ""SendMsgToHost"")" +
                    @" ORDER BY ""IcStart""";

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    icstart = (int)reader.GetDecimal(reader.GetOrdinal("IcStart"));
                    ps = (int)reader.GetDecimal(reader.GetOrdinal("Ps"));
                    ra = (int)reader.GetDecimal(reader.GetOrdinal("Ra"));
                    message = reader.GetString(reader.GetOrdinal("Message"));
                    icstop = (int)reader.GetDecimal(reader.GetOrdinal("IcStop"));
                    createtime = reader.GetDateTime(reader.GetOrdinal("CreateTime"));
                    msgfound = true;
                    break;
                }
                reader.Close();
                if (msgfound)
                {
                    //<Compone il messaggio da inviare a Host - TODO>
                    //<Lo costruisce in formato testo e lo da al converitotre NodeToSh1 - TODO>
                    //<Invia il messaggio - TODO>
                    //<se l'esito dell'invio è positivo va una commit altrimenti fa una rollback - TODO>

                    //HostBridge si mette in attesa di Ack da parte di Host
                    //Salva l'Ic del telegramma inviato
                    //------------------------------------------
                    cmd.CommandText = @"UPDATE ""HostControlBoard"" " +
                                      @"SET " +
                                      @"""IntVal"" ='" + icstart + "'" +
                                      @" WHERE ""ItemName""='IcSent'";

                    rows = cmd.ExecuteNonQuery();

                    cmd.CommandText = @"UPDATE ""HostControlBoard"" " +
                                      @"SET " +
                                      @"""IntVal"" ='" + "1" + "'" +
                                      @" WHERE " +
                                      @"   ""ItemName""='HostAckPending'";

                    rows = cmd.ExecuteNonQuery();
                    //MsgBytebuffer = SwapByte(MsgBytebuffer, 512);   // Ripristina il msg originale
                    //SendHostMessage(MsgBytebuffer);
                    FromHostAckPending = 1;                                             // In attesa di Ack da Host
                    SetHostAckPending(1);

                    // Verifica e setta il tipo di intervallo di ripetizione
                    //------------------------------------------------------
                    if (retrycounter <= numshortdelay)
                        retrytimerinterval = shorttimerinterval;
                    else
                        retrytimerinterval = longtimerinterval;
                }
                retstatus = SendMsgToSh1(icstart, ps, ra, message, icstop); //<Invia il telegramma a Host - TODO
                if (retstatus == 1)
                {
                    //<Invio fallito>
                }
                HostTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                HostTrans.Rollback();
            }
            catch (HostException ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, ex.Message);
                HostTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                HostTrans.Rollback();
            }       
            finally
            {
                if (!reader.IsClosed)
                {
                    reader.Close();
                }
            }
        }
        ///// <summary>
        ///// Invia un messaggio a Host via Sinec H1 - TODO
        ///// </summary>
        ///// <param name="IcStart"></param>
        ///// <param name="Ps"></param>
        ///// <param name="Ra"></param>
        ///// <param name="Message"></param>
        ///// <param name="IcStop"></param>
        ///// <returns></returns>
        public int SendMsgToSh1(int IcStart, int Ps, int Ra, string Message, int IcStop)
        {
            int result = 1; //Per adesso ritorna sempre 1, poi bisogna ritornare il valore di sinecH1
            //<Invia il messaggio a ost via Sinec H1>
            //Console.WriteLine("msg: " + IcStart + "," + Ps + "," + Ra + "," + Message + "," + IcStop);
            return (result);
        }
        ///// <summary>
        ///// Invia un ack a Host via Sinec H1 - TODO
        ///// </summary>
        ///// <param name="IcStart"></param>
        ///// <param name="Ps"></param>
        ///// <param name="Ra"></param>
        ///// <param name="IcStop"></param>
        ///// <returns></returns>
        public int SendAckToSh1(int IcStart, int Ps, int Ra, int IcStop)
        {
            int result = 1; //Per adesso ritorna sempre 1, poi bisogna ritornare il valore di sinecH1
            //<Invia il messaggio a ost via Sinec H1>
            //Console.WriteLine("msg: " + IcStart + "," + Ps + "," + Ra + "," + IcStop);
            return (result);
        }

        /// <summary>
        /// Scompatta in tokens il messagio in Byte Array inviato da Host
        /// </summary>
        /// <param name="MsgBytebuffer">Messaggio in Byte Array ricevuto da Host</param>
        /// <returns></returns>
        public string UnpackHostMessage(byte[] MsgBytebuffer)
        {
            #region Parte dichiarativa
            int sidx;                           // Send buffer index
            string Asciimessage = "";
            byte[] sendword = new byte[2];
            UInt16Converter Number = new UInt16Converter();

            UInt16 IC, IC1, PS, MISS, AR, PRIO, STS;
            string StrMSG, StrAGV, StrMISS, StrCodiceSupporto, StrCodiceSupportoOrig, StrCodiceSupportoChg, StrCodiceSorgente, StrCodiceDestinazione;
            string StrAGV2, StrAGV3, StrAGV4, StrSTS, StrSTS2, StrSTS3, StrSTS4;
            byte[] ByteMSG, ByteCodiceSupporto, ByteAGV, ByteCodiceSupportoOrig, ByteCodiceSupportoChg, ByteCodiceArrivo, ByteCodiceDestinazione, ByteCodice;
            byte ReadFlag; // 1= read barcode, 0= otherwise

            ByteMSG = new byte[2];
            ByteAGV = new byte[6];
            ByteCodiceSupporto = new byte[8];
            ByteCodiceSupportoOrig = new byte[8];
            ByteCodiceSupportoChg = new byte[8];
            ByteCodiceArrivo = new byte[8];
            ByteCodiceDestinazione = new byte[8];
            ByteCodice = new byte[6];

            Number = 0;
            #endregion
            #region Gestione parte comune a tutti i messaggi
            sidx = 0;

            // IC (Word 0)(Byte 0,1)
            //---
            Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
            //StrIC = Convert.ToString(Number);
            IC = Number;

            // PS (Word 1)(Byte 2,3)
            //---
            Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
            //StrPS = Convert.ToString(Number);
            PS = Number;

            // AR (Word 2)(Byte 4,5)
            //---
            Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
            //StrAR = Convert.ToString(Number);
            AR = Number;

            // MSG (Word 3)(Byte 6,7) - Qui sappiamo il tipo di messaggio
            //----
            ByteMSG[1] = MsgBytebuffer[sidx++]; ByteMSG[0] = MsgBytebuffer[sidx++];
            StrMSG = System.Text.Encoding.ASCII.GetString(ByteMSG);
            #endregion
            #region Gestione messaggio specifico
            switch (StrMSG)
            {
                #region RM - Exec Missione  -> 6 Token: {[MISS],[Supporto],[source],[dest],[prio],[readFlag]}
                case "RM":  //Exec Missione (170817 ral elevatore handling)
                    {
                        #region MISS                (Word 4    )(Byte 8,9)
                        //---
                        Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
                        StrMISS = Convert.ToString(Number);
                        #endregion
                        #region Codice Supporto     (Word 5-8  )(Byte 10,17)
                        //----
                        ByteCodiceSupporto[1] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[0] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[3] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[2] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[5] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[4] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[7] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[6] = MsgBytebuffer[sidx++];
                        StrCodiceSupporto = System.Text.Encoding.ASCII.GetString(ByteCodiceSupporto);
                        //----
                        #endregion
                        #region Codice Arrivo       (Word 9-12 )(Byte 18,25)
                        //----
                        ByteCodiceArrivo[1] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[0] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[3] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[2] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[5] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[4] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[7] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[6] = MsgBytebuffer[sidx++];
                        StrCodiceSorgente = System.Text.Encoding.ASCII.GetString(ByteCodiceArrivo);

                        //----
                        #endregion
                        #region Codice Destinazione (Word 13-16)(Byte 26,33)
                        //----
                        ByteCodiceDestinazione[1] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[0] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[3] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[2] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[5] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[4] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[7] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[6] = MsgBytebuffer[sidx++];
                        StrCodiceDestinazione = System.Text.Encoding.ASCII.GetString(ByteCodiceDestinazione);
                        //----
                        #endregion
                        #region PRIO                (Word 17   )(Byte 34,35)
                        //---
                        Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
                        PRIO = Number;
                        //----
                        #endregion
                        #region Read Flag           (Word 18   )(Byte 36,37)
                        //---
                        Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
                        ReadFlag = (byte)Number;
                        //----
                        #endregion
                        #region IC1                             (Byte 510,511)
                        //---
                        Number.Byte1 = MsgBytebuffer[510]; Number.Byte2 = MsgBytebuffer[511];
                        //StrIC1 = Convert.ToString(Number);
                        IC1 = Number;
                        //----
                        #endregion
                        #region Converte in codice dell'elevatore e abilita/disabilita lettura Barcode
                        // Nota: il controllo sul 4rto carattere == "G" è valido per il pian terreno ma non per il promo piano
                        // ---------------------------------------------------------------------------------------------------
                        if (StrCodiceSorgente == "PEC_01  ")                      // Se il trasporto parte dall'elevatore
                        {
                            if (StrCodiceDestinazione.Substring(3, 1) == "G")   // E' da effettuare al piano ground
                            {
                                //if (ReadFlag == 1)                              // Il Codice è da leggere
                                //{
                                    StrCodiceSorgente = "RCCG01  ";               // Converte il nome della stazione sorgente
                                //}
                                //else
                                //{
                                //    // Lascia il nome "PEC_01  "
                                //}
                            }
                            else                                                // Il trasporto E' da effettuare al primo piano
                            {
                                StrCodiceSorgente = "RCCF01  ";                   // Converte il nome della stazione sorgente
                            }
                        }
                        else if (StrCodiceDestinazione == "PEC_01  ")             // Se il trasporto va verso l'elevatore
                        {
                            if (StrCodiceSorgente.Substring(3, 1) == "G")       // E' da effettuare al piano ground
                            {
                                //if (ReadFlag == 1)                              // Il Codice è da leggere
                                //{
                                    StrCodiceDestinazione = "RCCG01  ";           // Converte il nome della stazione sorgente
                                //}
                                //else
                                //{
                                //    // Lascia il nome "PEC_01  "
                                //}
                            }
                            else                                                // Il trasporto E' da effettuare al primo piano
                            {
                                StrCodiceDestinazione = "RCCF01  ";               // Converte il nome della stazione sorgente
                            }
                        }
                        #endregion
                        #region Compose the tokens
                        Asciimessage = IC + "," + PS + "," + AR + "," + StrMSG + "," + StrMISS + "," + StrCodiceSupporto + "," + StrCodiceSorgente + "," + StrCodiceDestinazione + "," + PRIO + "," + ReadFlag + "," + IC1;
                        MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG RECEIVED: ( " + Asciimessage + " )"); 
                        #endregion
                    }
                    break;
                #endregion
                #region RS - Get stato sistema -> 1 Token
                case "RS":  //RS - Get stato sistema
                    {
                        #region IC1
                        // IC1
                        //---
                        Number.Byte1 = MsgBytebuffer[510]; Number.Byte2 = MsgBytebuffer[511];
                        IC1 = Number; 
                        #endregion
                        #region Compose the tokens
                        Asciimessage = IC + "," + PS + "," + AR + "," + StrMSG + "," + IC1;
                        MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG RECEIVED: ( " + Asciimessage + " )"); 
                        #endregion
                    }
                    break;
                #endregion
                #region RI - Get Id Sup -> 1 Token
                case "RI":  //Get Id Sup
                    {
                        #region IC1
                        //---
                        Number.Byte1 = MsgBytebuffer[510]; Number.Byte2 = MsgBytebuffer[511];
                        //StrIC1 = Convert.ToString(Number);
                        IC1 = Number; 
                        #endregion
                        #region Compose the tokens
                        Asciimessage = IC + "," + PS + "," + AR + "," + StrMSG + "," + IC1;
                        MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG RECEIVED: ( " + Asciimessage + " )"); 
                        #endregion
                    }
                    break;
                #endregion
                #region MM - Get Stato missione -> 1 Token
                case "MM":  //Get Stato missione
                    {
                        #region IC1
                        //---
                        Number.Byte1 = MsgBytebuffer[510]; Number.Byte2 = MsgBytebuffer[511];
                        //StrIC1 = Convert.ToString(Number);
                        IC1 = Number; 
                        #endregion
                        #region Compose the tokens
                        Asciimessage = IC + "," + PS + "," + AR + "," + StrMSG + "," + IC1;
                        MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG RECEIVED: ( " + Asciimessage + " )"); 
                        #endregion
                    }
                    break;
                #endregion
                #region SM - Stato Missione (Usato solo a scopo di test)
                case "SM":  //Stato missione - Soltanto a scopo di test
                    {
                        // MISS (Word 4)(Byte 8,9)
                        //---
                        Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
                        StrMISS = Convert.ToString(Number);

                        // Codice supporto (Word 5-8)(Byte 10,17)
                        //----
                        ByteCodiceSupportoOrig[1] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[0] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[3] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[2] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[5] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[4] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[7] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[6] = MsgBytebuffer[sidx++];
                        StrCodiceSupportoOrig = System.Text.Encoding.ASCII.GetString(ByteCodiceSupportoOrig);

                        // Codice supporto (Word 9-12)(Byte 18,25)
                        //----
                        ByteCodiceSupportoChg[1] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoChg[0] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoChg[3] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoChg[2] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoChg[5] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoChg[4] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoChg[7] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoChg[6] = MsgBytebuffer[sidx++];
                        StrCodiceSupportoChg = System.Text.Encoding.ASCII.GetString(ByteCodiceSupportoChg);

                        // Codice Arrivo (Word 13-16)(Byte 26,33)
                        //----
                        ByteCodiceArrivo[1] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[0] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[3] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[2] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[5] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[4] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[7] = MsgBytebuffer[sidx++];
                        ByteCodiceArrivo[6] = MsgBytebuffer[sidx++];
                        StrCodiceSorgente = System.Text.Encoding.ASCII.GetString(ByteCodiceArrivo);

                        // Codice Destinazione (Word 17-20)(Byte 34,41)
                        //----
                        ByteCodiceDestinazione[1] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[0] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[3] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[2] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[5] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[4] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[7] = MsgBytebuffer[sidx++];
                        ByteCodiceDestinazione[6] = MsgBytebuffer[sidx++];
                        StrCodiceDestinazione = System.Text.Encoding.ASCII.GetString(ByteCodiceDestinazione);

                        // PRIO (Word 21)(Byte 42,43)
                        //---
                        Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
                        //StrSTS = Convert.ToString(Number);
                        STS = Number;

                        // IC1
                        //---
                        Number.Byte1 = MsgBytebuffer[510]; Number.Byte2 = MsgBytebuffer[511];
                        //StrIC1 = Convert.ToString(Number);
                        IC1 = Number;

                        Asciimessage = IC + "," + PS + "," + AR + "," + StrMSG + "," + StrMISS + "," + StrCodiceSupportoOrig + "," + StrCodiceSupportoChg + "," + StrCodiceSorgente + "," + StrCodiceDestinazione + "," + STS + "," + IC1;
                        MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG SENT: ( " + Asciimessage + " )");
                        //Console.WriteLine(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "MSG SENT: " + Asciimessage);
                    }
                    break;
                #endregion
                #region SI - Stato Id Sup (Usato solo a scopo di test)
                case "SI":  //Id Sup -  Soltanto a scopo di test
                    {
                        // Codice Agv "Dispositivo" (Word 4-7)(Byte 8,15)
                        //----
                        ByteCodiceSupporto[1] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[0] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[3] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[2] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[5] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[4] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[7] = MsgBytebuffer[sidx++];
                        ByteCodiceSupporto[6] = MsgBytebuffer[sidx++];
                        StrAGV = System.Text.Encoding.ASCII.GetString(ByteCodiceSupporto);

                        // Codice supporto (Word 8-11)(Byte 16,13)
                        //----
                        ByteCodiceSupportoOrig[1] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[0] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[3] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[2] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[5] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[4] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[7] = MsgBytebuffer[sidx++];
                        ByteCodiceSupportoOrig[6] = MsgBytebuffer[sidx++];
                        StrCodiceSupportoOrig = System.Text.Encoding.ASCII.GetString(ByteCodiceSupportoOrig);

                        // Peso (Word 12)(Byte 24,25) Non usato
                        //----
                        sidx++; sidx++;

                        // Stato (Word 13)(Byte 26,27)
                        //----
                        Number.Byte1 = MsgBytebuffer[sidx++]; Number.Byte2 = MsgBytebuffer[sidx++];
                        //StrSTS = Convert.ToString(Number);
                        STS = Number;

                        // IC1
                        //---
                        Number.Byte1 = MsgBytebuffer[510]; Number.Byte2 = MsgBytebuffer[511];
                        //StrIC1 = Convert.ToString(Number);
                        IC1 = Number;

                        Asciimessage = IC + "," + PS + "," + AR + "," + StrMSG + "," + StrAGV + "," + StrCodiceSupportoOrig + "," + "00000" + "," + STS + "," + IC1;
                        MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG SENT: ( " + Asciimessage + " )");
                        //Console.WriteLine(System.DateTime.Now.ToString("yyyy - MM - dd HH: mm:ss") + "MSG SENT: " + Asciimessage);
                    }
                    break;
                #endregion
                #region SS - Stato Sistema (Usato solo a scopo di test)
                case "SS":  //Id Sup -  Soltanto a scopo di test
                    {
                        // Codice supporto (Word 4-6)(Byte 8,13)
                        //----
                        Number = 0;                             //Converte il numero in Byte
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];

                        // Numero missioni (Word 7)(Byte 14,15)
                        //----
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        MISS = Number;

                        // Codice supporto (Word 8-13)(Byte 16,27)
                        //----
                        Number = 0;                             //Converte il numero in Byte
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];

                        // Codice supporto (Word 14-16)(Byte 28,33) - Campo vuoto
                        //----                      
                        ByteAGV[1] = MsgBytebuffer[sidx++];
                        ByteAGV[0] = MsgBytebuffer[sidx++];
                        ByteAGV[3] = MsgBytebuffer[sidx++];
                        ByteAGV[2] = MsgBytebuffer[sidx++];
                        ByteAGV[5] = MsgBytebuffer[sidx++];
                        ByteAGV[4] = MsgBytebuffer[sidx++];
                        StrAGV = System.Text.Encoding.ASCII.GetString(ByteAGV);

                        // Codice supporto (Word 17)(Byte 34,35)
                        //----
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        StrSTS = Convert.ToString(Number);    //Converte la stringa in numero

                        // Codice supporto (Word 18-20)(Byte 36,41) - Campo vuoto
                        //----                      
                        ByteAGV[1] = MsgBytebuffer[sidx++];
                        ByteAGV[0] = MsgBytebuffer[sidx++];
                        ByteAGV[3] = MsgBytebuffer[sidx++];
                        ByteAGV[2] = MsgBytebuffer[sidx++];
                        ByteAGV[5] = MsgBytebuffer[sidx++];
                        ByteAGV[4] = MsgBytebuffer[sidx++];
                        StrAGV = System.Text.Encoding.ASCII.GetString(ByteAGV);

                        // Codice supporto (Word 21)(Byte 42,43)
                        //----
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        StrSTS = Convert.ToString(Number);    //Converte la stringa in numero

                        // Codice supporto (Word 22-24)(Byte 44,49) - Primo AGV
                        //----
                        ByteAGV[1] = MsgBytebuffer[sidx++];
                        ByteAGV[0] = MsgBytebuffer[sidx++];
                        ByteAGV[3] = MsgBytebuffer[sidx++];
                        ByteAGV[2] = MsgBytebuffer[sidx++];
                        ByteAGV[5] = MsgBytebuffer[sidx++];
                        ByteAGV[4] = MsgBytebuffer[sidx++];
                        StrAGV2 = System.Text.Encoding.ASCII.GetString(ByteAGV);

                        // Codice supporto (Word 25)(Byte 50,51)
                        //----
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        StrSTS2 = Convert.ToString(Number);    //Converte la stringa in numero

                        // Codice supporto (Word 26.28)(Byte 52,57) - Secondo agv
                        //----                     
                        ByteAGV[1] = MsgBytebuffer[sidx++];
                        ByteAGV[0] = MsgBytebuffer[sidx++];
                        ByteAGV[3] = MsgBytebuffer[sidx++];
                        ByteAGV[2] = MsgBytebuffer[sidx++];
                        ByteAGV[5] = MsgBytebuffer[sidx++];
                        ByteAGV[4] = MsgBytebuffer[sidx++];
                        StrAGV3 = System.Text.Encoding.ASCII.GetString(ByteAGV);


                        // Codice supporto (Word 29)(Byte 58,59)
                        //----
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        StrSTS3 = Convert.ToString(Number);    //Converte la stringa in numero

                        // Codice supporto (Word 30-32)(Byte 60,65) - Terzo AGV
                        //----
                        ByteAGV[1] = MsgBytebuffer[sidx++];
                        ByteAGV[0] = MsgBytebuffer[sidx++];
                        ByteAGV[3] = MsgBytebuffer[sidx++];
                        ByteAGV[2] = MsgBytebuffer[sidx++];
                        ByteAGV[5] = MsgBytebuffer[sidx++];
                        ByteAGV[4] = MsgBytebuffer[sidx++];
                        StrAGV4 = System.Text.Encoding.ASCII.GetString(ByteAGV);

                        // Codice supporto (Word 33)(Byte 66,67)
                        //----
                        Number.Byte1 = MsgBytebuffer[sidx++];
                        Number.Byte2 = MsgBytebuffer[sidx++];
                        StrSTS4 = Convert.ToString(Number);    //Converte la stringa in numero

                        // IC1
                        //---
                        Number.Byte1 = MsgBytebuffer[510];
                        Number.Byte2 = MsgBytebuffer[511];
                        IC1 = Number;
                        Asciimessage = IC + "," + PS + "," + AR + "," + StrMSG + "," + StrAGV + "," + StrSTS + "," + StrAGV2 + "," + StrSTS2 + "," + StrAGV3 + "," + StrSTS3 + "," + StrAGV4 + "," + StrSTS4 + "," + IC1;
                        MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "MSG SENT: ( " + Asciimessage + " )");
                        //Console.WriteLine(System.DateTime.Now.ToString("yyyy - MM - dd HH: mm:ss") + "MSG SENT: " + Asciimessage);
                #endregion
                    }
                    break;
                default:
                    { }
                    break;
            } 
            #endregion
            return (Asciimessage);
        }
        /// <summary>
        /// <para>Riceve il messaggio in sequenza di token </para>
        /// <para>trasforma i valori settando delle variabili interne</para>
        /// <para>restituisce un messaggio in ByteArray da inviare a Host</para>
        /// <para>ral 171201 changed</para>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] PackHostMessage(string[] tokens)
        {
            #region Parte dichiarativa
            int sidx; // Send buffer index
            int tkidx = 0;

            byte[] sendword = new byte[2];
            byte[] sendbuffer = new byte[512];
            UInt16Converter Number;

            UInt16 IC, IC1, PS, AR, MISS;
            string StrIC = "", StrPS = "", StrAR = "", StrMSG = "", StrMISS = "", StrAgv = "", StrCodiceSupportoOrig = "";
            string StrAgv2="",StrSTS2="",StrAgv3="",StrSTS3="",StrAgv4="",StrSTS4="";
            string StrCodiceSupportoChg = "", StrCodiceSorgente = "", StrCodiceDestinazione = "", StrSTS = "", StrIC1 = "";
            byte[] ByteMSG, ByteAgv, ByteCodiceSupportoOrig, ByteCodiceArrivo, ByteCodiceDestinazione;

            #endregion
            #region Raccolta dati
            /// Raccolta dati da coda o da UDP
            /// -----------------------------------------------------------------------------------------------------------
            StrIC = tokens[tkidx++].PadRight(5, ' ');
            StrPS = tokens[tkidx++].PadRight(5, ' ');
            StrAR = tokens[tkidx++].PadRight(5, ' ');
            StrMSG = tokens[tkidx++].PadRight(2, ' ');
            switch (StrMSG)
            {
                #region RM - Exec Missione
                case "RM":
                    {
                        StrMISS = tokens[tkidx++].PadRight(5, ' ');
                        StrCodiceSupportoOrig = tokens[tkidx++].PadRight(8, ' ');
                        if (tokens[tkidx++] == "RCCF01  " || tokens[tkidx] == "RCCF01  ") StrCodiceSorgente = "PEC_01  ";
                        else StrCodiceSorgente = tokens[tkidx].PadRight(8, ' ');
                        if (tokens[tkidx++] == "RCCF01  " || tokens[tkidx] == "RCCF01  ") StrCodiceDestinazione = "PEC_01  ";
                        else StrCodiceDestinazione = tokens[tkidx].PadRight(8, ' ');
                        StrSTS = tokens[tkidx++].PadLeft(5, '0');
                        StrIC1 = tokens[tkidx++].PadRight(5, ' ');
                    }
                    break; 
                #endregion
                #region SM - Stato Missione
                case "SM":
                    {
                        StrMISS = tokens[tkidx++].PadRight(5, ' ');
                        StrCodiceSupportoOrig = tokens[tkidx++].PadRight(8, ' ');
                        StrCodiceSupportoChg = tokens[tkidx++].PadRight(8, '0');
                        if (tokens[tkidx++] == "RCCF01  " || tokens[tkidx] == "RCCF01  ") StrCodiceSorgente = "PEC_01  ";
                        else StrCodiceSorgente = tokens[tkidx].PadRight(8, ' ');
                        if (tokens[tkidx++] == "RCCF01  " || tokens[tkidx] == "RCCF01  ") StrCodiceDestinazione = "PEC_01  ";
                        else StrCodiceDestinazione = tokens[tkidx].PadRight(8, ' ');
                        StrSTS = tokens[tkidx++].PadLeft(5, '0');
                        StrIC1 = tokens[tkidx++].PadRight(5, ' ');
                    }
                    break; 
                #endregion
                #region SI - Stato Id Sup
                case "SI":
                    {
                        StrAgv = tokens[tkidx++];
                        if (StrAgv == "1" || StrAgv == "2")             // Ground Floor
                            StrAgv = "AVCG0" + StrAgv;
                        else if (StrAgv == "3" || StrAgv == "4")        // First Floor
                            StrAgv = "AVCF0" + StrAgv;
                        StrAgv = StrAgv.PadRight(8, ' ');

                        StrCodiceSupportoOrig = tokens[tkidx++].PadRight(8, ' ');
                        StrSTS = tokens[tkidx++].PadRight(16, '0'); //ral 171201 added
                        StrIC1 = tokens[tkidx++].PadRight(5, ' ');
                    }
                    break;   
                #endregion
                #region RI - Get Id Sup
                case "RI":
                    {
                        StrIC1 = tokens[tkidx++].PadRight(5, ' ');
                    }
                    break; 
                #endregion
                default:
                    break;
            }
            #endregion
            #region Codifica byte array
            /// Codifica in ByteArray
            /// -----------------------------------------------------------------------------------------------------------

            #region Parte comune a tutti i messaggi
            // IC (Word 0)(Byte 0,1)
            //---
            sidx = 0;
            IC = Convert.ToUInt16(StrIC);    //Converte la stringa in numero
            Number = IC;                     //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // PS (Word 1)(Byte 2,3)
            //---
            PS = Convert.ToUInt16(StrPS);    //Converte la stringa in numero
            Number = PS;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // AR (Word 2)(Byte 4,5)
            //---
            AR = Convert.ToUInt16(StrAR);    //Converte la stringa in numero
            Number = AR;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // MSG (Word 3)(Byte 6,7)-----Qui sappiamo il tipo di messaggio
            //----
            ByteMSG = StringToByteArray(StrMSG); //Converte la stringa in Byte
            sendbuffer[sidx++] = ByteMSG[1];
            sendbuffer[sidx++] = ByteMSG[0];
            #endregion

            switch (StrMSG)
            {
                #region SM
                case "SM":
                    {
                        // MISS (Word 4)(Byte 8,9)
                        //---
                        MISS = Convert.ToUInt16(StrMISS);    //Converte la stringa in numero
                        Number = MISS;                    //Converte il numero in Byte
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // Codice supporto (Word 5-8)(Byte 10,17)
                        //----
                        ByteCodiceSupportoOrig = StringToByteArray(StrCodiceSupportoOrig); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[1];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[0];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[3];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[2];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[5];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[4];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[7];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[6];

                        // Codice supporto (Word 5-12)(Byte 18,25) - La mette a zero
                        //----
                        //ByteCodiceSupportoChg = StringToByteArray(StrCodiceSupportoChg); //Converte la stringa in Byte
                        Number = 0;                    //Converte il numero in Byte
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte1;

                        // Codice Arrivo (Word 9-12)(Byte 18,25)
                        //----
                        
                        if (StrCodiceSorgente == "RCCG01  " || StrCodiceSorgente == "RCCF01  ")
                        {
                            StrCodiceSorgente = "PEC_01  ";
                        }
                        ByteCodiceArrivo = StringToByteArray(StrCodiceSorgente); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteCodiceArrivo[1];
                        sendbuffer[sidx++] = ByteCodiceArrivo[0];
                        sendbuffer[sidx++] = ByteCodiceArrivo[3];
                        sendbuffer[sidx++] = ByteCodiceArrivo[2];
                        sendbuffer[sidx++] = ByteCodiceArrivo[5];
                        sendbuffer[sidx++] = ByteCodiceArrivo[4];
                        sendbuffer[sidx++] = ByteCodiceArrivo[7];
                        sendbuffer[sidx++] = ByteCodiceArrivo[6]; 

                        // Codice Destinazione (Word 13-16)(Byte 26,33)
                        //----
                        if (StrCodiceDestinazione == "RCCG01  " || StrCodiceDestinazione == "RCCF01  ")
                        {
                            StrCodiceDestinazione = "PEC_01  ";
                        }
                        ByteCodiceDestinazione = StringToByteArray(StrCodiceDestinazione); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteCodiceDestinazione[1];
                        sendbuffer[sidx++] = ByteCodiceDestinazione[0];
                        sendbuffer[sidx++] = ByteCodiceDestinazione[3];
                        sendbuffer[sidx++] = ByteCodiceDestinazione[2];
                        sendbuffer[sidx++] = ByteCodiceDestinazione[5];
                        sendbuffer[sidx++] = ByteCodiceDestinazione[4];
                        sendbuffer[sidx++] = ByteCodiceDestinazione[7];
                        sendbuffer[sidx++] = ByteCodiceDestinazione[6];

                        // STATO (Word 17)(Byte 34,35)
                        //---
                        Number = Convert.ToUInt16(StrSTS.Replace(" ", string.Empty));    //Converte la stringa in numero
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // NULL DATA (Word 17)(Byte 34,35)
                        //---
                        Number = 0;                    //Converte il numero in Byte
                        for (int i = sidx; i < 509; )
                        {
                            sendbuffer[i++] = Number.Byte1;
                            sendbuffer[i++] = Number.Byte2;
                        }

                        // IC1
                        //---
                        IC1 = Convert.ToUInt16(StrIC1);    //Converte la stringa in numero
                        Number = IC1;                      //Converte il numero in Byte
                        sendbuffer[510] = Number.Byte1;
                        sendbuffer[511] = Number.Byte2;
                    }
                    break;
                #endregion
                #region SI
                case "SI":
                    {
                        // Codice supporto (Word 4-7)(Byte 8,15)
                        //----
                        ByteAgv = StringToByteArray(StrAgv); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteAgv[1];
                        sendbuffer[sidx++] = ByteAgv[0];
                        sendbuffer[sidx++] = ByteAgv[3];
                        sendbuffer[sidx++] = ByteAgv[2];
                        sendbuffer[sidx++] = ByteAgv[5];
                        sendbuffer[sidx++] = ByteAgv[4];
                        sendbuffer[sidx++] = ByteAgv[7];
                        sendbuffer[sidx++] = ByteAgv[6];

                        // Codice supporto (Word 8-11)(Byte 16,23)
                        //----
                        ByteCodiceSupportoOrig = StringToByteArray(StrCodiceSupportoOrig); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[1];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[0];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[3];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[2];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[5];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[4];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[7];
                        sendbuffer[sidx++] = ByteCodiceSupportoOrig[6];

                        // NULL (Word 12)(Byte 24,25)   ral 171201 added
                        //----
                        Number = 0;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // NULL (Word 13)(Byte 26,27)   ral 171201 added
                        //----
                        Number = Convert.ToUInt16(StrSTS.Replace(" ", string.Empty));
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // IC1
                        //---
                        IC1 = Convert.ToUInt16(StrIC1);    //Converte la stringa in numero
                        Number = IC1;                      //Converte il numero in Byte
                        sendbuffer[510] = Number.Byte1;
                        sendbuffer[511] = Number.Byte2;
                    }
                    break;
                #endregion
                #region SS
                case "SS":
                    {
                        // Byte vuoti (Word 4-6)(Byte 8,13)
                        //----
                        Number = 0;                             //Converte il numero in Byte
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // Numero di missioni nel sistema (Word 7)(Byte 14,15)
                        //----
                        if (StrMISS != "")
                        {
                            MISS = Convert.ToUInt16(StrMISS);       //Converte la stringa in numero 
                        }
                        else
                        {
                            MISS = 0;
                        }
                        Number = MISS;                          //Converte il numero in Byte
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // Codice supporto (Word 8-13)(Byte 16,27)
                        //----
                        Number = 0;                             //Converte il numero in Byte
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // Codice supporto (Word 14-16)(Byte 28,33) - Nome primo AGV
                        //----
                        ByteAgv = StringToByteArray("000000"); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteAgv[1];
                        sendbuffer[sidx++] = ByteAgv[0];
                        sendbuffer[sidx++] = ByteAgv[3];
                        sendbuffer[sidx++] = ByteAgv[2];
                        sendbuffer[sidx++] = ByteAgv[5];
                        sendbuffer[sidx++] = ByteAgv[4];

                        // Codice supporto (Word 17)(Byte 34,35) - Stato primo AGV
                        //----
                        Number = 0;    //Converte la stringa in numero
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // Codice supporto (Word 18-29)(Byte 36,59) - Nome primo AGV
                        //----
                        StrAgv = tokens[5];
                        ByteAgv = StringToByteArray(StrAgv); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteAgv[1];
                        sendbuffer[sidx++] = ByteAgv[0];
                        sendbuffer[sidx++] = ByteAgv[3];
                        sendbuffer[sidx++] = ByteAgv[2];
                        sendbuffer[sidx++] = ByteAgv[5];
                        sendbuffer[sidx++] = ByteAgv[4];

                        // Codice supporto (Word 21)(Byte 42,43) - Stato primo AGV
                        //----
                        StrSTS = tokens[6];
                        Number = Convert.ToUInt16(StrSTS.Replace(" ", string.Empty));    //Converte la stringa in numero
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // Codice supporto (Word 22-24)(Byte 44,49) - Nome secondo AGV
                        //----
                        StrAgv2 = tokens[7];
                        ByteAgv = StringToByteArray(StrAgv2); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteAgv[1];
                        sendbuffer[sidx++] = ByteAgv[0];
                        sendbuffer[sidx++] = ByteAgv[3];
                        sendbuffer[sidx++] = ByteAgv[2];
                        sendbuffer[sidx++] = ByteAgv[5];
                        sendbuffer[sidx++] = ByteAgv[4];

                        // Codice supporto (Word 25)(Byte 50,51) - Stato AGV
                        //----
                        StrSTS2 = tokens[8];
                        Number = Convert.ToUInt16(StrSTS2);    //Converte la stringa in numero
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2; ;

                        // Codice supporto (Word 26-28)(Byte 52,57) - Nome terzo AGV
                        //----
                        StrAgv3 = tokens[9];
                        ByteAgv = StringToByteArray(StrAgv3); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteAgv[1];
                        sendbuffer[sidx++] = ByteAgv[0];
                        sendbuffer[sidx++] = ByteAgv[3];
                        sendbuffer[sidx++] = ByteAgv[2];
                        sendbuffer[sidx++] = ByteAgv[5];
                        sendbuffer[sidx++] = ByteAgv[4];

                        // Codice supporto (Word 29)(Byte 58,59) - Stato AGV
                        //----
                        StrSTS3 = tokens[10];
                        Number = Convert.ToUInt16(StrSTS3);    //Converte la stringa in numero
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // Codice supporto (Word 30-32)(Byte 60,65) - Nome quarto AGV
                        //----
                        StrAgv4 = tokens[11];
                        ByteAgv = StringToByteArray(StrAgv4); //Converte la stringa in Byte
                        sendbuffer[sidx++] = ByteAgv[1];
                        sendbuffer[sidx++] = ByteAgv[0];
                        sendbuffer[sidx++] = ByteAgv[3];
                        sendbuffer[sidx++] = ByteAgv[2];
                        sendbuffer[sidx++] = ByteAgv[5];
                        sendbuffer[sidx++] = ByteAgv[4];

                        // Codice supporto (Word 33)(Byte 66,67) - Stato AGV
                        //----
                        StrSTS4 = tokens[12];
                        Number = Convert.ToUInt16(StrSTS4);    //Converte la stringa in numero
                        sendbuffer[sidx++] = Number.Byte1;
                        sendbuffer[sidx++] = Number.Byte2;

                        // NULL DATA (Word 34-254)(Byte 68,509)
                        //---
                        Number = 0;                    //Converte il numero in Byte
                        for (int i = sidx; i < 510; )
                        {
                            sendbuffer[i++] = Number.Byte1;
                            sendbuffer[i++] = Number.Byte2;
                        }

                        // IC1
                        //---
                        IC1 = Convert.ToUInt16(StrIC);    //Converte la stringa in numero
                        Number = IC1;                      //Converte il numero in Byte
                        sendbuffer[510] = Number.Byte1;
                        sendbuffer[511] = Number.Byte2;
                    }
                    break;
                #endregion
                default:
                    break;
            } 
            #endregion

            return (sendbuffer);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string PrepareHostMessage(int Ic, int Ps, int ra, string Message)
        {
            string MessageToSend = "";
            return (MessageToSend);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string PrepareHostAck(int Ic,int Pa, int ra, int Di)
        {
            string AckToSend = "";
            string stric,strpa;
            stric = Ic.ToString();
            stric =stric.PadLeft(5, '0');
            strpa = Pa.ToString();
            strpa = strpa.PadLeft(5, '0');
            AckToSend = stric + "," + strpa + "," + ra.ToString() + "," + Di.ToString() + "," + stric;
            return (AckToSend);
        }
        /// <summary>
        /// Strasforma un Ack, da lista di token stringhe a ByteArray nel formato richiesto
        /// da Host
        /// [Ic],[Ps],[Ra],[Di],[PsEnd]
        /// </summary>
        /// <param name="ackmessage">Ascii string in token separati da virgola</param>
        /// <returns>ByteArray</returns>
        public byte[] PackHostAck(string ackmessage)
        {
            #region Parte dichiarativa
            byte[] sendbuffer = new byte[512];
            int sidx; // Send buffer index
            int tkidx = 0;

            byte[] sendword = new byte[2];
            UInt16Converter Number;

            UInt16 IC, IC1, PS, AR, DI;
            string StrIC = "", StrPS = "", StrAR = "", StrDI = "";
            string StrIC1 = "";
            string[] tokens = new string[20];

            tokens = ackmessage.Split(',');
            #endregion
            #region Preparazione messaggio

            StrIC = tokens[tkidx++].PadRight(5, ' ');
            StrPS = tokens[tkidx++].PadRight(5, ' ');
            StrAR = tokens[tkidx++].PadLeft(5, '0');
            StrDI = tokens[tkidx++].PadLeft(5, '0');
            StrIC1 = tokens[tkidx++].PadRight(5, ' ');


            // IC (Word 0)(Byte 0,1)
            //---
            sidx = 0;
            IC = Convert.ToUInt16(StrIC);    //Converte la stringa in numero
            Number = IC;                     //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // PS (Word 1)(Byte 2,3)
            //---
            PS = Convert.ToUInt16(StrPS);    //Converte la stringa in numero
            Number = PS;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // AR (Word 2)(Byte 4,5)
            //---
            AR = Convert.ToUInt16(StrAR);    //Converte la stringa in numero
            Number = AR;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // DI (Word 3)(Byte 6,7)
            //---
            DI = Convert.ToUInt16(StrDI);    //Converte la stringa in numero
            Number = DI;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // IC1 (Word 4)(Byte 8,9)
            //---
            IC1 = Convert.ToUInt16(StrIC1);    //Converte la stringa in numero
            Number = IC1;                      //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;
            #endregion

            return (sendbuffer);
        }

        /// <summary>
        /// <para>Raccoglie tutti i dati per il messaggio Stato Missione (SM) da inviare a Host</para>
        /// <para>Restituisce il messaggio in formato stringa</para>
        /// </summary>
        /// <param name="message">"[TS],[TID],[LID],[AGVNO],[Location],[phase],[description]"</param>
        /// <returns>"[IC],[PS],[RA],[SM],[TID],[UDC],[N/U],[SRC],[DST],[STS],[IC]"</returns>
        private string PrepareToQueueSMMessage(string message)
        {
            string[] tokens = new string[20];
            string TroRecord;
            string[] MasterTokens = new string[20];
            int tid;                        //Identificativo del trasporto
            int agvno;                      //Numero dell'Agv a cui il trasporto è stato assegnato
            int msgic, msgps, msgar;
            string prepmessage = "";        //Il messaggio da inviare a Host (senza testa e coda)

            string StrIC, StrPS, StrAR, StrCOD, StrTID;
            string StrCodiceSupportoOrig, StrCodiceSupportoChg, StrCodiceArrivo;
            string StrCodiceDestinazione, StrSTS = "";

            OracleTransaction ZoneTrans = null;

            tokens = message.Split(',');
            //In base alla phase aggiorna il master dei trasporti e manda un telegramma
            //se necessario, manda un telegramma a Host

            tid = System.Convert.ToInt32(tokens[1]);
            agvno = System.Convert.ToInt32(tokens[3]);
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                #region Prepara il telegramma da inviare a Host
                TroRecord = GetOrderFromMaster(tid);                        //Legge dal DB il record del trasporto master
                MasterTokens = TroRecord.Split(',');                        //Lo scompone in tokens
                msgic = 0;// GetNextNodeIc();                          //calcola l'Ic da utilizzare nel messaggio  
                StrIC = System.Convert.ToString(msgic).PadRight(5, ' ');    //Codice di correttezza iniziale (IC)
                msgps = 0;// GetNextNodePs();
                StrPS = System.Convert.ToString(msgps).PadRight(5, ' ');    //Progressivo messaggio (PS)
                msgar = 1;
                StrAR = System.Convert.ToString(msgar).PadRight(5, ' ');    //Richiesta di ack (AR)
                StrCOD = "SM";
                StrCOD = StrCOD.PadRight(2, ' ');                           //Codice del messaggio (SM)
                StrTID = System.Convert.ToString(tid).PadRight(5, ' ');
                StrTID = StrTID.PadRight(5, ' ');                           //Numero del trasporto (Tid)
                StrCodiceSupportoOrig = MasterTokens[3].PadRight(8, ' ');   //Udc
                StrCodiceSupportoChg = "00000000";
                StrCodiceArrivo = MasterTokens[4].PadRight(8, ' ');         //Stazione di origine
                StrCodiceDestinazione = MasterTokens[5].PadRight(8, ' ');   //Stazione di destinazione
                switch (tokens[5])                                          //Stato del trasporto
                {
                    case "1":
                        StrSTS = "1"; //on going
                        break;
                    case "2":
                        StrSTS = "1"; //on going
                        break;
                    case "3":
                    case "4":
                        StrSTS = "2"; //completed
                        break;
                    default:
                        break;
                }
                StrSTS = StrSTS.PadRight(5, ' ');
                prepmessage = StrIC + "," + StrPS + "," + StrAR + "," +     //Pacchettizza il corpo del messaggio
                            StrCOD + "," + StrTID + "," +
                            StrCodiceSupportoOrig + "," +
                            StrCodiceSupportoChg + "," +
                            StrCodiceArrivo + "," + StrCodiceDestinazione + "," +
                            StrSTS + "," + StrIC;
                //ZoneTrans.Commit();
                #endregion
            }
            catch (OracleException ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, prepmessage, ex.Message);
                ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, prepmessage, ex.Message);
                ZoneTrans.Rollback();
            }

            return (prepmessage);
        }

        private string PrepareToQueueSMMessage(string StrTid, string Lid, string Src, string Dst, string Sts)
        {
            string[] tokens = new string[20];
            int Tid;
            string TroRecord;
            string[] MasterTokens = new string[20];
            int msgic, msgps, msgar;
            string prepmessage = "";        //Il messaggio da inviare a Host (senza testa e coda)

            string StrIC, StrPS, StrAR, StrCOD, StrTID;
            string StrLid, StrLidChg;

            OracleTransaction ZoneTrans = null;

            //In base alla phase aggiorna il master dei trasporti e manda un telegramma
            //se necessario, manda un telegramma a Host

            Tid = System.Convert.ToInt32(StrTid);
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                #region Prepara il telegramma da inviare a Host
                TroRecord = GetOrderFromMaster(Tid);                        //Legge dal DB il record del trasporto master
                MasterTokens = TroRecord.Split(',');                        //Lo scompone in tokens
                msgic = 0;// GetNextNodeIc();                          //calcola l'Ic da utilizzare nel messaggio  
                StrIC = System.Convert.ToString(msgic).PadRight(5, ' ');    //Codice di correttezza iniziale (IC)
                msgps = 0;// GetNextNodePs();
                StrPS = System.Convert.ToString(msgps).PadRight(5, ' ');    //Progressivo messaggio (PS)
                msgar = 1;
                StrAR = System.Convert.ToString(msgar).PadRight(5, ' ');    //Richiesta di ack (AR)
                StrCOD = "SM";
                StrCOD = StrCOD.PadRight(2, ' ');                           //Codice del messaggio (SM)
                StrTID = System.Convert.ToString(Tid).PadRight(5, ' ');
                StrTID = StrTID.PadRight(5, ' ');                           //Numero del trasporto (Tid)
                StrLid = Lid.PadRight(8, ' ');   //Udc
                StrLidChg = "00000000";
                Src = Src.PadRight(8, ' ');         //Stazione di origine
                Dst = Dst.PadRight(8, ' ');   //Stazione di destinazione
                Sts = Sts.PadRight(5, ' ');                  //Stato del trasporto
                prepmessage = StrIC + "," + StrPS + "," + StrAR + "," +     //Pacchettizza il corpo del messaggio
                            StrCOD + "," + StrTID + "," +
                            StrLid + "," +
                            StrLidChg + "," +
                            Src + "," + Dst + "," +
                            Sts + "," + StrIC;
                //ZoneTrans.Commit();
                #endregion
            }
            catch (OracleException ex)
            {
                ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "PrepareToQueueSMMessage", ex.Message);
                ZoneTrans.Rollback();
            }

            return (prepmessage);
        }
        /// <summary>
        /// Raccoglie tutti i dati per il messaggio Stato Sistema (SS) da inviare a Host
        /// </summary>
        /// <returns>"[IC],[PS],[AR],[SS],[N.Miss][0]..[0][Punto.Impianto],[Stato],[Agv1],[Stato],[Agv2],[Stato],[Agv3],[Stato],[Agv4],[Stato],[IC1]"</returns>
        private string PrepareToQueueSSMessage()
        {
            string[] tokens = new string[20];
            string[] MasterTokens = new string[20];
            int msgic, msgps, msgar;
            string prepmessage = "";        //Il messaggio da inviare a Host (senza testa e coda)

            string StrIC, StrPS, StrAR, StrCOD, StrSISS, StrAGV1,StrAGV2,StrAGV3,StrAGV4;
            string StrSTS1, StrSTS2, StrSTS3, StrSTS4;

            if (GetSystemStatus() == "RUN") StrSISS = "0";                  // Sistema in RUN
            else StrSISS = "1";                                             // Sistema in Shutdonw
            StrSTS1 = "0";
            StrSTS2 = "0";
            StrSTS3 = "0";
            StrSTS4 = "0";

            //OracleTransaction ZoneTrans = null;

            //In base alla phase aggiorna il master dei trasporti e manda un telegramma
            //se necessario, manda un telegramma a Host

            //tid = System.Convert.ToInt32(tokens[1]);
            //agvno = System.Convert.ToInt32(tokens[3]);
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                #region Prepara il telegramma da inviare a Host
                //TroRecord = GetOrderFromMaster(tid);                        //Legge dal DB il record del trasporto master
                //MasterTokens = TroRecord.Split(',');                        //Lo scompone in tokens

                msgic = 0;// GetNextNodeIc();                                    //calcola l'Ic da utilizzare nel messaggio  
                StrIC = System.Convert.ToString(msgic).PadRight(5, ' ');    //Codice di correttezza iniziale (IC)
                msgps = 0;// GetNextNodePs();
                StrPS = System.Convert.ToString(msgps).PadRight(5, ' ');    //Progressivo messaggio (PS)
                msgar = 1;
                StrAR = System.Convert.ToString(msgar).PadRight(5, ' ');    //Richiesta di ack (AR)
                StrCOD = "SS";
                StrCOD = StrCOD.PadRight(2, ' ');                           //Codice del messaggio (SS)
                //StrSISS = System.Convert.ToString(tid).PadRight(5, ' ');
                StrSISS = StrSISS.PadRight(5, ' ');                             //Stato del sistema (Run = 0, Shotsone = 1)
                StrAGV1 = "AVCG01  ";                                       //Identificativo AGV
                StrSTS1 = StrSTS1.PadLeft(5, '0');                          //Stato dell'AGV (0 ok, 1 off)
                StrAGV2 = "AVCG02  ";                                       //Identificativo AGV
                StrSTS2 = StrSTS2.PadLeft(5, '0');                          //Stato dell'AGV (0 ok, 1 off)
                StrAGV3 = "AVCF01  ";                                       //Identificativo AGV
                StrSTS3 = StrSTS3.PadLeft(5, '0');                          //Stato dell'AGV (0 ok, 1 off)
                StrAGV4 = "AVCF02  ";                                       //Identificativo AGV
                StrSTS4 = StrSTS4.PadLeft(5, '0');                          //Stato dell'AGV (0 ok, 1 off)

                prepmessage = StrIC + "," + StrPS + "," + StrAR + "," +     //Pacchettizza il corpo del messaggio
                            StrCOD + "," + StrSISS + "," +
                            StrAGV1 + "," + StrSTS1 + "," +
                            StrAGV2 + "," + StrSTS2 + "," +
                            StrAGV3 + "," + StrSTS3 + "," +
                            StrAGV4 + "," + StrSTS4 + "," +
                            StrIC;
                //ZoneTrans.Commit();
                #endregion
            }
            catch (OracleException ex)
            {
                //ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "PrepareToQueueSSMessage", ex.Message);
                //ZoneTrans.Rollback();
            }

            return (prepmessage);
        }
        /// <summary>
        /// <para>Messaggio per la comunicazione dell'IdSupporto</para>
        /// <para>un messaggio per Agv, a chiusura un udc "000000"</para>
        /// <para>ral 171201 Changed - add parameter BcrFlg</para>
        /// </summary>
        /// <param name="AgvId"></param>
        /// <param name="Udc"></param>
        /// <returns>"[IC],[PS],[AR],[SI],[AGV],[UDC],[   ],[FLG],[IC1]"</returns>
        private string PrepareToQueueSIMessage(string AgvId, string Udc, string BcrFlg)
        {
            string[] tokens = new string[20];
            string[] MasterTokens = new string[20];
            string EndToken = "00000000";
            int msgic, msgps, msgar;
            string prepmessage = "";        //Il messaggio da inviare a Host (senza testa e coda)
            string StrIC, StrPS, StrAR, StrCOD, StrAGV;
            string StrUdc = Udc;
            string[] LidList = new string[4];

            OracleTransaction ZoneTrans = null;

            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                #region Prepara il telegramma da inviare a Host

                msgic = 0;// GetNextNodeIc();                               //calcola l'Ic da utilizzare nel messaggio  
                StrIC = System.Convert.ToString(msgic).PadRight(5, ' ');    //Codice di correttezza iniziale (IC)
                msgps = 0;// GetNextNodePs();
                StrPS = System.Convert.ToString(msgps).PadRight(5, ' ');    //Progressivo messaggio (PS)
                msgar = 1;
                StrAR = System.Convert.ToString(msgar).PadRight(5, ' ');    //Richiesta di ack (AR)
                StrCOD = "SI";
                StrCOD = StrCOD.PadRight(2, ' ');                           //Codice del messaggio (SI)
                LidList = GetAgvLids().Split(',');                          //Ricava gli Udc dai trasporti Attivi (Agv Con Carico A Bordo)
                if (Udc == EndToken)
                {
                    StrAGV = "";
                    StrAGV = StrAGV.PadRight(8, ' ');
                    StrUdc = EndToken;
                }
                else
                {
                    StrAGV = AgvId;
                    StrAGV = StrAGV.PadRight(8, ' ');
                    switch (StrAGV.Trim())
                    {
                        case "AVCG01":
                            {
                                Udc = LidList[0].Split(':')[1];
                            }    
                            break;
                        case "AVCG02":
                            {
                                Udc = LidList[1].Split(':')[1];
                            }
                            break;
                        case "AVCF01":
                            {
                                Udc = LidList[2].Split(':')[1];
                            }
                            break;
                        case "AVCF02":
                            {
                                Udc = LidList[3].Split(':')[1];
                            }
                            break;
                        default:
                            break;
                    }
                    StrUdc = Udc.PadRight(8, ' ');                             // Ricava l'UDC dal master dei trasporti
                }
                BcrFlg = BcrFlg.PadLeft(2, '0');

                prepmessage = StrIC + "," + StrPS + "," + StrAR + "," +     // Pacchettizza il corpo del messaggio
                            StrCOD + "," + StrAGV + "," + StrUdc + "," +                        
                            "00" + "," + BcrFlg + StrIC;   // ral 101201 Changed
                //ZoneTrans.Commit();
                #endregion
            }
            catch (OracleException ex)
            {
                ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "PrepareToQueueSIMessage", ex.Message);
                ZoneTrans.Rollback();
            }

            return (prepmessage);
        }
        /// <summary>
        /// Prepara il telergramma Stato Missione (SM) da inviare a Host
        /// </summary>
        /// <param name="message">"[IC],[PS],[RA],[SM],[TID],[UDC],[N/U],[SRC],[DST],[STS],[IC]"</param>
        /// <returns>ByteArray, pronto all'invio a Host</returns>
        private byte[] PrepareMsgStatoMissione(string message)
        {
            #region Parte dichiarativa
            int sidx; // Send buffer index
            int tkidx = 0;

            byte[] sendword = new byte[2];
            byte[] sendbuffer = new byte[512];
            byte[] ByteAgv = new byte[6];
            byte[] ByteLayoutPoint = new byte[8];
            UInt16Converter Number;

            UInt16 IC, IC1, PS, AR, MISS;
            string StrIC = "", StrPS = "", StrRA = "", StrCOD = "", StrTID = "", StrCodiceSupportoOrig = "";
            string StrCodiceSupportoChg = "", StrCodiceArrivo = "", StrCodiceDestinazione = "", StrSTS = "", StrIC1 = "";
            byte[] ByteMSG, ByteCodiceSupportoOrig, ByteCodiceArrivo, ByteCodiceDestinazione;
            string[] tokens = new string[20];

            #endregion

            #region Prearazione tokens ricevuti
            /// Raccolta dati 
            /// I token ricevuti vengono preparati nel formato da utilizzare:
            /// trasformandoli in scringhe della lunghezza prevista
            /// in base al formato dello specifico messaggio
            /// ---------------------------------------------------------------------------------------------------------
            tokens = message.Split(',');
            StrIC = tokens[tkidx++].PadRight(5, ' ');
            StrPS = tokens[tkidx++].PadRight(5, ' ');
            StrRA = tokens[tkidx++].PadRight(5, ' ');
            StrCOD = tokens[tkidx++].PadRight(2, ' ');

            StrTID = tokens[tkidx++].PadRight(5, ' ');
            StrCodiceSupportoOrig = tokens[tkidx++].PadRight(8, ' ');
            StrCodiceSupportoChg = tokens[tkidx++].PadRight(8, '0');
            StrCodiceArrivo = tokens[tkidx++].PadRight(8, ' ');
            StrCodiceDestinazione = tokens[tkidx++].PadRight(8, ' ');
            StrSTS = tokens[tkidx++].PadLeft(5, '0');
            StrIC1 = tokens[tkidx++].PadRight(5, ' ');

            #endregion
            #region Parte comune a tutti i messaggi
            // IC (Word 0)(Byte 0,1)
            //---
            sidx = 0;
            IC = Convert.ToUInt16(StrIC);    //Converte la stringa in numero
            Number = IC;                     //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // PS (Word 1)(Byte 2,3)
            //---
            PS = Convert.ToUInt16(StrPS);    //Converte la stringa in numero
            Number = PS;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // AR (Word 2)(Byte 4,5)
            //---
            AR = Convert.ToUInt16(StrRA);    //Converte la stringa in numero
            Number = AR;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // MSG (Word 3)(Byte 6,7)-----Qui sappiamo il tipo di messaggio
            //----
            ByteMSG = StringToByteArray(StrCOD); //Codice del messaggio
            sendbuffer[sidx++] = ByteMSG[1];
            sendbuffer[sidx++] = ByteMSG[0];
            #endregion

            #region Gestione SM
            // MISS (Word 4)(Byte 8,9)
            //---
            MISS = Convert.ToUInt16(StrTID);    //Converte la stringa in numero
            Number = MISS;                       //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // Codice supporto (Word 5-8)(Byte 10,17) - Codice supporto originale
            //----
            ByteCodiceSupportoOrig = StringToByteArray(StrCodiceSupportoOrig); //Converte la stringa in Byte
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[1];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[0];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[3];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[2];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[5];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[4];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[7];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[6];

            // Codice supporto (Word 9-12)(Byte 18,25) - La mette a zero
            //----
            Number = 0;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;

            // Codice Arrivo (Word 13-16)(Byte 26,33) - Codice punto sorgente
            //----
            ByteCodiceArrivo = StringToByteArray(StrCodiceArrivo); //Converte la stringa in Byte
            sendbuffer[sidx++] = ByteCodiceArrivo[1];
            sendbuffer[sidx++] = ByteCodiceArrivo[0];
            sendbuffer[sidx++] = ByteCodiceArrivo[3];
            sendbuffer[sidx++] = ByteCodiceArrivo[2];
            sendbuffer[sidx++] = ByteCodiceArrivo[5];
            sendbuffer[sidx++] = ByteCodiceArrivo[4];
            sendbuffer[sidx++] = ByteCodiceArrivo[7];
            sendbuffer[sidx++] = ByteCodiceArrivo[6];

            // Codice Destinazione (Word 17-20)(Byte 34,41) - Codice punto destinazione
            //----
            ByteCodiceDestinazione = StringToByteArray(StrCodiceDestinazione); //Converte la stringa in Byte
            sendbuffer[sidx++] = ByteCodiceDestinazione[1];
            sendbuffer[sidx++] = ByteCodiceDestinazione[0];
            sendbuffer[sidx++] = ByteCodiceDestinazione[3];
            sendbuffer[sidx++] = ByteCodiceDestinazione[2];
            sendbuffer[sidx++] = ByteCodiceDestinazione[5];
            sendbuffer[sidx++] = ByteCodiceDestinazione[4];
            sendbuffer[sidx++] = ByteCodiceDestinazione[7];
            sendbuffer[sidx++] = ByteCodiceDestinazione[6];

            // STATO (Word 21)(Byte 42,43) - Bit di stato missione
            //---
            Number = Convert.ToUInt16(StrSTS);    //Converte la stringa in numero
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // NULL DATA (Word 22-254)(Byte 44,509)
            //---
            Number = 0;                    //Converte il numero in Byte
            for (int i = sidx; i < 510; )
            {
                sendbuffer[i++] = Number.Byte1;
                sendbuffer[i++] = Number.Byte2;
            }

            // IC1
            //---
            IC1 = Convert.ToUInt16(StrIC1);    //Converte la stringa in numero
            Number = IC1;                      //Converte il numero in Byte
            sendbuffer[510] = Number.Byte1;
            sendbuffer[511] = Number.Byte2;
            #endregion
            return (sendbuffer);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private byte[] PrepareMsgStatoIdSupporto(string message)
        {
            #region Parte dichiarativa
            int sidx; // Send buffer index
            int tkidx = 0;

            byte[] sendword = new byte[2];
            byte[] sendbuffer = new byte[512];
            byte[] ByteAgv = new byte[6];
            byte[] ByteLayoutPoint = new byte[8];
            UInt16Converter Number;

            UInt16 IC, IC1, PS, AR, MISS;
            string StrIC = "", StrPS = "", StrRA = "", StrCOD = "", StrTID = "", StrAgv = "", StrCodiceSupportoOrig = "";
            string StrCodiceArrivo = "", StrCodiceDestinazione = "", StrSTS = "", StrIC1 = "";
            //string StrAgv2 = "", StrSTS2 = "", StrAgv3 = "", StrSTS3 = "", StrAgv4 = "", StrSTS4 = "";
            byte[] ByteMSG, ByteCodiceSupportoOrig, ByteCodiceArrivo, ByteCodiceDestinazione;
            string[] tokens = new string[20];


            #endregion

            #region Prearazione tokens ricevuti
            /// Raccolta dati 
            /// I token ricevuti vengono preparati nel formato da utilizzare:
            /// trasformandoli in scringhe della lunghezza prevista
            /// in base al formato dello specifico messaggio
            /// ---------------------------------------------------------------------------------------------------------
            tokens = message.Split(',');
            StrIC = tokens[tkidx++].PadRight(5, ' ');
            StrPS = tokens[tkidx++].PadRight(5, ' ');
            StrRA = tokens[tkidx++].PadRight(5, ' ');
            StrCOD = tokens[tkidx++].PadRight(2, ' ');

            StrAgv = tokens[tkidx++];
            switch (StrAgv)
            {
                case "1":
                case "2": StrAgv = "AVCG0" + StrAgv; break;
                case "3":
                case "4": StrAgv = "AVCF0" + StrAgv; break;
                default: break;
            }
            StrAgv = StrAgv.PadRight(8, ' ');
            StrCodiceSupportoOrig = tokens[tkidx++].PadRight(8, ' ');
            StrSTS = tokens[tkidx++].PadLeft(5, '0');
            StrIC1 = tokens[tkidx++].PadRight(5, ' ');

            #endregion
            #region Parte comune a tutti i messaggi
            // IC (Word 0)(Byte 0,1)
            //---
            sidx = 0;
            IC = Convert.ToUInt16(StrIC);    //Converte la stringa in numero
            Number = IC;                     //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // PS (Word 1)(Byte 2,3)
            //---
            PS = Convert.ToUInt16(StrPS);    //Converte la stringa in numero
            Number = PS;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // AR (Word 2)(Byte 4,5)
            //---
            AR = Convert.ToUInt16(StrRA);    //Converte la stringa in numero
            Number = AR;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // MSG (Word 3)(Byte 6,7)-----Qui sappiamo il tipo di messaggio
            //----
            ByteMSG = StringToByteArray(StrCOD); //Codice del messaggio
            sendbuffer[sidx++] = ByteMSG[1];
            sendbuffer[sidx++] = ByteMSG[0];
            #endregion

            #region Gestione SM
            // MISS (Word 4)(Byte 8,9)
            //---
            MISS = Convert.ToUInt16(StrTID);    //Converte la stringa in numero
            Number = MISS;                       //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // Codice supporto (Word 5-8)(Byte 10,17) - Codice supporto originale
            //----
            ByteCodiceSupportoOrig = StringToByteArray(StrCodiceSupportoOrig); //Converte la stringa in Byte
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[1];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[0];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[3];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[2];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[5];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[4];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[7];
            sendbuffer[sidx++] = ByteCodiceSupportoOrig[6];

            // Codice supporto (Word 9-12)(Byte 18,25) - La mette a zero
            //----
            Number = 0;                    //Converte il numero in Byte
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte1;

            // Codice Arrivo (Word 13-16)(Byte 26,33) - Codice punto sorgente
            //----
            ByteCodiceArrivo = StringToByteArray(StrCodiceArrivo); //Converte la stringa in Byte
            sendbuffer[sidx++] = ByteCodiceArrivo[1];
            sendbuffer[sidx++] = ByteCodiceArrivo[0];
            sendbuffer[sidx++] = ByteCodiceArrivo[3];
            sendbuffer[sidx++] = ByteCodiceArrivo[2];
            sendbuffer[sidx++] = ByteCodiceArrivo[5];
            sendbuffer[sidx++] = ByteCodiceArrivo[4];
            sendbuffer[sidx++] = ByteCodiceArrivo[7];
            sendbuffer[sidx++] = ByteCodiceArrivo[6];

            // Codice Destinazione (Word 17-20)(Byte 34,41) - Codice punto destinazione
            //----
            ByteCodiceDestinazione = StringToByteArray(StrCodiceDestinazione); //Converte la stringa in Byte
            sendbuffer[sidx++] = ByteCodiceDestinazione[1];
            sendbuffer[sidx++] = ByteCodiceDestinazione[0];
            sendbuffer[sidx++] = ByteCodiceDestinazione[3];
            sendbuffer[sidx++] = ByteCodiceDestinazione[2];
            sendbuffer[sidx++] = ByteCodiceDestinazione[5];
            sendbuffer[sidx++] = ByteCodiceDestinazione[4];
            sendbuffer[sidx++] = ByteCodiceDestinazione[7];
            sendbuffer[sidx++] = ByteCodiceDestinazione[6];

            // STATO (Word 21)(Byte 42,43) - Bit di stato missione
            //---
            Number = Convert.ToUInt16(StrSTS);    //Converte la stringa in numero
            sendbuffer[sidx++] = Number.Byte1;
            sendbuffer[sidx++] = Number.Byte2;

            // NULL DATA (Word 22-254)(Byte 44,509)
            //---
            Number = 0;                    //Converte il numero in Byte
            for (int i = sidx; i < 510; )
            {
                sendbuffer[i++] = Number.Byte1;
                sendbuffer[i++] = Number.Byte2;
            }

            // IC1
            //---
            IC1 = Convert.ToUInt16(StrIC1);    //Converte la stringa in numero
            Number = IC1;                      //Converte il numero in Byte
            sendbuffer[510] = Number.Byte1;
            sendbuffer[511] = Number.Byte2;
            #endregion
            return (sendbuffer);
        }
        /// <summary>
        /// Inverte la posizione di MSB e LSB i tutte le word del Byte Array in input
        /// </summary>
        /// <param name="ByteArray"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] SwapByte(byte[] ByteArray, int length)
        {
            byte tmpb;
            try
            {
                for (int i = 0; i < length; )
                {
                    tmpb = ByteArray[i];
                    ByteArray[i] = ByteArray[i + 1];
                    ByteArray[i + 1] = tmpb;
                    i += 2;
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, "SwapByte", ex.Message);
                //throw;
            }
            //stringa = ByteArrayToString(ByteArray);
            return (ByteArray);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        private void HandleQueuedMsgToHost()
        {
            string[] tokens = new string[20];
            Byte[] MsgBytebuffer = new byte[512];

            tokens = GetOldestNodeMessageToSend();
            // Adesso ho recuperato il messaggio da spedire
            // In base al messaggio preparo il telegramma in forma binaria
            //poi lo spedisco
            switch (tokens[3].Split(',')[0])
            {
                case "SM":
                    { 
                        //gestisce SM -passando il corpo del messaggio
                    }
                    break;
                case "SS":
                    {
                        //gestisce SS -passando il corpo del messaggio
                    }
                    break;
                case "SI":
                    {
                        //gestisce SS -passando il corpo del messaggio
                    }
                    break;
                default:
                    break;
            }
            //Prepara il messaggio
            //MsgBytebuffer = PrepareMsgStatoMissione(sndmessage);
            MsgBytebuffer = SwapByte(MsgBytebuffer, 512);           // Ripristina il msg originale
            ExecuteSendHostMessage(MsgBytebuffer);
        }
        /// <summary>
        /// Setta lo stato della comunicazione con Host
        /// </summary>
        /// <param name="CommStatus">"UP";"DOWN"</param>
        private void SetHostCommStatus(string CommStatus)
        {
           // Tags.SetString("AgvCtl.Settings.MHMStatus", CommStatus);
        }

        #region Sezione dedicata alla gestione degli allarmmi

        /// <summary>
        /// 
        /// </summary>
        /// <param name="AgvCtlMsg">"[0]AED,[1]AgvNo,[2]Cnt,[3]Errno1,[4]Errno2, ..., [Cnt]ErrnoCtn - Agv Digitron</param>
        /// <param name="AgvCtlMsg">"[0]AEA,[1]AgvNo,[2]Cnt,[3]Errno1,[4]Errno2, ..., [Cnt]ErrnoCtn - Agv Laser</param>
        /// <param name="AgvCtlMsg">"[0]AEC,[1]AlarmText - Allarme di sistema</param>
        /// <returns>Un lista di token, ogni toke costituito da due sotto token</returns>
        public string[] UnpackAgvCtlAlarm(string AgvCtlMsg)
        {
            string[] AlarmTokens = new string[1];      // [Severity,DigAgv:Agvno,Testo Allarme][Severity,Testo Allarme] ....
            string AgvId        = "";
            string[] AgvCtlMsgTokens;
            string[] ErrorTextTokens = new string[2];
            string ErrorText    = "";
            int Cnt             = 0;
            string ErrorCode    = "";
            string Severity     = "";

            OracleDataReader reader = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            AgvCtlMsgTokens = AgvCtlMsg.Split(',');                   // split in token del messaggio: "AED,AgvNo,Cnt,Errno1,Errno2,..."          

            #region Handle "AED"
            // Messaggi da Agv Digitron
            if (AgvCtlMsgTokens[0] == "AED" && AgvCtlMsgTokens.Length > 3)  // Si tratta di un allarme da Agv Digitron
            {
                try
                {
                    Cnt = Convert.ToInt32(AgvCtlMsgTokens[2]);              // Legge il numero di allarmi da registrare
                    AlarmTokens = new string[Cnt];                          // re instanzia l'array per un numero maggiore di elementi
                    AgvId += "DigAgv " + AgvCtlMsgTokens[1];

                    for (int i = 0; i < Cnt; i++)
                    {
                        ErrorCode = AgvCtlMsgTokens[3 + i];

                        cmd.CommandText =   @"SELECT *" +
                                            @" FROM ""DigitronErrors""" +
                                            @" WHERE ""ErrorCode"" = '" + ErrorCode + "'";

                        reader = cmd.ExecuteReader();
                        ErrorText = "";
                        while (reader.Read())
                        {
                            ErrorText = reader.GetString(reader.GetOrdinal("Display"));
                            break;
                        }
                        reader.Close();

                        if (ErrorText == "")
                        {
                            ErrorText = ErrorCode + " Non Definito";
                        }
                        else
                        {
                            ErrorTextTokens = ErrorText.Split('-');
                            ErrorText = ErrorTextTokens[1];
                        }
                        AlarmTokens[i] = ErrorTextTokens[0] + ':' + AgvId + ", " + ErrorText.Trim();
                    }
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    reader.Close();
                }
                finally
                {
                    if (!reader.IsClosed) reader.Close();
                }
            }
            #endregion
            #region Handle "AEA"
            //
            else if (AgvCtlMsgTokens[0] == "AEA" && AgvCtlMsgTokens.Length > 3)  // Si tratta di un allarme da Agv Digitron
            {
                try
                {
                    Cnt = Convert.ToInt32(AgvCtlMsgTokens[2]);              // Legge il numero di allarmi da registrare
                    AlarmTokens = new string[Cnt];                          // re instanzia l'array per un numero maggiore di elementi
                    AgvId += "Lgv " + AgvCtlMsgTokens[1];

                    for (int i = 0; i < Cnt; i++)
                    {
                        ErrorCode = AgvCtlMsgTokens[3 + i];

                        cmd.CommandText =   @"SELECT *" +
                                            @" FROM ""AgvStatusBitDefinitions""" +
                                            @" WHERE ""Idx"" = '" + ErrorCode + "'";;

                        reader = cmd.ExecuteReader();
                        ErrorText = "";
                        while (reader.Read())
                        {
                            ErrorText = reader.GetString(reader.GetOrdinal("AltDisplay"));
                            Severity = reader.GetString(reader.GetOrdinal("Severity"));
                            break;
                        }
                        reader.Close();

                        if (ErrorText == "")
                        {
                            ErrorText = ErrorCode + " Non Definito";
                        }
                        AlarmTokens[i] = Severity + ':' + AgvId + ", " + ErrorText.Trim();
                    }
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    reader.Close();
                }
                finally
                {
                    if (!reader.IsClosed) reader.Close();
                }
            }
            #endregion
            #region Handle AEC
            //"AEC,ErrNo"
            //-----------
            else if (AgvCtlMsgTokens[0] == "AEC" && AgvCtlMsgTokens.Length > 3)  // Si tratta di un allarme AgvCtl
            {
                AlarmTokens[0] = AgvCtlMsgTokens[1];
            }
            #endregion
            #region Handle Messaggio generico
            //"Messaggio generico"
            //--------------------
            else
            {
                AlarmTokens[0] = AgvCtlMsgTokens[1];
            }
            #endregion
            return (AlarmTokens);
        }

        /// <summary>
        /// <pare>Spacchetta il messaggio d'errore di un AGV Digitron</pare>
        /// <pare>restituendo una stringa</pare>
        /// <pare>e sostituendo i codici di errore con le relative stringhe di testo</pare>
        /// </summary>
        /// <param name="DAgvMessage">"AE,AGVNO,CNT,ERRCod1,ERRCod2,..." dove Cnt è il numero degli errori</param>
        /// <returns>[Agv],[Error1],[Error2],...</returns>
        public string UnpackDigAgvAlarm(string DAgvMessage)
        {
            string DAgvMTokens  = "";
            string[] tmpTokens;
            string ErrorText    = "";
            int Cnt = 0;
            string ErrorCode    = "";
            string Severity     = "";

            OracleDataReader reader = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            tmpTokens = DAgvMessage.Split(',');
            // "AED,AgvNo,Cnt,Errno1,Errno2,..."
            //----------------------------------
            if (tmpTokens[0] == "AED" && tmpTokens.Length > 3)  // Si tratta di un allarme da Agv Digitron
            {
                try
                {
                    Cnt = Convert.ToInt32(tmpTokens[2]);
                    DAgvMTokens += "DigAgv: ";
                    DAgvMTokens += tmpTokens[1] + " (";

                    for (int i = 0; i < Cnt; i++)
                    {
                        ErrorCode = tmpTokens[3 + i];

                        cmd.CommandText =   @"SELECT *" +
                                            @" FROM ""DigitronErrors""" +
                                            @" WHERE ""ErrorCode"" = '" + ErrorCode + "'";

                        reader = cmd.ExecuteReader();
                        ErrorText = "";
                        while (reader.Read())
                        {
                            ErrorText = reader.GetString(reader.GetOrdinal("Display"));
                        }
                        reader.Close();
                        if (ErrorText == "") ErrorText = ErrorCode + " Non Definito";
                        if (i > 0) DAgvMTokens += "; ";              //Non è il primo Errore
                        DAgvMTokens += ErrorText;
                    }
                    DAgvMTokens += ")";
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    reader.Close();
                }
                finally
                {
                    if (!reader.IsClosed) reader.Close();
                }
            }
            //"AEA,AgvNo,ErrNo"
            //-----------------
            else if (tmpTokens[0] == "AEA" && tmpTokens.Length == 3)  // Si tratta di un allarme da Agv Laser
            {
                try
                {
                    DAgvMTokens += "LaserAgv: ";
                    DAgvMTokens += tmpTokens[1] + " (";

                    ErrorCode = tmpTokens[2];

                    cmd.CommandText =   @"SELECT *" +
                                        @" FROM ""AgvStatusBitDefinitions""" +
                                        @" WHERE ""Idx"" = '" + ErrorCode + "'";

                    reader = cmd.ExecuteReader();
                    ErrorText = "";
                    while (reader.Read())
                    {
                        ErrorText = reader.GetString(reader.GetOrdinal("AltDisplay"));
                        Severity = reader.GetString(reader.GetOrdinal("Severity"));
                    }
                    reader.Close();
                    if (ErrorText == "")
                    {
                        ErrorText = ErrorCode + " Non Definito";
                    }
                    else
                    {
                        ErrorText = Severity + " - " + ErrorText;
                    }

                    DAgvMTokens += ErrorText;
                    DAgvMTokens += ")";
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    reader.Close();
                }
                finally
                {
                    if (!reader.IsClosed) reader.Close();
                }
            }
            //"AEC,ErrNo"
            //-----------
            else if (tmpTokens[0] == "AEC" && tmpTokens.Length > 3)  // Si tratta di un allarme AgvCtl
            {
                DAgvMTokens = DAgvMessage;
            }
            //"Messaggio generico"
            //--------------------
            else
            {
                DAgvMTokens = DAgvMessage;
            }
            return (DAgvMTokens);
        }
        public string UnpackLaserAgvAlarm(string DAgvMessage)
        {
            string DAgvMTokens = "";
            string[] tmpTokens;
            string ErrorText = "";
            int Cnt = 0;
            string ErrorCode = "";
            string Severity = "";

            OracleDataReader reader = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            tmpTokens = DAgvMessage.Split(',');
            // "AED,AgvNo,Cnt,Errno1,Errno2,..."
            //----------------------------------
            if (tmpTokens[0] == "AED" && tmpTokens.Length > 3)  // Si tratta di un allarme da Agv Digitron
            {
                try
                {
                    Cnt = Convert.ToInt32(tmpTokens[2]);
                    DAgvMTokens += "DigAgv: ";
                    DAgvMTokens += tmpTokens[1] + " (";

                    for (int i = 0; i < Cnt; i++)
                    {
                        ErrorCode = tmpTokens[3 + i];

                        cmd.CommandText =   @"SELECT *" +
                                            @" FROM ""DigitronErrors""" +
                                            @" WHERE ""ErrorCode"" = '" + ErrorCode + "'";

                        reader = cmd.ExecuteReader();
                        ErrorText = "";
                        while (reader.Read())
                        {
                            ErrorText = reader.GetString(reader.GetOrdinal("Display"));
                        }
                        reader.Close();
                        if (ErrorText == "") ErrorText = ErrorCode + " Non Definito";
                        if (i > 0) DAgvMTokens += "; ";              //Non è il primo Errore
                        DAgvMTokens += ErrorText;
                    }
                    DAgvMTokens += ")";
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    reader.Close();
                }
                finally
                {
                    if (!reader.IsClosed) reader.Close();
                }
            }
            //"AEA,AgvNo,ErrNo"
            //-----------------
            else if (tmpTokens[0] == "AEA" && tmpTokens.Length == 3)  // Si tratta di un allarme da Agv Laser
            {
                try
                {
                    DAgvMTokens += "LaserAgv: ";
                    DAgvMTokens += tmpTokens[1] + " (";

                    ErrorCode = tmpTokens[2];

                    cmd.CommandText =   @"SELECT *" +
                                        @" FROM ""AgvStatusBitDefinitions""" +
                                        @" WHERE ""Idx"" = '" + ErrorCode + "'";

                    reader = cmd.ExecuteReader();
                    ErrorText = "";
                    while (reader.Read())
                    {
                        ErrorText = reader.GetString(reader.GetOrdinal("AltDisplay"));
                        Severity = reader.GetString(reader.GetOrdinal("Severity"));
                    }
                    reader.Close();
                    if (ErrorText == "")
                    {
                        ErrorText = ErrorCode + " Non Definito";
                    }
                    else
                    {
                        ErrorText = Severity + " - " + ErrorText;
                    }

                    DAgvMTokens += ErrorText;
                    DAgvMTokens += ")";
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    reader.Close();
                }
                finally
                {
                    if (!reader.IsClosed) reader.Close();
                }
            }
            //"AEC,ErrNo"
            //-----------
            else if (tmpTokens[0] == "AEC" && tmpTokens.Length > 3)  // Si tratta di un allarme AgvCtl
            {
                DAgvMTokens = DAgvMessage;
            }
            //"Messaggio generico"
            //--------------------
            else
            {
                DAgvMTokens = DAgvMessage;
            }
            return (DAgvMTokens);
        }

        /// <summary>
        /// <pare>Spacchetta il messaggio AgvUi</pare>
        /// <pare>restituendo una stringa</pare>
        /// <pare>e sostituendo i codici di errore con le relative stringhe di testo</pare>
        /// </summary>
        /// <param name="DAgvMessage">"AE,AGVNO,CNT,ERRCod1,ERRCod2,..." dove Cnt è il numero degli errori</param>
        /// <returns>[Agv],[Error1],[Error2],...</returns>
        public string UnpackAgvUiAlarm(string AgvUiMessage)
        {
            string[] Tokens;
            Tokens = AgvUiMessage.Split(',');

            if (Tokens.Length >= 3)
            {
                AgvUiMessage = "User: (" + Tokens[0] + ":" + Tokens[1] + ")->" + Tokens[2];
            }
            //User(Andreetto)->Login
            return (AgvUiMessage);
        }
        public string UnpackHostBridgeAlarm(string HbrgMessage)
        {
            return (HbrgMessage);
        }
        /// <summary>
        /// restituisce il prossimo numero si sequenza per l'allarme da archiviare in Alarms
        /// </summary>
        /// <returns></returns>
        private int GetNextAlarmSeq()
        {
            int rows;
            int progressive = 0;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                // Recupera il valore corrente di Ic e lo aggiorna o incrementadolo o resettandolo a 1
                //------------------------------------------------------------------------------------
                cmd.CommandText =   @"SELECT *" +
                                    @" FROM ""HostControlBoard""" +
                                    @" WHERE ""ItemName"" = 'AlarmSeq'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    progressive = reader.GetInt32(reader.GetOrdinal("IntVal"));
                    break;
                }
                reader.Close();


                if (++progressive > MaxAlarms) progressive = 1;

                // Aggiorna la tabella di controllo con il nuovo valore Ic
                //--------------------------------------------------------
                cmd.CommandText =   @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + progressive + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'AlarmSeq'";
                rows = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            return progressive;
        }
        private void SendAlarm(string Source, string Type, string AlarmText)
        {
            System.Messaging.Message msg;
            string message = (Source + ":" + Type + ":" + AlarmText).Trim();

            msg = new System.Messaging.Message();
            lock (logsendq)
            {
                logsendq.Send(msg, (message).Trim());
            }
        }

        #endregion
    }
}
