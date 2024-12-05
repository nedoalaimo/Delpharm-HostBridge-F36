//********************************************************************
// Filename: HostBridgeQueries.cs 
//___________________________________________________________________ 
// Application  : Host interface handling
// Main program : HostBridge.cs
// Version      : 1.0.0
// Status       : c         [[c]oding, [t]est, [e]rror, ok]
// Author       : Roberto Alaimo - Bit Automation
// Date         : 01.05.16
// ___________________________________________________________________
// Description  : Queries handling 
//                 
//_____________________________________________________________________
// Changes      :
// Date     Author      Description
// 171220   RAL         Changed for F36
//********************************************************************

using System;
using System.Data;
using MhcsLib;
using Oracle.DataAccess.Client;

namespace HostBridge
{
    partial class HostBridge : MhcsLib.SvcApp
    {
        /// <summary>
        /// Legge la tabella di controllo di Host
        /// </summary>
        /// <returns></returns>
        private void GetHostControlBoard()
        {
            //int timerinterval = 0;
            //int rows;
            OracleDataReader reader = null;
            Oracle.DataAccess.Client.OracleCommand cmd = null;
            System.DateTime TlgTime = System.DateTime.Now;
            try
            {
                this.connection = Connection;
                cmd = new Oracle.DataAccess.Client.OracleCommand();

                cmd.Connection = connection;
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted); 

                // Legge il valore
                //----------------------------------------------------------
                cmd.CommandText =   @"SELECT *" +
                                    @" FROM ""HostControlBoard""";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    switch (reader.GetString(reader.GetOrdinal("ItemName")))
                    {
                        case "HostAckPending":
                            FromHostAckPending = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        case "NumRetryDelay":
                            numretrydelay = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        case "NumShortDelay":
                            numshortdelay = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        case "IsSent":
                            isSent = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        case "PsSent":
                            psSent = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        case "HostTimerInterval":
                            hosttimerinterval = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        //case "RetryTimerInterval":
                        //    retrytimerinterval = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                        //    break;
                        case "ShortTimerInterval":
                            shorttimerinterval = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        case "LongTimerInterval":
                            longtimerinterval = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        default:
                            break;
                    }
                }
                reader.Close();
                //ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
            }
            catch (HostException ex)
            {
                //ZoneTrans.Rollback();
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, ex.Message);
            }
            catch (Exception ex)
            {
                // error on GetHostControlBoard
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }
        }
        /// <summary>
        /// Get the date time "yyyyMMddHHmmss"
        /// </summary>
        /// <returns>string</returns>


        /// <summary>
        /// Verifica se HostBridge è in attesa di un ack da Host
        /// </summary>
        /// <returns>1= in attesa; 0 altrimenti</returns>
        private int GetHostAckPending(OracleConnection Connection)
        {
            int value = 0;
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
                                    @" WHERE ""ItemName"" = 'HostAckPending'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    value = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                    break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                // Error on GetHostAckPending
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }
            return value;
        }
        /// <summary>
        /// Restituisce il nnumero dell'AGV corrispondente al nome Host
        /// </summary>
        /// <param name="AgvName"></param>
        /// <returns>0=nessun AGV trovato con quel nome</returns>
        private int GetNodeAgvNo(string AgvName)
        {
            int AgvNo = 0;
            OracleDataReader reader = null;
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
                                    @" FROM ""HostAgvNumber""" +
                                    @" WHERE ""DeviceID"" = '" + AgvName.Trim() + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AgvNo = (int)reader.GetDecimal(reader.GetOrdinal("AgvNo"));
                    break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }

            return (AgvNo);
        }
        /// <summary>
        /// Dato il numero Node dell'AGV, restituisce il cosirpondente nome Host
        /// </summary>
        /// <param name="AgvNo"></param>
        /// <returns></returns>
        private string GetHostAgvName(int AgvNo)
        {
            string AgvName = "";
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
                                    @" FROM ""HostAgvNumber""" +
                                    @" WHERE ""AgvNo"" = '" + AgvNo + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AgvName = reader.GetString(reader.GetOrdinal("DeviceID"));
                    break;
                }
                reader.Close();

            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }
            return (AgvName);
        }
        /// <summary>
        /// Ritorna o stato del sistena: Run or Shutdown
        /// </summary>
        /// <returns></returns>
        public string GetSystemStatus()
        {
            return (Tags.GetString("AgvCtl" + ".Settings.SystemStatus"));    //Run or Shutdown
        }
        /// <summary>
        /// ritorna il il messaggio di ack più vecchio, in token stringa
        /// </summary>
        /// <returns></returns>
        public string[] GetNodeAckToSend()
        {
            OracleDataReader reader = null;
            string[] tokens = new string[5];
            int ic = 0, pa = 0, ra = 0, di = 0, ic1 = 0;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""SendAckToHost""" +
                                    @" ORDER BY ""IcStart"" " + "ASC";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ic = GetNextNodeAckIc();
                    //ic = (int)reader.GetDecimal(reader.GetOrdinal("IcStart"));
                    pa = (int)reader.GetDecimal(reader.GetOrdinal("Ps"));
                    ra = (int)reader.GetDecimal(reader.GetOrdinal("Ra"));
                    di = (int)reader.GetDecimal(reader.GetOrdinal("Di"));
                    //ic1 = (int)reader.GetDecimal(reader.GetOrdinal("IcStop"));
                    ic1 = ic;
                    break;
                }
                reader.Close();
                if (ic != 0)
                {
                    tokens[0] = System.Convert.ToString(ic);
                    tokens[1] = System.Convert.ToString(pa);
                    tokens[2] = System.Convert.ToString(ra);
                    tokens[3] = System.Convert.ToString(di);
                    tokens[4] = System.Convert.ToString(ic1);
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }
            return (tokens);
        }
        /// <summary>
        ///<para>Recupera il telegramma di Ack con il PS inditaco o più vecchio</para>
        ///<para>Rotorna:"[Ic][Pa][Ra][Di][Ic]"</para>
        /// </summary>
        /// <returns>il messaggio sotto forma di Lista di token (int)</returns>
        public int[] GetNodeAckToSend(int Ps)
        {
            OracleDataReader reader = null;
            int[] tokens = new int[5];
            int ic = 0, pa = 0, ra = 0, di = 0, ic1 = 0;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                if (Ps != 0)
                {
                    cmd.CommandText = @"SELECT *" +
                                @" FROM ""SendAckToHost""" +
                                @" WHERE ""Ps"" = '" + Ps + "'";
                    reader = cmd.ExecuteReader();
                }
                else
                {
                    cmd.CommandText = @"SELECT *" +
                                @" FROM ""SendAckToHost""" +
                                @" ORDER BY ""CreateTime"" " + "ASC";
                    reader = cmd.ExecuteReader();
                }
                while (reader.Read())
                {
                    ic = GetNextNodeAckIc();
                    //ic  = (int)reader.GetDecimal(reader.GetOrdinal("IcStart"));
                    pa = (int)reader.GetDecimal(reader.GetOrdinal("Ps"));
                    ra = (int)reader.GetDecimal(reader.GetOrdinal("Ra"));
                    di = (int)reader.GetDecimal(reader.GetOrdinal("Di"));
                    //ic1 = (int)reader.GetDecimal(reader.GetOrdinal("IcStop"));
                    ic1 = ic;
                    break;
                }
                reader.Close();
                if (ic != 0) //Non ha trovato ack in coda da inviare
                {
                    tokens[0] = ic;
                    tokens[1] = pa;
                    tokens[2] = ra;
                    tokens[3] = di;
                    tokens[4] = ic1;
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }
            return (tokens);
        }
        /// <summary>
        /// <para>Ritrova il messaggio di Host, precedentemente salvato e ancora da processare</para>
        /// <para>Ritorna: "[Ic][Ps][Ra][Cod][Trn][Udc][Src][Dst][Pri][IC]"</para>
        /// </summary>
        /// <returns></returns>
        public string[] GetOldestHostMessageReceived()
        {
            OracleDataReader reader = null;
            string[] tokens = new string[20];
            string[] MsgTokens = new string[10];
            string Message = "";
            int tokidx = 0;

            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""RecMsgFromHost""" +
                                    @" ORDER BY ""CreateTime"" " + "ASC";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("IcStart")));
                    tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("Ps")));
                    tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("Ra")));
                    Message = reader.GetString(reader.GetOrdinal("Message"));
                    if (Message.Length > 2)
                    {
                        MsgTokens = Message.Split(',');
                        for (int i = 0; i < MsgTokens.Length; i++)
                        {
                            tokens[tokidx++] = MsgTokens[i];
                        }
                    }
                    else if (Message.Length == 2)
                    {
                        tokens[tokidx++] = Message;
                    }
                    tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("IcStop")));
                    break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }
            return (tokens);
        }
        public string[] GetOldestNodeMessageToSend()
        {
            string[] tokens = new string[15];
            string[] MsgTokens = new string[10];
            int tokidx = 0;
            int Ps = 0;
            bool FirstTry = false;
            int Seq = 0;

            OracleDataReader reader = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = Convert.ToDateTime(null);
            string strigdate = "";
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;
            int rows;

            try
            {
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""SendMsgToHost""" +
                                    @" ORDER BY ""Seq"" " + "ASC";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Seq = (int)reader.GetDecimal(reader.GetOrdinal("Seq"));
                    TlgTime = reader.GetDateTime(reader.GetOrdinal("CreateTime"));                   
                    strigdate = TlgTime.ToString("MM/dd/yyyy hh:mm:ss tt");
                    tokens[tokidx++] = System.Convert.ToString(GetNextNodeIc());
                    //tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("IcStart")));
                    tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("Ps")));
                    if (tokens[tokidx - 1] == "0")
                    {
                        FirstTry = true;
                        Ps = GetNextNodePs();
                        tokens[tokidx - 1] = System.Convert.ToString(Ps);
                    }
                    tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("Ra")));
                    tokens[tokidx] = reader.GetString(reader.GetOrdinal("Message"));
                    if (tokens[3].Contains(","))
                    {
                        MsgTokens = tokens[3].Split(',');
                        //if (MsgTokens[0] == "RM")
                        //{
                        for (int i = 0; i < MsgTokens.Length; i++)
                        {
                            tokens[tokidx++] = MsgTokens[i];
                        }
                        //}
                    }
                    //tokens[tokidx++] = System.Convert.ToString((int)reader.GetDecimal(reader.GetOrdinal("IcStop")));
                    tokens[tokidx++] = tokens[0];
                    break;
                }
                reader.Close();

                if (FirstTry)
                {
                    //System.DateTime myDate = System.DateTime.ParseExact(strigdate, "MM/dd/yyy hh:mm:ss",
                    //                   System.Globalization.CultureInfo.InvariantCulture);
                    cmd.CommandText = @"UPDATE ""SendMsgToHost"" " +
                      @"SET " +
                      @"""Ps"" ='" + Ps + "'" +
                      @" WHERE ""Seq"" = '" + Seq + "'";
                    //@" WHERE ""CreateTime""='" + TlgTime + "'";


                    rows = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }
            return (tokens);
        }
        /// <summary>
        /// Scompatta in tokens il messaggio di ack in Byte Array inviato da Host
        /// </summary>
        /// <param name="AckBytebuffer"></param>
        /// <returns></returns>
        public string GetHostAck(byte[] AckBytebuffer)
        {
            #region Parte dichiarativa
            int sidx;                           // Send buffer index
            string Asciimessage = "";
            byte[] sendword = new byte[2];
            UInt16Converter Number;

            UInt16 IC, IC1, PS, AR, DI;
            byte[] ByteMSG, ByteCodiceSupporto, ByteCodiceSupportoOrig, ByteCodiceSupportoChg, ByteCodiceArrivo, ByteCodiceDestinazione;

            ByteMSG = new byte[2];
            ByteCodiceSupporto = new byte[8];
            ByteCodiceSupportoOrig = new byte[8];
            ByteCodiceSupportoChg = new byte[8];
            ByteCodiceArrivo = new byte[8];
            ByteCodiceDestinazione = new byte[8];

            Number = 0;
            #endregion


            sidx = 0;

            // IC (Word 0)(Byte 0,1)
            //---
            Number.Byte1 = AckBytebuffer[sidx++]; Number.Byte2 = AckBytebuffer[sidx++];
            //StrIC = Convert.ToString(Number);
            IC = Number;

            // PS (Word 1)(Byte 2,3)
            //---
            Number.Byte1 = AckBytebuffer[sidx++]; Number.Byte2 = AckBytebuffer[sidx++];
            //StrPS = Convert.ToString(Number);
            PS = Number;

            // AR (Word 2)(Byte 4,5)
            //---
            Number.Byte1 = AckBytebuffer[sidx++]; Number.Byte2 = AckBytebuffer[sidx++];
            //StrAR = Convert.ToString(Number);
            AR = Number;

            // DI
            //---
            Number.Byte1 = AckBytebuffer[sidx++]; Number.Byte2 = AckBytebuffer[sidx++];
            //StrIC1 = Convert.ToString(Number);
            DI = Number;

            // IC1
            //---
            Number.Byte1 = AckBytebuffer[sidx++]; Number.Byte2 = AckBytebuffer[sidx++];
            //StrIC1 = Convert.ToString(Number);
            IC1 = Number;

            Asciimessage = IC + "," + PS + "," + AR + "," + DI + "," + IC1;
            MessageWriter.Log(MhcsLib.DebugZones.Events, HOSTBRIDGE_LOG, "ACK RECEIVED: ( " + Asciimessage + " )");
            return (Asciimessage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Tid"></param>
        /// <param name="Udc"></param>
        /// <param name="Dst"></param>
        /// <test>Done</test>
        private void GetTransportData(long Tid, ref string Udc, ref string Dst)
        {
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
                                    @" FROM ""TrOrderMaster""" +
                                    @" WHERE ""TrOrderNr"" = '" + Tid + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("UDC"))) Udc = reader.GetString(reader.GetOrdinal("UDC"));
                    if (!reader.IsDBNull(reader.GetOrdinal("DestName"))) Dst = reader.GetString(reader.GetOrdinal("DestName"));
                    break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }
        }
        private string GetAgvLids() //171220
        {
            int AgvNo = 0;
            string Udc ="";
            string AgvLidList = "";
            string[] AgvLidTkn = {"01:","02:","03:","04:"};
            OracleDataReader reader = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;
            cmd.CommandText = @"SELECT *" +
                    @" FROM ""TrOrderMaster""" +
                    @" WHERE ""TrOrderPhase"" = '" + 1 + "'";
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                AgvNo = (int)reader.GetDecimal(reader.GetOrdinal("AgvNr"));
                Udc = reader.GetString(reader.GetOrdinal("UDC"));
                switch (AgvNo)
                {
                    case 1:
                        {
                            AgvLidTkn[0] += Udc;
                        }
                        break;
                    case 2:
                        {
                            AgvLidTkn[1] += Udc;
                        }
                        break;
                    case 3:
                        {
                            AgvLidTkn[2] += Udc;
                        }
                        break;
                    case 4:
                        {
                            AgvLidTkn[3] += Udc;
                        }
                        break;
                    default:
                        break;
                }
            }

            reader.Close();
            AgvLidList = AgvLidTkn[0] + "," + AgvLidTkn[1] + "," + AgvLidTkn[2] + "," + AgvLidTkn[3];
            return (AgvLidList);
        }
