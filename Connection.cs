/***************************************************************************
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) version 3.                                           *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.         *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;

namespace pidgeon_sv
{
    public class Connection
    {
        /// <summary>
        /// The active users.
        /// </summary>
        public static List<Connection> ActiveUsers = new List<Connection>();
        /// <summary>
        /// The account.
        /// </summary>
        public SystemUser account = null;
        /// <summary>
        /// The status.
        /// </summary>
        public Status status = Status.WaitingPW;
        /// <summary>
        /// The client.
        /// </summary>
        public System.Net.Sockets.TcpClient client = null;
        /// <summary>
        /// The _r.
        /// </summary>
        public System.IO.StreamReader _r = null;
        /// <summary>
        /// The _w.
        /// </summary>
        public System.IO.StreamWriter _w = null;
        public bool SSL = true;
        /// <summary>
        /// The main.
        /// </summary>
        public Thread main = null;
        /// <summary>
        /// The IP
        /// </summary>
        public string IP;
        private ProtocolMain protocol;
        private bool Connected = false;
        /// <summary>
        /// Connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return Connected;
            }
        }

        public Connection()
        {
            protocol = null;
            IP = "unknown";
        }

        ~Connection()
        {
            Core.DebugLog("Destructor called for " + IP);
        }

        public static void ConnectionKiller(object data)
        {
            try
            {
                Connection conn = (Connection)data;
                if (conn.main != null)
                {
                    Thread.Sleep(60000);
                    if (conn.status == Connection.Status.WaitingPW)
                    {
                        Core.SL("Failed to authenticate in time - killing connection " + conn.IP);
                        if (conn.main.ThreadState == ThreadState.WaitSleepJoin || conn.main.ThreadState == ThreadState.Running)
                        {
                            conn.main.Abort();
                        }
                        else
                        {
                            Core.SL("DEBUG: The thread is aborted " + conn.IP);
                        }
                        ConnectionClean(conn);
                        return;
                    }
                }
                else
                {
                    Core.SL("DEBUG: NULL " + conn.IP);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }
        
        public void Disconnect()
        {
            lock (this)
            {
                if (Connected)
                {
                    Connected = false;
                    if (protocol != null)
                    {
                        protocol.Disconnect();
                    }
                    if (_r != null)
                    {
                        _r.Close();
                    }
                    if (_w != null)
                    {
                        _w.Close();
                    }
                }
            }
        }
        
        public static void InitialiseClient(object data, bool SSL)
        {
            Connection connection = null;
            try
            {
                string ssl = "";
                if (SSL)
                {
                    ssl = "SSL ";
                }
                System.Net.Sockets.TcpClient client = (System.Net.Sockets.TcpClient)data;
                connection = new Connection();
                Core.SL("Opening a new " + ssl + "connection to " + client.Client.RemoteEndPoint.ToString());
                connection.main = Thread.CurrentThread;
                connection.client = client;
                connection.IP = client.Client.RemoteEndPoint.ToString();
                Thread checker = new Thread(ConnectionKiller);
                checker.Name = "watcher";
                connection.SSL = SSL;
                connection.Connected = true;
                checker.Start(connection);

                lock (ActiveUsers)
                {
                    ActiveUsers.Add(connection);
                }
                
                if (SSL)
                {
                    X509Certificate cert = new X509Certificate2(Config.CertificatePath, "pidgeon");
                    System.Net.Security.SslStream _networkSsl = new SslStream(client.GetStream(), false,
                        new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    _networkSsl.AuthenticateAsServer(cert);
                    connection._w = new StreamWriter(_networkSsl);
                    connection._r = new StreamReader(_networkSsl, Encoding.UTF8);
                } else
                {
                    System.Net.Sockets.NetworkStream ns = client.GetStream();
                    connection._w = new StreamWriter(ns);
                    connection._r = new StreamReader(ns, Encoding.UTF8);
                }

                string text = connection._r.ReadLine();

                connection.protocol = new ProtocolMain(connection);
                while (connection.IsConnected && !connection._r.EndOfStream)
                {
                    try
                    {
                        text = connection._r.ReadLine();
                        if (text == "")
                        {
                            continue;
                        }

                        if (ProtocolMain.Valid(text))
                        {
                            connection.protocol.parseCommand(text);
                            continue;
                        }
                        else
                        {
                            Core.SL("Debug: invalid text: " + text + " from " + client.Client.RemoteEndPoint.ToString());
                            System.Threading.Thread.Sleep(800);
                        }
                    }
                    catch (IOException)
                    {
                        Core.SL("Connection closed: " + connection.IP);
                        connection.protocol.Exit();
                        connection.Connected = false;
                        ConnectionClean(connection);
                        return;

                    }
                    catch (ThreadAbortException)
                    {
                        connection.protocol.Exit();
                        connection.Connected = false;
                        ConnectionClean(connection);
                        return;
                    }
                    catch (Exception fail)
                    {
                        Core.handleException(fail);
                    }
                }
                Core.SL("Connection closed by remote: " + connection.IP);
                connection.protocol.Exit();
                ConnectionClean(connection);
            }
            catch (System.IO.IOException)
            {
                Core.SL("Connection closed: " + connection.IP);
                ConnectionClean(connection);
                return;
            }
            catch (ThreadAbortException)
            {
                if (connection != null)
                {
                    ConnectionClean(connection);
                }
                return;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                if (connection != null)
                {
                    ConnectionClean(connection);
                }
                return;
            }
        }
        
        public static void InitialiseClient(object data)
        {
            InitialiseClient (data, false);
        }
        
        public static void InitialiseClientSSL(object data)
        {
            InitialiseClient (data, true);
        }
        
        /// <summary>
        /// Remove all data associated with the connection
        /// </summary>
        /// <param name="connection"></param>
        public static void ConnectionClean(Connection connection)
        {
            try
            {
                Core.SL("Cleaning data for connection: " + connection.IP);
                connection.Disconnect();
                lock (ActiveUsers)
                {
                    if (ActiveUsers.Contains(connection))
                    {
                        ActiveUsers.Remove(connection);
                        Core.SL("Data for connection were cleaned: " + connection.IP);
                        connection.client.Client.Close();
                        connection.client.Close();
                    }
                }
                connection.protocol = null;
                GC.Collect();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        
        public enum Status
        {
            WaitingPW,
            Connected,
            Disconnected,
        }
    }
}
