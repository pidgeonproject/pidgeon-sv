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
    /// <summary>
    /// Connection of pidgeon to services
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// The active users.
        /// </summary>
        public static List<Connection> ConnectedUsers = new List<Connection>();
        /// <summary>
        /// The system user
        /// </summary>
        public SystemUser User = null;
        /// <summary>
        /// The status
        /// </summary>
        public Status status = Status.WaitingPW;
        /// <summary>
        /// The client
        /// </summary>
        public System.Net.Sockets.TcpClient client = null;
        private System.IO.StreamReader _StreamReader = null;
        public System.IO.StreamWriter _StreamWriter = null;
        /// <summary>
        /// Using SSL
        /// </summary>
        public bool SSL = true;
        /// <summary>
        /// The main
        /// </summary>
        private Thread main = null;
        /// <summary>
        /// The IP
        /// </summary>
        public string IP;
        /// <summary>
        /// Protocol
        /// </summary>
        private ProtocolMain protocol = null;
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
            SystemLog.DebugLog("Destructor called for " + IP);
        }

        public static void ConnectionKiller(object data)
        {
            try
            {
                Connection conn = (Connection)data;
                conn.Timeout();
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

        public void Timeout()
        {
            if (main != null)
            {
                Thread.Sleep(60000);
                if (status == Connection.Status.WaitingPW)
                {
                    SystemLog.Text("Failed to authenticate in time - killing connection " + IP);
                    Core.DisableThread(main);
                    Clean();
                    return;
                }
            }
            else
            {
                SystemLog.DebugLog("Invalid main of " + IP);
            }
        }

        /// <summary>
        /// Disconnect from client and close all underlying objects
        /// </summary>
        public void Disconnect()
        {
            lock (this)
            {
                if (Connected)
                {
                    if (protocol != null)
                    {
                        protocol.Exit();
                        protocol = null;
                    }
                    if (_StreamReader != null)
                    {
                        _StreamReader.Close();
                        _StreamReader = null;
                    }
                    if (_StreamWriter != null)
                    {
                        _StreamWriter.Close();
                        _StreamWriter = null;
                    }
                    Connected = false;
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
                SystemLog.Text("Opening a new " + ssl + "connection to " + client.Client.RemoteEndPoint.ToString());
                connection.main = Thread.CurrentThread;
                connection.client = client;
                connection.IP = client.Client.RemoteEndPoint.ToString();
                Thread checker = new Thread(ConnectionKiller);
                checker.Name = "watcher";
                connection.SSL = SSL;
                connection.Connected = true;
                checker.Start(connection);

                lock (ConnectedUsers)
                {
                    ConnectedUsers.Add(connection);
                }

                if (SSL)
                {
                    X509Certificate cert = new X509Certificate2(Configuration._System.CertificatePath, "pidgeon");
                    System.Net.Security.SslStream _networkSsl = new SslStream(client.GetStream(), false,
                        new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    _networkSsl.AuthenticateAsServer(cert);
                    connection._StreamWriter = new StreamWriter(_networkSsl);
                    connection._StreamReader = new StreamReader(_networkSsl, Encoding.UTF8);
                }
                else
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
                            connection.protocol.ParseCommand(text);
                            continue;
                        }
                        else
                        {
                            SystemLog.Text("Debug: invalid text: " + text + " from " + client.Client.RemoteEndPoint.ToString());
                            System.Threading.Thread.Sleep(800);
                        }
                    }
                    catch (IOException)
                    {
                        SystemLog.Text("Connection closed: " + connection.IP);
                        connection.Clean();
                        return;
                    }
                    catch (ThreadAbortException)
                    {
                        connection.Clean();
                        return;
                    }
                }
                SystemLog.Text("Connection closed by remote: " + connection.IP);
                connection.Clean();
                return;
            }
            catch (IOException)
            {
                SystemLog.Text("Connection closed: " + connection.IP);
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            if (connection != null)
            {
                connection.Clean();
            }
        }

        public static void InitialiseClient(object data)
        {
            InitialiseClient(data, false);
        }

        public static void InitialiseClientSSL(object data)
        {
            InitialiseClient(data, true);
        }

        public void Clean()
        {
            try
            {
                SystemLog.Text("Disconnecting connection: " + IP);
                Disconnect();

                lock (ConnectedUsers)
                {
                    if (ConnectedUsers.Contains(this))
                    {
                        ConnectedUsers.Remove(this);
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