#if false
    public void GetNextMessageToHost()
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
            //HostTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

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
                //HostBridge si mette in attesa di Ack da parte di Host
                //Salva l'Ic del telegramma inviato
                //------------------------------------------
                cmd.CommandText = @"UPDATE ""HostControlBoard"" " +
                                  @"SET " +
                                  @"""IntVal"" ='" + icstart + "'" +
                                  @" WHERE ""ItemName""='IcSent'";

                rows = cmd.ExecuteNonQuery();

                FromHostAckPending = 1;                                 // In attesa di ack da Host
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
            //HostTrans.Commit();
        }
        catch (OracleException e)
        {
            MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
            //HostTrans.Rollback();
        }
        catch (HostException ex)
        {
            MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, ex.Message);
            //HostTrans.Rollback();
        }
        catch (Exception ex)
        {
            //HostTrans.Rollback();
        }
        finally
        {
            if (!reader.IsClosed) reader.Close();
        }
    } 
#endif
        private string GetOrderFromMaster(int orderid)
        {
            string TroRecord = "";
            System.DateTime TmpCreateTime, TmpStartTime, TmpPickTime, TmpDropTime, TmpEndTime;

            //int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            //OracleTransaction ZoneTrans = null;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();


            TmpCreateTime = TmpStartTime = TmpPickTime = TmpDropTime = TmpEndTime = Convert.ToDateTime(null);

            try
            {
                cmd.Connection = connection;
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted); // Sul try più esterno per garantire consistenza

                // Get the transport order data and copy it in History table
                //----------------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                                @" FROM ""TrOrderMaster""" +
                                @" WHERE ""TrOrderNr"" ='" + orderid + "'";
                try
                {
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        TroRecord += (int)reader.GetDecimal(reader.GetOrdinal("TrOrderNr")) + ",";
                        TroRecord += (int)reader.GetDecimal(reader.GetOrdinal("AgvType")) + ",";
                        TroRecord += (int)reader.GetDecimal(reader.GetOrdinal("AgvNr")) + ",";
                        TroRecord += reader.GetString(reader.GetOrdinal("UDC")) + ",";
                        TroRecord += reader.GetString(reader.GetOrdinal("SourceName")) + ",";
                        TroRecord += reader.GetString(reader.GetOrdinal("DestName")) + ",";
                        TroRecord += (int)reader.GetDecimal(reader.GetOrdinal("TrOrderType")) + ",";
                        TroRecord += (int)reader.GetDecimal(reader.GetOrdinal("Urgent")) + ",";
                        TroRecord += (int)reader.GetDecimal(reader.GetOrdinal("TrOrderState")) + ",";
                        TroRecord += (int)reader.GetDecimal(reader.GetOrdinal("TrOrderPhase")) + ",";
                        if (!reader.IsDBNull(reader.GetOrdinal("CreateTime")))
                            TroRecord += reader.GetDateTime(reader.GetOrdinal("CreateTime")) + ",";
                        else
                            TroRecord += ",";
                        if (!reader.IsDBNull(reader.GetOrdinal("StartTime")))
                            TroRecord += reader.GetDateTime(reader.GetOrdinal("StartTime")) + ",";
                        else
                            TroRecord += ",";
                        if (!reader.IsDBNull(reader.GetOrdinal("PickTime")))
                            TroRecord += reader.GetDateTime(reader.GetOrdinal("PickTime")) + ",";
                        else
                            TroRecord += ",";
                        if (!reader.IsDBNull(reader.GetOrdinal("DropTime")))
                            TroRecord += reader.GetDateTime(reader.GetOrdinal("DropTime")) + ",";
                        else
                            TroRecord += ",";
                        if (!reader.IsDBNull(reader.GetOrdinal("EndTime")))
                            TroRecord += reader.GetDateTime(reader.GetOrdinal("EndTime")) + ",";
                        else
                            TroRecord += ",";
                        if (!reader.IsDBNull(reader.GetOrdinal("Error")))
                            TroRecord += reader.GetString(reader.GetOrdinal("Error"));
                        else
                            TroRecord += ",";

                        break;
                    }
                    reader.Close();
                }
                catch (OracleException e)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                    //ZoneTrans.Rollback();
                    if (!reader.IsClosed) reader.Close();
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    //ZoneTrans.Rollback();
                    if (!reader.IsClosed) reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }

            return (TroRecord);
        }

        private Int32 GetTransportPhase(long Tid)
        {
            int Phase = 0;
            string message = "";
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
                                    @" FROM ""TrOrderMaster""" +
                                    @" WHERE ""TrOrderNr"" = '" + Tid + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("TrOrderPhase"))) Phase = (int)reader.GetDecimal(reader.GetOrdinal("TrOrderPhase"));
                    break;
                }
                reader.Close();
                if (message != "" && message.Split(',')[0] == "MS") Tid = System.Convert.ToInt32(message.Split(',')[1]);
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
            }
            return (Phase);
        }
        /// <summary>
        /// Restituisce il valore del flag di attesa ack da Host
        /// </summary>
        /// <returns></returns>
        /// <test>Done</test>
        private int GetHostAckPending()
        {
            Oracle.DataAccess.Client.OracleCommand cmd = null;
            System.DateTime TlgTime = System.DateTime.Now;
            try
            {
                this.connection = Connection;
                cmd = new Oracle.DataAccess.Client.OracleCommand();

                cmd.Connection = connection;
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // Legge il valore
                //----------------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                    @" FROM ""HostControlBoard""";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    switch (reader.GetString(reader.GetOrdinal("ItemName")))
                    {
                        case "HostAckPending":
                            FromHostAckPending = (int)reader.GetDecimal(reader.GetOrdinal("IntVal"));
                            break;
                        default:
                            break;
                    }
                }
                reader.Close();
                //ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                if (!reader.IsClosed) reader.Close();
                //ZoneTrans.Rollback();
            }
            catch (HostException ex)
            {
                //ZoneTrans.Rollback();
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
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
            return (FromHostAckPending);
        }


        /// <summary>
        /// Verifica se il telegramma è già presente nella tabella dei messaggi ricevuti
        /// </summary>
        /// <param name="Ps"></param>
        /// <returns></returns>
        private bool IsDuplicatedMessage(int Ic, int Ps)
        {
            bool TgmDuplicated = false;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                // Verifica se in coda vi è già un messaggio con lo stesso Ps
                //-----------------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""RecMsgFromHost""" +
                                    @" WHERE ""Ps"" = '" + Ps + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TgmDuplicated = true;       //Il messaggio è già presente in tabella
                    break;
                }
                reader.Close();

                if (!TgmDuplicated)
                {
                    // Verifica se l'Ic del telegramma appena ricevuto è uguale a quello del telegramma precedente
                    //---------------------------------------------------------------------------------------------
                    cmd.CommandText = @"SELECT *" +
                                        @" FROM ""HostControlBoard""" +
                                        @" WHERE ""ItemName"" = 'IcReceived'";
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if ((int)reader.GetDecimal(reader.GetOrdinal("IntVal")) == Ic)
                        {
                            TgmDuplicated = true;       //Il messaggio è già presente in tabella
                            break;
                        }
                    }
                    reader.Close();

                    if (!TgmDuplicated)
                    {
                        // Verifica se l'Ic del telegramma appena ricevuto è uguale a quello del telegramma precedente
                        //---------------------------------------------------------------------------------------------
                        cmd.CommandText = @"SELECT *" +
                                            @" FROM ""HostControlBoard""" +
                                            @" WHERE ""ItemName"" = 'PsReceived'";
                        reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if ((int)reader.GetDecimal(reader.GetOrdinal("IntVal")) == Ps)
                            {
                                TgmDuplicated = true;       //Il messaggio è già presente in tabella
                                break;
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            finally
            {
                if (!reader.IsClosed)
                {
                    reader.Close();
                }
            }
            return (TgmDuplicated);
        }
        /// <summary>
        /// Verifica 
        /// </summary>
        /// <param name="MsgCode"></param>
        /// <returns>true se il codice è valido</returns>
        private bool IsValidMsgCode(string MsgCode)
        {
            bool result = false;
            switch (MsgCode)
            {
                case "RI":
                case "MM":
                case "RS":
                case "RM":
                    result = true;
                    break;
                default:
                    break;
            }
            return (result);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IsHostAckQueued()
        {
            bool result = false;
            return (result);
        }
        /// <summary>
        /// Verifica se se informazioni contenute nel messaggio sono corrette
        /// </summary>
        /// <param name="Tokens"></param>
        /// <returns></returns>
        private int IsValidMessageRecord(string[] Tokens)
        {
            int PickFloor = 0;
            int ErrorVal = BFULL;       //Per default i dati sono errati (Errore generico)

            switch (Tokens[3])
            {
                case "RM":              //[IC],[PS],[RA],["RM"],[NMiss],[UDC],[Source],[Dest],[Urgenza],[IC1]
                    {
                        if (GetNodeAgvNo(Tokens[5]) == 0)       //La sorgente non è un AGV
                        {
                            if ((PickFloor = IsValidStation(Tokens[5], "Pick")) != 0 &&
                                PickFloor == IsValidStation(Tokens[6], "Drop"))
                            {
                                ErrorVal = VDATA;
                            }
                            else
                            {
                                ErrorVal = IDATA;
                            }
                        }
                        else if (IsValidStation(Tokens[6], "Drop") != 0)                                   //La sorgente è un AGV
                        {
                            ErrorVal = VDATA;
                        }
                    }
                    break;
                case "RS":
                case "MM":
                case "RI":
                    ErrorVal = VDATA;
                    break;
                default:
                    ErrorVal = IDATA;
                    break;
            }
            return (ErrorVal);
        }
        /// <summary>
        /// Verifica la validità del messaggio ricevuto da Host
        /// </summary>
        /// <param name="Tokens"></param>
        /// <param name="TokenNum"></param>
        /// <returns>Restituisce il valore DI da inviare con 'Ack di risposta</returns>
        private int IsHostMessageValid(string[] Tokens, int TokenNum)
        {
            int ErrorVal = VDATA;   //Nessun errore per default
            // Se Node non è in Run archivia comunque i messaggi ed invia l'Ack.

            //if (GetSystemStatus() != "Run")
            //{
            //    ErrorVal = NORUN;                           //Il sistema non è in funione in automatico
            //}
            if (Tokens[0] != Tokens[TokenNum - 1] && ErrorVal == VDATA)
            {
                ErrorVal = IDATA;                           //Fine telegramma non trovata
            }
            //else if (IsDuplicatedMessage(System.Convert.ToInt32(Tokens[0]), System.Convert.ToInt32(Tokens[1])) && ErrorVal == VDATA)
            else if (!IsValidHostIc(System.Convert.ToInt32(Tokens[0])) && ErrorVal == VDATA)
            {
                ErrorVal = IDATA;                            //Il telegramma è già presente nella tabella dei telegrammi ricevuti
            }
            else if (!IsValidMsgCode(Tokens[3]) && ErrorVal == VDATA)
            {
                ErrorVal = IDATA;                           //Il codice del messaggio è corretto: RM,RS,MM,Ri,...
            }
            //else if (!IsValidMsgRecord(Tokens))
            //{ 
            //    ErrorVal = IDATA                            //Il Messaggio è costruito correttamente
            //}
            return (ErrorVal);
        }
        /// <summary>
        /// verifica se la stazione esiste e può essere usata 
        /// per il correto Load Exchange
        /// </summary>
        /// <param name="StationName"></param>
        /// <param name="Lex">"Pick" o "Drop"</param>
        /// <returns>ritorna il numero del layout: -1 o +1. 
        /// 0 = stn non trovata</returns>
        private int IsValidStation(string StationName, String Lex)
        {
            int floor = 0;
            string LexType = "";
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
                                    @" FROM ""HostStationName""" +
                                    @" WHERE ""StationName"" = '" + StationName.Trim() + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    LexType = reader.GetString(reader.GetOrdinal("LexType"));
                    floor = reader.GetInt16(reader.GetOrdinal("Floor"));
                    break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }

            return (floor);
        }
        /// <summary>
        /// Verifica se l'Ic esiste già in un telegramma (ack o messaggio) accodato
        /// </summary>
        /// <param name="Ic"></param>
        /// <returns></returns>
        private bool IsValidNodeIc(int Ic)
        {
            OracleDataReader reader = null;
            bool IcNew = true;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                // Controlla se l'Ic esiste già tra gli Ack in coda
                //-------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""SendAckToHost""" +
                                    @" WHERE ""IcStart"" = '" + Ic + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    IcNew = false;
                    break;
                }
                reader.Close();


                if (IcNew)
                {
                    // Controlla se l'Ic esiste già tra i messaggi accodati
                    //-----------------------------------------------------
                    cmd.CommandText = @"SELECT *" +
                                        @" FROM ""SendMsgToHost""" +
                                        @" WHERE ""IcStart"" = '" + Ic + "'";
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        IcNew = false;
                        break;
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }

            return (IcNew);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Ic"></param>
        /// <returns></returns>
        private bool IsValidHostIc(int Ic)
        {
            bool IcNew = true;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                // Controlla se l'Ic esiste già tra gli Ack in coda
                //-------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""RecAckFromHost""" +
                                    @" WHERE ""IcStart"" = '" + Ic + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    IcNew = false;
                    break;
                }
                reader.Close();


                if (IcNew)
                {
                    // Controlla se l'Ic esiste già tra i messaggi accodati
                    //-----------------------------------------------------
                    cmd.CommandText = @"SELECT *" +
                                        @" FROM ""RecMsgFromHost""" +
                                        @" WHERE ""IcStart"" = '" + Ic + "'";
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        IcNew = false;
                        break;
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            finally
            {
                if (!reader.IsClosed) reader.Close();
            }

            return (IcNew);
        }
        private int IsATransportAck(int Pa)
        {
            //int rows;
            int Tid = 0;
            string message = "";
            string[] tokens = new string[10];
            Oracle.DataAccess.Client.OracleCommand cmd;
            //System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                // Recupera il valore corrente di Ic e lo aggiorna o incrementadolo o resettandolo a 1
                //------------------------------------------------------------------------------------
                cmd.CommandText = @"SELECT *" +
                                    @" FROM ""SendMsgToHost""" +
                                    @" WHERE ""Ps"" = '" + Pa + "'";
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("Message"))) message = reader.GetString(reader.GetOrdinal("Message"));
                    break;
                }
                reader.Close();
                if (message != "")
                {
                    tokens = message.Split(',');
                    if (tokens[0].Trim() == "SM") Tid = System.Convert.ToInt32(tokens[1]);
                }
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                reader.Close();
            }
            return (Tid);
        }

    }
}