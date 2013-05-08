﻿/***************************************************************************
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
        private System.IO.StreamReader _StreamReader = null;
        public System.IO.StreamWriter _StreamWriter = null;
        /// <summary>
        /// Using SSL
        /// </summary>
        public bool SSL = true;
        /// <summary>
        /// The main.
        /// </summary>
        private Thread main = null;
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
                        Core.DisableThread(conn.main);
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
                        protocol.Exit();
                        protocol = null;
                    }
                    if (_StreamReader != null)
                    {
                        _StreamReader.Close();
                    }
                    if (_StreamWriter != null)
                    {
                        _StreamWriter.Close();
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
                    X509Certificate cert = new X509Certificate2(Config._System.CertificatePath, "pidgeon");
                    System.Net.Security.SslStream _networkSsl = new SslStream(client.GetStream(), false,
                        new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    _networkSsl.AuthenticateAsServer(cert);
                    connection._StreamWriter = new StreamWriter(_networkSsl);
                    connection._StreamReader = new StreamReader(_networkSsl, Encoding.UTF8);
                } else
                {
                    System.Net.Sockets.NetworkStream ns = client.GetStream();
                    connection._StreamWriter = new StreamWriter(ns);
                    connection._StreamReader = new StreamReader(ns, Encoding.UTF8);
                }

                string text = connection._StreamReader.ReadLine();

                connection.protocol = new ProtocolMain(connection);
                while (connection.IsConnected && !connection._StreamReader.EndOfStream)
                {
                    try
                    {
                        text = connection._StreamReader.ReadLine();
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
            // there is a high possibility of some network exception here
            try
            {
                Core.SL("Disconnecting connection: " + connection.IP);
                connection.Disconnect();
                connection.protocol = null;
                GC.Collect();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }

            try
            {
                lock (ActiveUsers)
                {
                    if (ActiveUsers.Contains(connection))
                    {
                        ActiveUsers.Remove(connection);
                    }
                }
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
