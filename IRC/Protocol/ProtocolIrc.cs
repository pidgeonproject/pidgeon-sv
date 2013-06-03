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
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace pidgeon_sv
{
    public partial class ProtocolIrc : Protocol
    {
        private System.Net.Sockets.NetworkStream _network = null;
        private System.IO.StreamReader _reader = null;
        public Network _server;
        private System.IO.StreamWriter _writer = null;
        private SslStream _networkSsl = null;
        private Messages _messages = new Messages();
        public System.Threading.Thread main = null;
        public System.Threading.Thread deliveryqueue = null;
        public System.Threading.Thread keep = null;
        public System.Threading.Thread BufferTh = null;
        public Buffer buffer = null;
        public DateTime pong = DateTime.Now;
        private bool destroyed = false;

        public bool IsConnected
        {
            get
            {
                if (_server != null)
                {
                    return (_server.Connected);
                }
                return false;
            }
        }

        public enum Priority
        {
            High = 8,
            Normal = 2,
            Low = 1
        }  

        public override void Part(string name, Network network = null)
        {
            Transfer("PART " + name);
        }

        public override void Transfer(string text, Priority Pr = Priority.Normal)
        {
            _messages.DeliverMessage(text, Pr);
        }

        private void _Ping()
        {
            try
            {
                while (_server.Connected)
                {
                    Transfer("PING :" + _server._Protocol.Server, Priority.High);
                    System.Threading.Thread.Sleep(24000);
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

        private void Start()
        {
            _messages.protocol = this;
            try
            {
                _server.Connected = true;
                if (!SSL)
                {
                    _network = new System.Net.Sockets.TcpClient(Server, Port).GetStream();
                    _writer = new System.IO.StreamWriter(_network);
                    _reader = new System.IO.StreamReader(_network, Encoding.UTF8);
                }

                if (SSL)
                {
                    System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(Server, Port);
                    _networkSsl = new System.Net.Security.SslStream(client.GetStream(), true,
                        new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    _networkSsl.AuthenticateAsClient(Server);
                    _writer = new System.IO.StreamWriter(_networkSsl);
                    _reader = new System.IO.StreamReader(_networkSsl, Encoding.UTF8);
                }

                _writer.WriteLine("USER " + _server.Ident + " 8 * :" + _server.UserName);
                _writer.WriteLine("NICK " + _server.nickname);
                _writer.Flush();

                keep = new System.Threading.Thread(_Ping);
                keep.Name = "pinger thread";
                keep.Start();

            }
            catch (ThreadAbortException)
            {
                // shutting down
                return; 
            }
            catch (Exception b)
            {
                ProtocolMain.Datagram dt = new ProtocolMain.Datagram("CONNECTION", "PROBLEM");
                dt.Parameters.Add("network", Server);
                dt.Parameters.Add("info", b.Message);
                owner.Deliver(dt);
                Console.WriteLine(b.Message);
                return;
            }
            string text = "";
            try
            {
                deliveryqueue = new System.Threading.Thread(_messages.Run);
                deliveryqueue.Start();

                while (_server.Connected && !_reader.EndOfStream)
                {
                    text = _reader.ReadLine();
                    ProcessorIRC parser = new ProcessorIRC(_server, text, ref pong);
                    parser.Result();
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                ProtocolMain.Datagram dt = new ProtocolMain.Datagram("CONNECTION", "PROBLEM");
                dt.Parameters.Add("network", Server);
                dt.Parameters.Add("info", fail.Message);
                owner.Deliver(dt);
                Core.handleException(fail);
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public override bool Command(string cm)
        {
            try
            {
                if (cm.StartsWith(" ") != true && cm.Contains(" "))
                {
                    // uppercase
                    string first_word = cm.Substring(0, cm.IndexOf(" ")).ToUpper();
                    string rest = cm.Substring(first_word.Length);
                    _writer.WriteLine(first_word + rest);
                    _writer.Flush();
                    return true;
                }
                _writer.WriteLine(cm.ToUpper());
                _writer.Flush();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            return false;
        }

        private void Send(string ms)
        {
            try
            {
                if (IsConnected)
                {
                    _writer.WriteLine(ms);
                    _writer.Flush();
                }
            }
            catch (IOException fail)
            {
                Core.DebugLog("Error: connection " + Server + " was closed: " + fail.ToString());
                Disconnect();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }

        public override int Message(string text, string to, Priority _priority = Priority.Normal)
        {
            Transfer("PRIVMSG " + to + " :" + text, _priority);
            return 0;
        }

        public override int Message2(string text, string to, Priority _priority = Priority.Normal)
        {
            Transfer("PRIVMSG " + to + " :" + delimiter.ToString() + "ACTION " + text + delimiter.ToString(), _priority);
            return 0;
        }

        public override void Join(string name, Network network = null)
        {
            Transfer("JOIN " + name);
        }

        public override int requestNick(string _Nick)
        {
            Transfer("NICK " + _Nick);
            return 0;
        }

        public void ClearBuffers()
        {
            Core.DebugLog("Removing all buffers for " + Server);
            lock (buffer)
            {
                owner.data.DeleteCache(Server);
                buffer.oldmessages.Clear();
                buffer.messages.Clear();
            }
            lock (owner.Messages)
            {
                List<ProtocolMain.SelfData> delete = new List<ProtocolMain.SelfData>();
                foreach (ProtocolMain.SelfData ms in owner.Messages)
                {
                    if (ms.network == _server)
                    {
                        delete.Add(ms);
                    }
                }
                foreach (ProtocolMain.SelfData ms in delete)
                {
                    owner.Messages.Remove(ms);
                }
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }
            if (!_server.Connected)
            {
                return;
            }
            try
            {
                Core.DisableThread(keep);
                _writer.WriteLine("QUIT :" + _server.Quit);
                _writer.Flush();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            _server.Connected = false;
        }
        
        public override void Exit()
        {
            if (destroyed)
            {
                Core.DebugLog("This network was already destroyed");
                return;
            }
            ClearBuffers();
            destroyed = true;
            Disconnect();
            Core.DisableThread(deliveryqueue);
            Core.DisableThread(keep);
            if (Thread.CurrentThread != main)
            {
                Core.DisableThread(main);
            }
            _server.Destroy();
            return;
        }

        public override bool Open()
        {
            main = new System.Threading.Thread(Start);
            main.Start();
            buffer.protocol = this;
            BufferTh = new System.Threading.Thread(buffer.Run);
            BufferTh.Start();
            return true;
        }
    }
}
