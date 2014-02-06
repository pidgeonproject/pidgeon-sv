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
    public class Session
    {
        /// <summary>
        /// The active users.
        /// </summary>
        public static List<Session> ConnectedUsers = new List<Session>();
        private static ulong LastSID = 0;
        private static object LastSIDlock = new object();
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
        //private System.Net.Sockets.TcpClient client = null;
        private System.IO.StreamReader _StreamReader = null;
        public System.IO.StreamWriter _StreamWriter = null;
        /// <summary>
        /// Whether this session is secured using SSL or not
        /// </summary>
        public bool UsingSSL = true;
        /// <summary>
        /// The session thread.
        /// </summary>
        private Thread SessionThread = null;
        public DateTime CreatedTime
        {
            get
            {
                return createdtime;
            }
        }
        private DateTime createdtime;
        /// <summary>
        /// The IP
        /// </summary>
        public string IP;
        /// <summary>
        /// Gets the protocol.
        /// </summary>
        /// <value>
        /// The protocol.
        /// </value>
        public ProtocolMain Protocol
        {
            get
            {
                return protocol;
            }
        }
        /// <summary>
        /// Protocol which this session is using to communicate with client
        /// </summary>
        private ProtocolMain protocol = null;
        private bool Connected = false;
        public ulong SessionID
        {
            get
            {
                return this.SID;
            }
        }
        private ulong SID = 0;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="pidgeon_sv.Session"/> class.
        /// </summary>
        public Session()
        {
            this.protocol = null;
            this.IP = "unknown";
            this.createdtime = DateTime.Now;
            this.SID = GetSID();
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="pidgeon_sv.Session"/> is reclaimed by garbage collection.
        /// </summary>
        ~Session()
        {
            SystemLog.DebugLog("Destructor called for session of " + IP);
        }

        private static void ConnectionKiller(object data)
        {
            try
            {
                Session conn = (Session)data;
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

        private void Timeout()
        {
            if (SessionThread != null)
            {
                Thread.Sleep(60000);
                if (status == Session.Status.WaitingPW)
                {
                    SystemLog.WriteLine("Failed to authenticate in time - killing connection " + IP);
                    Core.DisableThread(SessionThread);
                    Clean();
                    return;
                }
            }
            else
            {
                SystemLog.DebugLog("SessionThread is NULL for " + this.IP);
            }
        }

        private static ulong GetSID()
        {
            lock (LastSIDlock)
            {
                LastSID++;
                return LastSID;
            }
        }

        public void Kill()
        {
            SystemLog.WriteLine("Killing session " + this.SessionID.ToString());
            this.Disconnect();
            Core.DisableThread(this.SessionThread);
        }

        /// <summary>
        /// Disconnect from client and close all underlying objects
        /// </summary>
        public void Disconnect()
        {
            SystemLog.WriteLine("Disconnecting from: " + this.IP);
            lock (this)
            {
                Connected = false;
                if (this.protocol != null && this.protocol.IsConnected)
                {
                    this.protocol.Disconnect();
                }
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
                }
                SystemUser user = this.User;
                if (user != null)
                {
                    // remove the reference to user who owned this session so that it can be cleaned up
                    this.User = null;
                    // remove the reference to this session from the user
                    user.UpdateCB();
                }
            }
        }

        public static void InitialiseClient(object data, bool SSL)
        {
            Session session = null;
            try
            {
                string ssl = "";
                if (SSL)
                {
                    ssl = "SSL ";
                }
                System.Net.Sockets.TcpClient client = (System.Net.Sockets.TcpClient)data;
                session = new Session();
                SystemLog.WriteLine("Opening a new " + ssl + "connection to " + client.Client.RemoteEndPoint.ToString());
                session.SessionThread = Thread.CurrentThread;
                //session.client = client;
                session.IP = client.Client.RemoteEndPoint.ToString();
                Thread checker = new Thread(ConnectionKiller);
                checker.Name = "watcher";
                session.UsingSSL = SSL;
                session.Connected = true;
                checker.Start(session);

                lock (ConnectedUsers)
                {
                    ConnectedUsers.Add(session);
                }

                if (SSL)
                {
                    X509Certificate cert = new X509Certificate2(Configuration._System.CertificatePath, "pidgeon");
                    System.Net.Security.SslStream _networkSsl = new SslStream(client.GetStream(), false,
                        new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    _networkSsl.AuthenticateAsServer(cert);
                    session._StreamWriter = new StreamWriter(_networkSsl);
                    session._StreamReader = new StreamReader(_networkSsl, Encoding.UTF8);
                }
                else
                {
                    System.Net.Sockets.NetworkStream ns = client.GetStream();
                    session._StreamWriter = new StreamWriter(ns);
                    session._StreamReader = new StreamReader(ns, Encoding.UTF8);
                }

                string text = session._StreamReader.ReadLine();

                session.protocol = new ProtocolMain(session);
                while (session.IsConnected && !session._StreamReader.EndOfStream)
                {
                    try
                    {
                        text = session._StreamReader.ReadLine();
                        if (text == "")
                        {
                            continue;
                        }

                        if (ProtocolMain.Datagram.IsValid(text))
                        {
                            session.protocol.ParseCommand(text);
                            continue;
                        }
                        else
                        {
                            SystemLog.DebugLog("invalid text: " + text + " from " + client.Client.RemoteEndPoint.ToString());
                            System.Threading.Thread.Sleep(800);
                        }
                    }
                    catch (IOException)
                    {
                        SystemLog.WriteLine("Connection closed: " + session.IP);
                        session.Clean();
                        return;
                    }
                    catch (ThreadAbortException)
                    {
                        session.Clean();
                        return;
                    }
                }
                SystemLog.WriteLine("Connection closed by remote: " + session.IP);
                session.Clean();
                return;
            }
            catch (IOException)
            {
                SystemLog.WriteLine("Connection closed: " + session.IP);
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            if (session != null)
            {
                session.Clean();
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

        /// <summary>
        /// Clean this instance.
        /// </summary>
        private void Clean()
        {
            try
            {
                SystemLog.DebugLog("Removing information about session for connection: " + IP);
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
