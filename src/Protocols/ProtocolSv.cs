//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Xml;
using System.Threading;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace pidgeon_sv
{
    /// <summary>
    /// Protocol for pidgeon services
    /// </summary>
    public partial class ProtocolSv
    {   
        private System.Threading.Thread main = null;
        private System.Threading.Thread keep = null;
        private object StreamLock = new object();

        private System.Net.Sockets.NetworkStream networkStream;
        private System.IO.StreamReader streamReader = null;
        private System.IO.StreamWriter streamWriter = null;
        /// <summary>
        /// Password
        /// </summary>
        public string Password = "";
        //private Status ConnectionStatus = Status.WaitingPW;
        private SslStream _networkSsl = null;
        /// <summary>
        /// Whether the services have finished loading
        /// </summary>
        public bool FinishedLoading = false;
        /// <summary>
        /// Nickname
        /// </summary>
        public string Username = "";
        /// <summary>
        /// This needs to be true when the services are in process of disconnecting
        /// </summary>
        private bool disconnecting = false;
        public string Server = "unknown";
        public bool SSL = false;
        public bool Respond = false;
        public int Port = Configuration.Network.ServerPort;
        public bool IsConnected
        {
            get
            {
                return Connected;
            }
        }
        private bool Connected = false;

        private void _Ping()
        {
            try
            {
                while (IsConnected)
                {
                    Deliver(new ProtocolMain.Datagram("PING"));
                    System.Threading.Thread.Sleep(480000);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }

        private void Start()
        {
            try
            {
                SystemLog.WriteLine("Connecting to " + Server);

                if (!SSL)
                {
                    networkStream = new System.Net.Sockets.TcpClient(Server, Port).GetStream();
                    streamWriter = new System.IO.StreamWriter(networkStream);
                    streamReader = new System.IO.StreamReader(networkStream, Encoding.UTF8);
                }

                if (SSL)
                {
                    System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(Server, Port);
                    _networkSsl = new System.Net.Security.SslStream(client.GetStream(), false,
                        new System.Net.Security.RemoteCertificateValidationCallback(Session.ValidateServerCertificate), null);
                    _networkSsl.AuthenticateAsClient(Server);
                    streamWriter = new System.IO.StreamWriter(_networkSsl);
                    streamReader = new System.IO.StreamReader(_networkSsl, Encoding.UTF8);
                }

                Connected = true;

                Deliver(new ProtocolMain.Datagram("PING"));
                Deliver(new ProtocolMain.Datagram("LOAD"));
                ProtocolMain.Datagram login = new ProtocolMain.Datagram("AUTH", "");
                login.Parameters.Add("user", Username);
                login.Parameters.Add("pw", Password);
                Deliver(login);
                Deliver(new ProtocolMain.Datagram("GLOBALNICK"));
                Deliver(new ProtocolMain.Datagram("NETWORKLIST"));
                Deliver(new ProtocolMain.Datagram("STATUS"));
                keep = new System.Threading.Thread(_Ping);
                keep.Name = "pinger thread";
                keep.Start();
            }
            catch (System.Threading.ThreadAbortException)
            {
                return;
            }
            catch (Exception b)
            {
                SystemLog.Error(b.Message);
                return;
            }
            string text = "";
            try
            {
                while (!streamReader.EndOfStream && Connected)
                {
                    text = streamReader.ReadLine();
                    if (Valid(text))
                    {
                        // if this return false the thread must be stopped now
                        if (!Process(text))
                        {
                            return;
                        }
                        continue;
                    }
                }
            }
            catch (System.IO.IOException fail)
            {
                if (IsConnected && !disconnecting)
                {
                    // we need to wrap this in another exception handler because the 
                    // following functions are easy to throw some
                    try
                    {
                        SystemLog.Warning("Quit: " + fail.Message);
                        Disconnect();
                        keep.Abort();
                        return;
                    }
                    catch (Exception f1)
                    {
                        Core.handleException(f1);
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                if (IsConnected)
                {
                    Core.handleException(fail);
                }
            }
        }

        private void ReleaseNetwork()
        {
            if (streamReader != null)
            {
                streamReader.Dispose();
                streamReader = null;
            }
            if (_networkSsl != null)
            {
                _networkSsl.Dispose();
                _networkSsl = null;
            }
            if (streamWriter != null)
            {
                streamWriter.Dispose();
                streamWriter = null;
            }
        }

        private void SendData(string network, string data)
        {
            ProtocolMain.Datagram line = new ProtocolMain.Datagram("RAW", data);
            line.Parameters.Add("network", network);
            line.Parameters.Add("priority", "High");
            Deliver(line);
        }

        private bool Process(string dg)
        {
            try
            {
                System.Xml.XmlDocument datagram = new System.Xml.XmlDocument();
                datagram.LoadXml(dg);
                foreach (XmlNode curr in datagram.ChildNodes)
                {
                    switch (curr.Name.ToUpper())
                    {
                        case "SLOAD":
                            ResponsesSv.sLoad(curr, this);
                            break;
                        case "SSTATUS":
                            ResponsesSv.sStatus(curr, this);
                            break;
                        case "SAUTH":
                            ResponsesSv.sAuth(curr, this);
                            break;
                        case "SSYSTEM":
                            ResponsesSv.sMaintenance(curr, this);
                            break;
                        case "SFAIL":
                            ResponsesSv.sError(curr, this);
                            break;
                        case "SDEBUG":
                            ResponsesSv.sDebug(curr, this);
                            break;
                        case "SUSERLIST":
                            ResponsesSv.sUserList(curr, this);
                            break;
                    }
                }
            }
            catch (System.Xml.XmlException xx)
            {
                SystemLog.DebugLog("Unable to parse: " + xx.ToString());
            }
            catch (ThreadAbortException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will close the protocol, that mean it will release all objects and memory, you should only call it when you want to remove this
        /// </summary>
        public void Exit()
        {
            lock (this)
            {
                if (IsConnected)
                {
                    Disconnect();
                }
                disconnecting = true;

                if (streamWriter != null) streamWriter.Close();
                if (streamReader != null) streamReader.Close();
                streamWriter = null;
                streamReader = null;
                this.main.Abort();
                this.keep.Abort();
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            lock (this)
            {
                disconnecting = true;
                if (!IsConnected)
                {
                    SystemLog.DebugLog("User attempted to disconnect services that are already disconnected");
                    disconnecting = false;
                    return false;
                }
                try
                {
                    if (streamWriter != null) streamWriter.Close();
                    if (streamReader != null) streamReader.Close();
                    if (SSL)
                    {
                        if (_networkSsl != null)
                        {
                            _networkSsl.Close();
                        }
                    }
                    else
                    {
                        if (networkStream != null)
                        {
                            networkStream.Close();
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException fail)
                {
                    SystemLog.DebugLog("Problem when disconnecting from network " + Server + ": " + fail.ToString());
                }
                ReleaseNetwork();
                Connected = false;
                disconnecting = false;
            }
            return true;
        }

        /// <summary>
        /// Send a datagram to server
        /// </summary>
        /// <param name="message"></param>
        public void Deliver(ProtocolMain.Datagram message)
        {
            Send(message.ToDocumentXmlText());
        }

        /// <summary>
        /// Open
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            main = new System.Threading.Thread(Start);
            main.Start();
            return true;
        }

        /// <summary>
        /// Check if it's a valid data
        /// </summary>
        /// <param name="datagram"></param>
        /// <returns></returns>
        public static bool Valid(string datagram)
        {
            if (datagram.StartsWith("<", StringComparison.Ordinal) && datagram.EndsWith(">", StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }

        private void SafeDc(string reason)
        {
            try
            {
                Disconnect();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }

        /// <summary>
        /// Write raw data
        /// </summary>
        /// <param name="text"></param>
        public void Send(string text)
        {
            if (IsConnected)
            {
                try
                {
                    lock (StreamLock)
                    {
                        streamWriter.WriteLine(text);
                        streamWriter.Flush();
                    }
                }
                catch (System.IO.IOException er)
                {
                    SafeDc(er.Message);
                }
                catch (Exception f)
                {
                    if (IsConnected)
                    {
                        Core.handleException(f);
                    }
                    else
                    {
                        SystemLog.DebugLog("ex " + f.ToString());
                    }
                }
            }
            else
            {
                SystemLog.DebugLog("ERROR: Can't send a datagram because connection to " + Server + " is closed");
            }
        }

        /// <summary>
        /// Current status of services
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// Waiting for a password
            /// </summary>
            WaitingPW,
            /// <summary>
            /// Everything work
            /// </summary>
            Connected,
        }
    }
}
