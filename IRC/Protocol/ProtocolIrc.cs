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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace pidgeon_sv
{
    public partial class ProtocolIrc : Protocol
    {
        private System.Net.Sockets.NetworkStream _network;
        private System.IO.StreamReader _reader;
        public Network _server;
        private System.IO.StreamWriter _writer;
        private SslStream _networkSsl;
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

        public string convertUNIX(string time)
        {
            long baseTicks = 621355968000000000;
            long tickResolution = 10000000;

            long epoch = (DateTime.Now.ToUniversalTime().Ticks - baseTicks) / tickResolution;
            long epochTicks = (epoch * tickResolution) + baseTicks;
            return new DateTime(epochTicks, DateTimeKind.Utc).ToString();
        }

        public void _Ping()
        {
            try
            {
                while (_server.Connected)
                {
                    Transfer("PING :" + _server._protocol.Server, Priority.High);
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

        public void Start()
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

                _writer.WriteLine("USER " + _server.ident + " 8 * :" + _server.username);
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

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public void ClientData(string content)
        {
            ProtocolMain.Datagram dt = new ProtocolMain.Datagram("DATA", content);
            dt.Parameters.Add("network", Server);
            buffer.DeliverMessage(dt);
        }

        /// <summary>
        /// Get a size of backlog that starts from given id and has a specific maximal size
        /// </summary>
        /// <param name="mqid"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public int getBacklogSize(int mqid, int size)
        {
            // check memory first (quickly)
            Core.DebugLog("Retrieving size of backlog for " + Server);
            DateTime start_time = DateTime.Now;
            bool FoundNewer = false;
            int backlog_size = 0;
            lock (buffer.oldmessages)
            {
                foreach (Buffer.Message message in buffer.oldmessages)
                {
                    if (int.Parse(message.message.Parameters["MQID"]) > mqid)
                    {
                        FoundNewer = true;
                        backlog_size++;
                    }
                }
            }
            if (!FoundNewer)
            {
                Core.DebugLog("No backlog data");
                return 0;
            }
            // now search the disk
            backlog_size = backlog_size + owner.data.MessagePool_Backlog(size, mqid, Server);
            Core.DebugLog("Parsed size " + backlog_size.ToString() + " in " + (DateTime.Now - start_time).ToString());
            return backlog_size;
        }

        public void getDepth(int RequestedSize, ProtocolMain user, int mqid)
        {
            try
            {
                Core.DebugLog("User " + owner.nickname + " requested a backlog of data starting from " + mqid.ToString());
                user.TrafficChunks = true;
                int total_count = RequestedSize;
                int total_requested_size = RequestedSize;
                int index = 0;
                int backlog_size = 0;
                lock (buffer.oldmessages)
                {
                    if (buffer.oldmessages.Count == 0)
                    {
                        // we don't need to deliver any backlog
                        Core.DebugLog("User " + owner.nickname + " requested a backlog, there are no data");
                        ProtocolMain.Datagram size = new ProtocolMain.Datagram("BACKLOG", "0");
                        size.Parameters.Add("network", Server);
                        user.Deliver(size);
                        return;
                    }
                    if (buffer.oldmessages.Count < RequestedSize)
                    {
                        // the backlog needs to be parsed from file
                        Core.DebugLog("User " + owner.nickname + " requested a backlog of " + RequestedSize.ToString() + " datagrams, but there are not so many in memory as they requested, recovering some from storage");
                        // we get the total size of memory and disk
                        total_count = buffer.oldmessages.Count + owner.data.GetMessageSize(Server);
                        if (total_count < RequestedSize)
                        {
                            Core.DebugLog("User " + owner.nickname + " requested a backlog of " + RequestedSize.ToString() + " datagrams, but there are not so many in memory neither in the storage in total only " + total_count.ToString() + " right now :o");
                        }
                        // we get a backlog size in case that user has some mqid
                        if (mqid > 0)
                        {
                            backlog_size = getBacklogSize(mqid, RequestedSize);
                        }
                        else
                        {
                            backlog_size = total_count;
                        }

                        // in case that user should get more messages than he requested we fix it
                        if (backlog_size > total_requested_size)
                        {
                            backlog_size = total_requested_size;
                        }
                        Core.DebugLog("Delivering backlog messages to peer: " + backlog_size.ToString());
                        ProtocolMain.Datagram count = new ProtocolMain.Datagram("BACKLOG", backlog_size.ToString());
                        count.Parameters.Add("network", Server);
                        user.Deliver(count);
                        // we send the data using the storage
                        owner.data.MessagePool_DeliverData(total_count - buffer.oldmessages.Count, ref index, user, Server, mqid);
                        if (index < 0)
                        {
                            // this makes no sense, the datafile was probably corrupted
                            Core.DebugLog("Something went wrong");
                            return;
                        }
                        backlog_size = buffer.oldmessages.Count;
                    }
                    else
                    {
                        // backlog doesn't need to be parsed from file
                        backlog_size = getBacklogSize(mqid, RequestedSize);
                        if (backlog_size > total_requested_size)
                        {
                            backlog_size = total_requested_size;
                        }
                        
                        ProtocolMain.Datagram count = new ProtocolMain.Datagram("BACKLOG", backlog_size.ToString());
                        count.Parameters.Add("network", Server);
                        user.Deliver(count);
                    }

                    int i = 0;
                    // now we need to deliver the remaining data from memory
                    if (backlog_size > buffer.oldmessages.Count)
                    {
                        Core.DebugLog("For some reason the backlog size was bigger than number of all messages in memory");
                        backlog_size = buffer.oldmessages.Count;
                    }

                    while (i < backlog_size)
                    {
                        if (int.Parse(buffer.oldmessages[i].message.Parameters["MQID"]) > mqid)
                        {
                            ProtocolMain.Datagram text = new ProtocolMain.Datagram(buffer.oldmessages[i].message._Datagram);
                            text._InnerText = buffer.oldmessages[i].message._InnerText;
                            foreach (KeyValuePair<string, string> current in buffer.oldmessages[i].message.Parameters)
                            {
                                text.Parameters.Add(current.Key, current.Value);
                            }
                            index++;
                            text.Parameters.Add("buffer", index.ToString());
                            user.Deliver(text);
                        }
                        i++;
                    }
                    user.TrafficChunks = false;
                    user.Deliver(new ProtocolMain.Datagram("PING"));    
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                user.TrafficChunks = false;
            }
        }

        public void getRange(ProtocolMain user, int from, int last)
        {
            Core.DebugLog("User " + owner.nickname + " requested a range of data starting from " + from.ToString());
            int index = 0;
            lock (buffer.oldmessages)
            {
                foreach (Buffer.Message curr in buffer.oldmessages)
                {
                    int mq =int.Parse(curr.message.Parameters["MQID"]);
                    if (from >= mq && last <= mq)
                    {
                        ProtocolMain.Datagram text = new ProtocolMain.Datagram(curr.message._Datagram);
                        text._InnerText = curr.message._InnerText;
                        foreach (KeyValuePair<string, string> current in curr.message.Parameters)
                        {
                            text.Parameters.Add(current.Key, current.Value);
                        }
                        text.Parameters.Add("range", index.ToString());
                        index++;
                        user.Deliver(text);
                    }
                }
            }

            owner.data.MessagePool_Range(from, last, Server, ref index, user);
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
                _writer.WriteLine("QUIT :" + _server.quit);
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
            deliveryqueue.Abort();
            keep.Abort();
            if (Thread.CurrentThread != main && (main.ThreadState == System.Threading.ThreadState.Running || main.ThreadState == ThreadState.WaitSleepJoin))
            {
                main.Abort();
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
