//********************************************************************
// Filename: HostBridgeUpdate.cs 
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
// 170817   ral         Gestione del ReadFlag
//********************************************************************

using System;
using MhcsLib;
using System.Collections;
using Oracle.DataAccess.Client;
using System.Net.Mail;

namespace HostBridge
{
    partial class HostBridge : MhcsLib.SvcApp
    {
        OracleConnection connection;
        #region ORDINI DI TRASPORTO *****************
        /// <summary>
        /// <para>L'ordine di trasporto è stato creato e inoltrato al gestore degli AGV - AgvCtl</para>
        /// <para>Il trasporto è ready e non è ancora stato assegnato ad alcun veicolo</para>
        /// <para>In questa tabella sono archiviati soltanto i trasporti creati da Host, non quelli locali</para>
        /// </summary>
        /// <param name="orderid"></param>
        /// <param name="loadid"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="strprio"></param>
        /// <param name="Connection"></param>
        private void CreateOrderInMaster(string orderid, string loadid, string source, string dest, string strprio, string phase)
        {
            int rows;

            OracleTransaction ZoneTrans = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            int id = System.Convert.ToInt32(orderid);
            int prio = System.Convert.ToInt32(strprio);
            int ph = System.Convert.ToInt32(phase);
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;
                // Create the record with the status Waiting
                //------------------------------------------
                cmd.CommandText = @"INSERT INTO ""TrOrderMaster"" " +
                                    @"VALUES (" +
                                                @"'" + id + "', " +    // TrOrderNr
                                                @"'" + 0 + "', " +          // AgvTyp
                                                @"'" + 0 + "', " +          // AgvNr
                                                @"'" + loadid + "', " +     // UDC
                                                @"'" + source + "', " +     // SourceName (Source station name)
                                                @"'" + dest + "', " +       // DestName (Destination station name)
                                                @"'" + 1 + "', " +          // TrOrderType (1 = HostType; 0 = local transport)
                                                @"'" + prio + "', " +       // Priority (1 = Urgent; 0 = not urgent)
                                                @"'" + 0 + "', " +          // TrOrderState (0 = available; 1 = assigned; 9 = todelete)
                                                @"'" + ph + "', " +         // TrOrderPhase (0 = ready; 1 = pick phase; 2 = drop phase; 9 = completed)
                                                @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ", " +       // Data creazione
                                                @"To_DATE(null)" + ", " +   // StartTime
                                                @"To_DATE(null)" + ", " +   // PickTime
                                                @"To_DATE(null)" + ", " +   // DropTime
                                                @"To_DATE(null)" + ", " +   // EndTime
                                                @"'" + "" + "'" + ")";      // Error


                rows = cmd.ExecuteNonQuery();
                ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                ZoneTrans.Rollback();
                if (!reader.IsClosed) reader.Close();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                ZoneTrans.Rollback();
                if (!reader.IsClosed) reader.Close();
            }

        }
        /// <summary>
        /// Cancella un record dal master dei trasporti e lo inserisce nello storico
        /// </summary>
        /// <param name="orderid"></param>
        /// <param name="Connection"></param>
        private void DeleteOrderInMaster(Int64 orderid)
        {
            int TmpTrOrderNr, TmpAgvType, TmpAgvNr;
            int TmpTrOrderType, TmpUrgent, TmpTrOrderState, TmpTrOderPhase;
            System.DateTime TmpCreateTime, TmpStartTime, TmpPickTime, TmpDropTime, TmpEndTime;
            string TmpError, TmpUDC, TmpSourceName, TmpDestName;
            bool onefound = false;
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            OracleTransaction ZoneTrans = null;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();

            TmpTrOrderNr = TmpAgvType = TmpAgvNr = 0;
            TmpTrOrderType = TmpUrgent = TmpTrOrderState = TmpTrOderPhase = 0;
            TmpCreateTime = TmpStartTime = TmpPickTime = TmpDropTime = TmpEndTime = Convert.ToDateTime(null);
            TmpError = TmpUDC = TmpSourceName = TmpDestName = "";

            try
            {
                cmd.Connection = connection;
                ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted); // Sul try più esterno per garantire consistenza

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
                        TmpTrOrderNr = (int)reader.GetDecimal(reader.GetOrdinal("TrOrderNr"));
                        TmpAgvType = (int)reader.GetDecimal(reader.GetOrdinal("AgvType"));
                        TmpAgvNr = (int)reader.GetDecimal(reader.GetOrdinal("AgvNr"));
                        TmpUDC = reader.GetString(reader.GetOrdinal("UDC"));
                        TmpSourceName = reader.GetString(reader.GetOrdinal("SourceName"));
                        TmpDestName = reader.GetString(reader.GetOrdinal("DestName"));
                        TmpTrOrderType = (int)reader.GetDecimal(reader.GetOrdinal("TrOrderType"));
                        TmpUrgent = (int)reader.GetDecimal(reader.GetOrdinal("Urgent"));
                        TmpTrOrderState = (int)reader.GetDecimal(reader.GetOrdinal("TrOrderState"));
                        TmpTrOderPhase = (int)reader.GetDecimal(reader.GetOrdinal("TrOrderPhase"));
                        if (!reader.IsDBNull(reader.GetOrdinal("CreateTime"))) TmpCreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime"));
                        if (!reader.IsDBNull(reader.GetOrdinal("StartTime"))) TmpStartTime = reader.GetDateTime(reader.GetOrdinal("StartTime"));
                        if (!reader.IsDBNull(reader.GetOrdinal("PickTime"))) TmpPickTime = reader.GetDateTime(reader.GetOrdinal("PickTime"));
                        if (!reader.IsDBNull(reader.GetOrdinal("DropTime"))) TmpDropTime = reader.GetDateTime(reader.GetOrdinal("DropTime"));
                        if (!reader.IsDBNull(reader.GetOrdinal("EndTime"))) TmpEndTime = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                        if (!reader.IsDBNull(reader.GetOrdinal("Error"))) TmpError = reader.GetString(reader.GetOrdinal("Error"));
                        onefound = true;
                        break;
                    }
                    reader.Close();
                }
                catch (OracleException e)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                    ZoneTrans.Rollback();
                    if (!reader.IsClosed) reader.Close();
                }
                catch (Exception ex)
                {
                    MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    ZoneTrans.Rollback();
                    if (!reader.IsClosed) reader.Close();
                }

                if (onefound)
                {
                    try
                    {
                        string StrCreateTime = (TmpCreateTime == Convert.ToDateTime(null) ? "null" : "'" + TmpCreateTime + "'" + ",'" + DATAFORMAT + "'");
                        string StrStartTime = (TmpStartTime == Convert.ToDateTime(null) ? "null" : "'" + TmpStartTime + "'" + ",'" + DATAFORMAT + "'");
                        string StrPickTime = (TmpPickTime == Convert.ToDateTime(null) ? "null" : "'" + TmpPickTime + "'" + ",'" + DATAFORMAT + "'");
                        string StrDropTime = (TmpDropTime == Convert.ToDateTime(null) ? "null" : "'" + TmpDropTime + "'" + ",'" + DATAFORMAT + "'");
                        string StrEndTime = (TmpEndTime == Convert.ToDateTime(null) ? "null" : "'" + TmpEndTime + "'" + ",'" + DATAFORMAT + "'");

                        // Create the record in Transport history table
                        //------------------------------------------
                        cmd.CommandText = @"INSERT INTO ""TrOrderHistory"" " +
                         @"VALUES (" +
                                         @"'" + TmpTrOrderNr + "', " +          // TrOrderNr
                                         @"'" + TmpAgvType + "', " +            // AgvTyp
                                         @"'" + TmpAgvNr + "', " +              // AgvNr
                                         @"'" + TmpUDC + "', " +                // UDC
                                         @"'" + TmpSourceName + "', " +         // SourceName (Source station name)
                                         @"'" + TmpDestName + "', " +           // DestName (Destination station name)
                                         @"'" + TmpTrOrderType + "', " +        // TrOrderType (1 = HostType; 0 = local transport)
                                         @"'" + TmpUrgent + "', " +             // Priority (1 = Urgent; 0 = not urgent)
                                         @"'" + TmpTrOrderState + "', " +       // TrOrderState (0 = available; 1 = assigned; 9 = todelete)
                                         @"'" + TmpTrOderPhase + "', " +        // TrOrderPhase (0 = ready; 1 = pick phase; 2 = drop phase; 9 = completed)
                                         @"To_DATE(" + StrCreateTime + ")" + ", " +    // Data creazione
                                         @"To_DATE(" + StrStartTime + ")" + ", " +     // StartTime
                                         @"To_DATE(" + StrPickTime + ")" + ", " +      // PickTime
                                         @"To_DATE(" + StrDropTime + ")" + ", " +      // DropTime
                                         @"To_DATE(" + StrEndTime + ")" + ", " +       // EndTime
                                         @"'" + TmpError + "'" + ")";           // Error

                        rows = cmd.ExecuteNonQuery();
                    }
                    catch (OracleException e)
                    {
                        MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                        ZoneTrans.Rollback();
                        if (!reader.IsClosed) reader.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                        ZoneTrans.Rollback();
                        if (!reader.IsClosed) reader.Close();
                    }


                    try
                    {
                        // Delete the TrOrderMaster
                        //------------------------------------------
                        cmd.CommandText = @"DELETE ""TrOrderMaster"" " +
                                        @" WHERE ""TrOrderNr"" ='" + orderid + "'";     // Matching on ID or the tansport

                        rows = cmd.ExecuteNonQuery();

                    }
                    catch (OracleException e)
                    {
                        MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                        if (!reader.IsClosed) reader.Close();
                        ZoneTrans.Rollback();
                    }
                    catch (Exception ex)
                    {
                        MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                        if (!reader.IsClosed) reader.Close();
                    }
                }
                ZoneTrans.Commit();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                ZoneTrans.Rollback();
            }
        }
        /// <summary>
        /// <para>L'ordine di trasporto è stato assegnato a un AGV</para>
        /// </summary>
        /// <param name="orderid"></param>
        /// <param name="agvnr"></param>
        /// <param name="Connection"></param>
        private void StartTrOrderInMaster(Int64 orderid, int agvnr, OracleConnection Connection)
        {
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            OracleTransaction ZoneTrans = null;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                cmd.Connection = connection;
                ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // Update the TrOrderMaster
                //------------------------------------------
                cmd.CommandText = @"UPDATE ""TrOrderMaster"" " +
                                @"SET ""AgvNr"" ='" + agvnr + "'," +
                                @"""TrOrderState"" ='" + 1 + "'," +
                                @"""TrOrderPhase"" ='" + 1 + "'," +
                                @"""StartTime"" = To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" +
                                @"WHERE " +
                                @"""TrOrderNr"" ='" + orderid + "' ";     // Matching on ID or the tansport

                rows = cmd.ExecuteNonQuery();
                ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
        }
        /// <summary>
        /// E' stato effettuato il prelievo
        /// </summary>
        /// <param name="orderid"></param>
        /// <param name="agvnr"></param>
        /// <param name="Connection"></param>
        private void PickDoneInMaster(Int64 orderid, int agvnr, string error, OracleConnection Connection)
        {
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            OracleTransaction ZoneTrans = null;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                cmd.Connection = connection;
                ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // Update the TrOrderMaster
                //------------------------------------------
                cmd.CommandText = @"UPDATE ""TrOrderMaster"" " +
                                @"SET ""AgvNr"" ='" + agvnr + "'," +
                                @"""TrOrderState"" ='" + 1 + "'," +
                                @"""TrOrderPhase"" ='" + 2 + "'," +
                                @"""Error"" ='" + error + "'," +
                                @"""PickTime"" = To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" +
                                @"WHERE " +
                                @"""TrOrderNr"" ='" + orderid + "' ";     // Matching on ID or the tansport

                rows = cmd.ExecuteNonQuery();
                ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
        }
        /// <summary>
        /// E' stato effettuato il deposito
        /// HostBridge cambia lo stato del trasport in Drop Done e si mette in attesa di un ack per
        /// poter cancellare il trasporto dal master.
        /// </summary>
        /// <param name="message">"[IC],[PS],[RA],[SM],[TID],[UDC],[N/U],[SRC],[DST],[STS],[IC]"</param>
        /// <param name="agvnr"></param>
        /// <param name="Connection"></param>
        private void DropDoneInMaster(string message, int agvnr, OracleConnection Connection)
        {
            string[] sndmessage = new string[20];
            int rows;
            int orderid;
            int error;

            sndmessage = message.Split(',');

            orderid = System.Convert.ToInt32(sndmessage[4]);
            error = System.Convert.ToInt32(sndmessage[9]);

            Oracle.DataAccess.Client.OracleCommand cmd;
            OracleTransaction ZoneTrans = null;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                cmd.Connection = connection;
                ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // Update the TrOrderMaster
                //------------------------------------------
                cmd.CommandText = @"UPDATE ""TrOrderMaster"" " +
                                @"SET ""AgvNr"" ='" + agvnr + "'," +
                                @"""TrOrderState"" ='" + 1 + "'," +
                                @"""TrOrderPhase"" ='" + 3 + "'," +
                                @"""Error"" ='" + error + "'," +
                                @"""DropTime"" = To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + "," +
                                @"""EndTime"" = To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" +
                                @"WHERE " +
                                @"""TrOrderNr"" ='" + orderid + "' ";     // Matching on ID or the tansport

                rows = cmd.ExecuteNonQuery();
                ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
        }
        /// <summary>
        /// chiamata quando il trasporto viene completato in modo anomalo
        /// </summary>
        /// <param name="orderid"></param>
        /// <param name="agvnr"></param>
        /// <param name="trorderstate"></param>
        /// <param name="trorderphase"></param>
        /// <param name="error"></param>
        /// <param name="Connection"></param>
        private void CompletedTrOrderInMaster(Int64 orderid, int agvnr, int trorderstate, int trorderphase, string error, OracleConnection Connection)
        {
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            OracleTransaction ZoneTrans = null;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                cmd.Connection = connection;
                ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // Update the TrOrderMaster
                //------------------------------------------
                cmd.CommandText = @"UPDATE ""TrOrderMaster"" " +
                                @"SET ""AgvNr"" ='" + agvnr + "'," +
                                @"""TrOrderState"" ='" + trorderstate + "'," +
                                @"""TrOrderPhase"" ='" + trorderphase + "'," +
                                @"""Error"" ='" + error + "'," +
                                @"""EndTime"" = To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" +
                                @"WHERE " +
                                @"""TrOrderNr"" ='" + orderid + "' ";     // Matching on ID or the tansport

                rows = cmd.ExecuteNonQuery();
                ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                if (!reader.IsClosed) reader.Close();
                ZoneTrans.Rollback();
            }
        }
        /// <summary>
        /// Setta HostPendingAck al valore passato come parametro
        /// </summary>
        /// <param name="value">1= in attesa; 0 altrimenti</param>
        ///<test>Done</test>
        private void SetHostAckPending(int value)
        {
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;
            FromHostAckPending = value;
            try
            {

                // Aggiorna la tabella di controllo con il nuovo valore Ic
                //--------------------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + value + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'HostAckPending'";
                rows = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
        }
        #endregion
        #region TELEGRAMMI DA E VERSO HOST***********
        /// <summary>
        /// Salva il messaggio nella coda dei messaggi da Host
        /// </summary>
        /// <param name="StrIc"></param>
        /// <param name="StrPs"></param>
        /// <param name="StrRa"></param>
        /// <param name="Message"></param>
        /// <param name="StrIcEnd"></param>
        /// <returns>Restutisce l'esito del salvataggio</returns>
        private int QueueMsgFromHost(string StrIc, string StrPs, string StrRa, string MCod, string StrIcEnd)
        {
            int result      = BFULL;
            int rows;
            int Ic, Ps;
            string Message  = "";
            Ic              = System.Convert.ToInt32(StrIc);
            Ps              = System.Convert.ToInt32(StrPs);
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;
                // Create the record with the status Waiting
                //------------------------------------------
                Message = MCod;
                cmd.CommandText = @"INSERT INTO ""RecMsgFromHost"" " +
                                  @"VALUES (" +
                                                @"'" + StrIc + "', " +          // Contatore di inizio messaggio
                                                @"'" + StrPs + "', " +          // Progressivo messaggio
                                                @"'" + StrRa + "', " +          // Flag di richiesta ack
                                                @"'" + Message + "', " +        // Il corpo del messaggio
                                                @"'" + StrIc + "', " +          // Contatore di inizio messaggio
                                                @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ")";

                rows = cmd.ExecuteNonQuery();

                // Update Host Ic and Ps in Host Control Board
                //------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + Ic + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'IcReceived'";

                rows = cmd.ExecuteNonQuery();

                // Update Host Ic and Ps in Host Control Board
                //------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + Ps + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'PsReceived'";

                rows = cmd.ExecuteNonQuery();

                //SaveHostCounters(System.Convert.ToInt32(Ic), System.Convert.ToInt32(Ps));

                //ZoneTrans.Commit();
                result = VDATA;                                             // Operazione andata a buon fine
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            return (result);
        }
        private int QueueMsgFromHost(string StrIc, string StrPs, string StrRa, string MCod, string Trn, string Udc, string Source, string Dest, string Prio, string ReadFlag, string IcEnd)
        {
            int result      = BFULL;
            int Ic, Ps;
            string Message  = "";
            Ic = System.Convert.ToInt32(StrIc);
            Ps = System.Convert.ToInt32(StrPs);
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;
                Message = MCod + "," + Trn + "," + Udc + "," + Source + "," + Dest + "," + Prio + "," + ReadFlag;
                // Create the record with the status Waiting
                //------------------------------------------
                cmd.CommandText = @"INSERT INTO ""RecMsgFromHost"" " +
                                  @"VALUES (" +
                                                @"'" + StrIc + "', " +          // Contatore di inizio messaggio
                                                @"'" + StrPs + "', " +          // Progressivo messaggio
                                                @"'" + StrRa + "', " +          // Flag di richiesta ack
                                                @"'" + Message + "', " +        // Il corpo del messaggio
                                                @"'" + StrIc + "', " +          // Contatore di inizio messaggio
                                                @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ")";

                rows = cmd.ExecuteNonQuery();

                // Update Host Ic and Ps in Host Control Board
                //------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + Ic + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'IcReceived'";

                rows = cmd.ExecuteNonQuery();

                // Update Host Ic and Ps in Host Control Board
                //------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + Ps + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'PsReceived'";

                //SaveHostCounters(System.Convert.ToInt32(Ic), System.Convert.ToInt32(Ps));

                //ZoneTrans.Commit();
                result = VDATA;                                             // Operazione andata a buon fine

            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            return (result);
        }
        /// <summary>
        /// Cancella il messaggio da Host con il Ps dato
        /// </summary>
        /// <param name="Ps"></param>
        private int QueueAckFromHost(string Ic, string Ps, string Ra, string Di, string IcEnd)
        {
            int result = VDATA;
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;
                // Create the record with the status Waiting
                //------------------------------------------
                cmd.CommandText = @"INSERT INTO ""RecAckFromHost"" " +
                                  @"VALUES (" +
                                                @"'" + Ic + "', " +         // Contatore di inizio messaggio
                                                @"'" + Ps + "', " +         // Progressivo messaggio
                                                @"'" + Ra + "', " +         // Flag di richiesta ack
                                                @"'" + Di + "', " +         // Diagnostico
                                                @"'" + IcEnd + "', " +      // Contatore di fine messaggio
                                                @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ")";

                rows = cmd.ExecuteNonQuery();

                // Update Host Ic and Ps in Host Control Board
                //------------------------------------------
                SaveHostCounters(System.Convert.ToInt32(Ic), System.Convert.ToInt32(Ps));

                //ZoneTrans.Commit();
                result = VDATA;                                             // Operazione andata a buon fine
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            return (result);
        }
        private int QueueAckToHost(string Ic, string Pa, string Ra, string Di, string IcEnd)
        {
            int result = VDATA;
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;
                // Create the record with the status Waiting
                //------------------------------------------
                cmd.CommandText = @"INSERT INTO ""SendAckToHost"" " +
                                  @"VALUES (" +
                                                @"'" + Ic + "', " +         // Contatore di inizio messaggio
                                                @"'" + Pa + "', " +         // Progressivo messaggio
                                                @"'" + Ra + "', " +         // Flag di richiesta ack
                                                @"'" + Di + "', " +         // Diagnostico
                                                @"'" + IcEnd + "', " +       // Contatore di fine messaggio
                                                @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ")";

                rows = cmd.ExecuteNonQuery();
                //ZoneTrans.Commit();
                result = VDATA;                                             // Operazione andata a buon fine
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            return (result);
        }
        /// <summary>
        /// Salva nel DB il messaggio da inviare a Host
        /// </summary>
        /// <param name="SndMessage">[IC],[PS],[RA],[SM],[TID],[UDC],[N/U],[SRC],[DST],[STS],[IC]</param>
        /// <returns></returns>
        private int QueueSMMsgToHost(string SndMessage)
        {
            string Ic, Ps, Ra, Body;
            int result = BFULL;
            int rows;
            int buflen;
            int Seq = 0;
            buflen = SndMessage.Split(',').Length;
            Body = "";

            Ic = SndMessage.Split(',')[0];
            Ps = SndMessage.Split(',')[1];
            Ra = SndMessage.Split(',')[2];

            Body += SndMessage.Split(',')[3]; 
            for (int i = 4; i < buflen - 1; i++) //Takes out the last IC
            {
                Body += "," + SndMessage.Split(',')[i];
            }

            OracleTransaction ZoneTrans = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;

                cmd.CommandText = @"SELECT MAX(""Seq"")FROM ""SendMsgToHost"" ";


                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader[0].ToString() != "")
                    {
                        Seq = Convert.ToInt32(reader[0].ToString());
                    }
                }
                reader.Close();

                if (Seq < 10000)
                {
                    Seq++;
                }
                else
                {
                    Seq = 1;
                }

                // Create the record with the status Waiting
                //------------------------------------------
                cmd.CommandText = @"INSERT INTO ""SendMsgToHost"" " +
                                  @"VALUES (" +
                                                @"'" + Ic   + "', " +       // Contatore di inizio messaggio
                                                @"'" + Ps   + "', " +       // Progressivo messaggio
                                                @"'" + Ra   + "', " +       // Flag di richiesta ack
                                                @"'" + Body + "', " +       // Il corpo del messaggio
                                                @"'" + Ic   + "', " +       // Contatore di fine messaggio
                                                @"'" + Seq  + "', " +       // Contatore di fine messaggio
                                                @"TO_DATE(" + "'" +         // Data e orario della regisrazione
                                                    TlgTime + "'" + ",'" + 
                                                    DATAFORMAT + "')" + ")";
                                                

                rows = cmd.ExecuteNonQuery();

                //ZoneTrans.Commit();
                result = VDATA;                                             // Operazione andata a buon fine
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                ZoneTrans.Rollback();
                if (!reader.IsClosed) reader.Close();
                result = BFULL;
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                ZoneTrans.Rollback();
                if (!reader.IsClosed) reader.Close();
                result = BFULL;
            }
            return (result);
        }
        private int QueueSIMsgToHost(string SndMessage)
        {
            string Ic, Ps, Ra, Body;
            int result = BFULL;
            int rows;
            Ic = SndMessage.Split(',')[0];
            Ps = SndMessage.Split(',')[1];
            Ra = SndMessage.Split(',')[2];
            Body = SndMessage.Split(',')[3] +
                ',' + SndMessage.Split(',')[4] + ',' + SndMessage.Split(',')[5];

            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;
                // Create the record with the status Waiting
                //------------------------------------------
                cmd.CommandText = @"INSERT INTO ""SendMsgToHost"" " +
                                  @"VALUES (" +
                                                @"'" + Ic + "', " +         // Contatore di inizio messaggio
                                                @"'" + Ps + "', " +         // Progressivo messaggio
                                                @"'" + Ra + "', " +         // Flag di richiesta ack
                                                @"'" + Body + "', " +       // Il corpo del messaggio
                                                @"'" + Ic + "', " +         // Contatore di fine messaggio
                                                @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ")";

                rows = cmd.ExecuteNonQuery();

                //ZoneTrans.Commit();
                result = VDATA;                                             // Operazione andata a buon fine
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                //ZoneTrans.Rollback();
                result = BFULL;
            }
            return (result);
        }

        /// <summary>
        /// Salva nella Host control board i valori dei contatori Ps e Ic ricevuti con l'utimo telegramma da Host
        /// </summary>
        /// <param name="Ic"></param>
        /// <param name="Ps"></param>
        private void SaveHostCounters(int Ic, int Ps)
        {
            int rows;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                // Update Host Ic and Ps in Host Control Board
                //------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + Ic + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'IcReceived'";

                rows = cmd.ExecuteNonQuery();

                // Update Host Ic and Ps in Host Control Board
                //------------------------------------------
                cmd.CommandText = @"UPDATE  ""HostControlBoard"" " +
                                    @"SET " +
                                    @"""IntVal"" = '" + Ps + "'" +
                                    @"WHERE " +
                                    @"""ItemName"" =" + "'PsReceived'";

                rows = cmd.ExecuteNonQuery();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                //ZoneTrans.Rollback();
            }
        }
        /// <summary>
        /// Cancella un messaggio dalla coda messaggi verso Host
        /// </summary>
        /// <param name="Ps">Il Ps del messaggio da cancellare</param>
        private void DequeueMessageToHost(int Ps)
        {
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                cmd.CommandText = @"DELETE ""SendMsgToHost""" +
                                  @" WHERE ""Ps"" = '" + Ps + "'";
                int m = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
        }
        /// <summary>
        /// Cancella, dalla coda degli Ack verso Host, l'Ack con il Ps corrispondente
        /// </summary>
        /// <param name="Ps"></param>
        private void DequeueAckToHost(int Ps)
        {
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                cmd.CommandText = @"DELETE ""SendAckToHost""" +
                                  @" WHERE ""Ps"" = '" + Ps + "'";
                int m = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
        }
        /// <summary>
        /// Cancella un messaggio dalla coda messaggi da Host
        /// </summary>
        /// <param name="Ps">Il Ps del messaggio da cancellare</param>
        /// <test>Done</test>
        private void DequeueMessageFromHost(int Ps)
        {
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                cmd.CommandText = @"DELETE ""RecMsgFromHost""" +
                                  @" WHERE ""Ps"" = '" + Ps + "'";
                int m = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
        }
        /// <summary>
        /// Cancella, dalla coda degli Ack verso Host, l'Ack con il Ps corrispondente
        /// </summary>
        /// <param name="Ps"></param>
        private void DequeueAckFromHost(int Ps)
        {
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            cmd.Connection = Connection;

            try
            {
                cmd.CommandText = @"DELETE ""RecAckFromHost""" +
                                  @" WHERE ""Ps"" = '" + Ps + "'";
                int m = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
        }
        /// <summary>
        /// <para>Aggiorna il log dei messaggi inviati e ricevuti tra Node e Host</para>
        /// </summary>
        /// <param name="Sender">Chi invia</param>
        /// <param name="MsgType">Ack o Msg</param>
        /// <param name="MessageBody">Il messaggio</param>
        private void InserRecordInHostLog(string Sender, string MsgType, string MessageBody)
        {
            int rows;

            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                cmd.Connection = connection;
                // Create the record with the status Waiting
                //------------------------------------------
                cmd.CommandText = @"INSERT INTO ""HostTlgLog"" " +
                                   @"VALUES (" +
                                       @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ", " +       // Data registazione
                                       @"'" + Sender + "', " +                                                     // Chi ha inviato il telegramma
                                       @"'" + MsgType + "', " +                                                    // Se si tratta di un Ack o di un Msg
                                       @"'" + MessageBody + "')";                                                 // Il messaggio


                rows = cmd.ExecuteNonQuery();
                //ZoneTrans.Commit();
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
                //ZoneTrans.Rollback();
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                //ZoneTrans.Rollback();
            }

        }
        /// <summary>
        /// <para>Inserisce un nuovo record nella tabella degli allarmi</para>
        /// <para>il massimo numero di record in tabella è definito da MaxAlarms in HostGlobals</para>
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="Severity"></param>
        /// <param name="AlarmText"></param>
        private void InserRecordInAlarms(string Sender, string Severity, string AlarmText)
        {
            int rows;
            int Seq = 0;
            bool AlarmRecordFound = false;
            string myemail = "segrate.mhmsystem@sepnsms.segratepharma.com";
            string toemail = "";

            if (AlarmText.Length > 256) AlarmText = AlarmText.Substring(0, 254);        // Limita la lunghezza della stringa al massimo consentito
            OracleDataReader reader = null;
            Oracle.DataAccess.Client.OracleCommand cmd;
            System.DateTime TlgTime = System.DateTime.Now;
            this.connection = Connection;
            cmd = new Oracle.DataAccess.Client.OracleCommand();
            try
            {
                Seq = GetNextAlarmSeq();

                cmd.Connection = connection;
                cmd.CommandText = @"SELECT *" +
                    @" FROM ""Alarms""" +
                    @" WHERE ""Seq"" = '" + Seq + "'";

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AlarmRecordFound = true;
                    break;
                }
                reader.Close();

                if (AlarmRecordFound)
                {
                    try
                    {

                        // Create the record In Alarms
                        //------------------------------------------
                        cmd.CommandText = @"UPDATE ""Alarms"" " +
                                          @"SET " +
                                                @"""CreateTime"" = To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')," +        // Data registazione
                                                @"""Sender"" ='" + Sender + "', " +                                                     // Chi ha generato l'allarme
                                                @"""Severity"" ='" + Severity + "', " +                                                 // importanza dell'allarme
                                                @"""Text"" ='" + AlarmText + "'" +
                                                @"WHERE " +
                                                @"""Seq"" ='" + Seq + "' ";     // Matching on ID or the tansport

                        rows = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    }
                }
                else
                {
                    //ZoneTrans = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                    try
                    {
                        // Create the record In Alarms
                        //------------------------------------------
                        cmd.CommandText = @"INSERT INTO ""Alarms"" " +
                                           @"VALUES (" +
                                               @"'" + Seq + "', " +                                                        // Il numero di sequenza
                                               @"To_DATE(" + "'" + TlgTime + "'" + ",'" + DATAFORMAT + "')" + ", " +       // Data registazione
                                               @"'" + Sender + "', " +                                                     // Chi ha generato l'allarme
                                               @"'" + Severity + "', " +                                                   // importanza dell'allarme
                                               @"'" + AlarmText + "')";                                                    // Il testo dell'allarme

                        rows = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
                    }
                }
            }
            catch (OracleException e)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ORACLE_EXCEPTION, cmd.CommandText, e.Message);
            }
            catch (Exception ex)
            {
                MessageWriter.Log(MhcsLib.DebugZones.Errors, HOSTBRIDGE_ERR_COMMAND, cmd.CommandText, ex.Message);
            }
            try // Send SMS
            {
                cmd.CommandText = @"SELECT * FROM ""AgvSmsNumbers""";

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    toemail = reader.GetString(reader.GetOrdinal("Number"));
                    MailMessage mail = new MailMessage(myemail, toemail + "@sepnsms01.segratepharma.com");
                    SmtpClient client = new SmtpClient();
                    client.Port = 25;
                    client.EnableSsl = false;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Host = "sepmmailp01.segratepharma.com";
                    client.Credentials = new System.Net.NetworkCredential(@"SEPUSER\alaimor", "Segrate2017");

                    mail.Subject = "MHM Messaging System on Production";
                    mail.Body = AlarmText;
                    try
                    {
                        if (Severity != "Information" && Severity != "Informazione")
                        {
                            client.Send(mail); 
                        }
                    }
                    catch (Exception)
                    {
                        //throw;
                    }
                }
                reader.Close();
                
            }
            catch (Exception)
            {
                if (!reader.IsClosed) reader.Close();
            }
        }

        #endregion

    }
}