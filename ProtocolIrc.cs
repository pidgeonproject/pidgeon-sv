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
using System.Text;
using System.Net;
using System.Threading;
using System.Xml;

namespace pidgeon_sv
{
    public class ProtocolIrc : Protocol
    {
        public enum Priority
        {
            High = 8,
            Normal = 2,
            Low = 1
        }

        public class MessageOrigin
        {
            public string text = null;
            public DateTime time;
        }

        private System.Net.Sockets.NetworkStream _network;
        private System.IO.StreamReader _reader;

        public Network _server;
        private System.IO.StreamWriter _writer;


        Messages _messages = new Messages();

        public List<MessageOrigin> MessageBuffer = new List<MessageOrigin>();


        public System.Threading.Thread main;
        public System.Threading.Thread deliveryqueue;
        public System.Threading.Thread keep;
        public System.Threading.Thread th;


        public Buffer buffer = null;
        public DateTime pong = DateTime.Now;


        public class Buffer
        {
            public Account parent = null;
            public string Network = null;

            public Buffer(Account _account, string server)
            {
                Network = server;
                parent = _account;
            }

            public class Message
            {
                public Priority _Priority;
                public DateTime time;
                public ProtocolMain.Datagram message;
                public Message()
                {
                    time = DateTime.Now;
                }

                public string ToDocumentXmlText()
                {
                    XmlDocument datagram = new XmlDocument();
                    XmlNode b1 = datagram.CreateElement("message");
                    Dictionary<string, string> Parameters = new Dictionary<string, string>();
                    Parameters.Add("priority", _Priority.ToString());
                    Parameters.Add("time", time.ToBinary().ToString());
                    foreach (KeyValuePair<string, string> curr in Parameters)
                    {
                        XmlAttribute b2 = datagram.CreateAttribute(curr.Key);
                        b2.Value = curr.Value;
                        b1.Attributes.Append(b2);
                    }
                    b1.InnerText = message.ToDocumentXmlText();
                    datagram.AppendChild(b1);
                    return datagram.InnerXml;
                }
            }

            public List<Message> messages = new List<Message>();
            public List<Message> oldmessages = new List<Message>();
            public ProtocolIrc protocol;

            public void DeliverMessage(ProtocolMain.Datagram Message, Priority Pr = Priority.Normal)
            {
                Message text = new Message();
                text._Priority = Pr;
                text.message = Message;
                lock (messages)
                {
                    messages.Add(text);
                    return;
                }
            }

            public void Run()
            {
                try
                {
                    while (true)
                    {
                        try
                        {
                            if (messages.Count == 0)
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                            List<Message> newmessages = new List<Message>();
                            if (messages.Count > 0)
                            {
                                lock (messages)
                                {
                                    newmessages.AddRange(messages);
                                    messages.Clear();
                                }
                            }
                            if (newmessages.Count > 0)
                            {
                                if (protocol.owner != null)
                                {
                                    foreach (Message message in newmessages)
                                    {
                                        message.message.Parameters.Add("time", message.time.ToBinary().ToString());
                                        protocol.owner.Deliver(message.message);
                                    }
                                }

                                if (oldmessages.Count > Config.maxbs)
                                {
                                    FlushOld();
                                }
                                lock (oldmessages)
                                {
                                    oldmessages.AddRange(newmessages);
                                }
                            }
                            newmessages.Clear();
                            System.Threading.Thread.Sleep(200);
                        }
                        catch (System.Threading.ThreadAbortException)
                        {
                            return;
                        }
                    }
                }
                catch (Exception fail)
                {
                    Core.handleException(fail, true);
                }
            }

            public void FlushOld()
            {
                int Count = 0;
                lock (oldmessages)
                {
                    while (oldmessages.Count > Config.minbs)
                    {
                        Count++;
                        parent.data.MessagePool_InsertData(oldmessages[0], Network);
                        oldmessages.RemoveAt(0);
                    }
                }
                Core.DebugLog("Stored " + Count.ToString());
            }
        }

        class Messages
        {
            public struct Message
            {
                public Priority _Priority;
                public string message;
            }
            public List<Message> messages = new List<Message>();
            public List<Message> newmessages = new List<Message>();
            public ProtocolIrc protocol;

            public void DeliverMessage(string Message, Priority Pr = Priority.Normal)
            {
                Message text = new Message();
                text._Priority = Pr;
                text.message = Message;
                lock (messages)
                {
                    messages.Add(text);
                    return;
                }
            }

            public void Run()
            {
                while (true)
                {
                    try
                    {
                        if (messages.Count > 0)
                        {
                            lock (messages)
                            {
                                newmessages.AddRange(messages);
                                messages.Clear();
                            }
                        }
                        if (newmessages.Count > 0)
                        {
                            List<Message> Processed = new List<Message>();
                            Priority highest = Priority.Low;
                            while (newmessages.Count > 0)
                            {
                                // we need to get all messages that have been scheduled to be send
                                lock (messages)
                                {
                                    if (messages.Count > 0)
                                    {
                                        newmessages.AddRange(messages);
                                        messages.Clear();
                                    }
                                }
                                highest = Priority.Low;
                                // we need to check the priority we need to handle first
                                foreach (Message message in newmessages)
                                {
                                    if (message._Priority > highest)
                                    {
                                        highest = message._Priority;
                                        if (message._Priority == Priority.High)
                                        {
                                            break;
                                        }
                                    }
                                }
                                // send highest priority first
                                foreach (Message message in newmessages)
                                {
                                    if (message._Priority >= highest)
                                    {
                                        Processed.Add(message);
                                        protocol.Send(message.message);
                                        System.Threading.Thread.Sleep(1000);
                                        if (highest != Priority.High)
                                        {
                                            break;
                                        }
                                    }
                                }
                                foreach (Message message in Processed)
                                {
                                    if (newmessages.Contains(message))
                                    {
                                        newmessages.Remove(message);
                                    }
                                }
                            }
                        }
                        newmessages.Clear();
                        System.Threading.Thread.Sleep(200);
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                        return;
                    }
                }
            }
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
                _network = new System.Net.Sockets.TcpClient(Server, Port).GetStream();
                _server.Connected = true;

                _writer = new System.IO.StreamWriter(_network);
                _reader = new System.IO.StreamReader(_network, Encoding.UTF8);


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
            catch (System.Threading.ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }

        public void ClientData(string content)
        {
            ProtocolMain.Datagram dt = new ProtocolMain.Datagram("DATA", content);
            dt.Parameters.Add("network", Server);
            buffer.DeliverMessage(dt);
        }

        public void getDepth(int n, ProtocolMain user)
        {
            try
            {
                Core.DebugLog("User " + owner.nickname + " requested a backlog of data");
                int i = 0;
				int MQID = 0;
                user.TrafficChunks = true;
                int total_count = 0;
                total_count = n;
                int index = 0;
                lock (buffer.oldmessages)
                {
                    if (buffer.oldmessages.Count == 0)
                    {
                        Core.DebugLog("User " + owner.nickname + " requested a backlog, there are no data");
                        ProtocolMain.Datagram size = new ProtocolMain.Datagram("BACKLOG", "0");
                        size.Parameters.Add("network", Server);
                        user.Deliver(size);
                        return;
                    }
                    if (buffer.oldmessages.Count < n)
                    {
                        Core.DebugLog("User " + owner.nickname + " requested a backlog of " + n.ToString() + " datagrams, but there are not so many in memory as they requested, recovering some from storage");
                        if (buffer.oldmessages.Count + owner.data.GetMessageSize(Server) < n)
                        {
                            total_count = buffer.oldmessages.Count + owner.data.GetMessageSize(Server);
                            Core.DebugLog("User " + owner.nickname + " requested a backlog of " + n.ToString() + " datagrams, but there are not so many in memory neither in the storage in total only " + total_count.ToString() + " right now :o");
                        }
                        ProtocolMain.Datagram count = new ProtocolMain.Datagram("BACKLOG", total_count.ToString());
                        count.Parameters.Add("network", Server);
                        user.Deliver(count);
                        owner.data.MessagePool_DeliverData(total_count - buffer.oldmessages.Count, ref index, user, Server, ref MQID);
                        if (index < 0)
                        {
                            Core.DebugLog("Something went wrong");
                            return;
                        }
                        n = buffer.oldmessages.Count;
                    }
                    else
                    {
                        ProtocolMain.Datagram count = new ProtocolMain.Datagram("BACKLOG", n.ToString());
                        count.Parameters.Add("network", Server);
                        user.Deliver(count);
                    }

                    while (i < n)
                    {
                        ProtocolMain.Datagram text = new ProtocolMain.Datagram(buffer.oldmessages[i].message._Datagram);
                        text._InnerText = buffer.oldmessages[i].message._InnerText;
                        foreach (KeyValuePair<string, string> current in buffer.oldmessages[i].message.Parameters)
                        {
                            text.Parameters.Add(current.Key, current.Value);
                        }
                        text.Parameters.Add("buffer", index.ToString());
						text.Parameters.Add("MQID", MQID.ToString());
                        user.Deliver(text);
                        i = i + 1;
                        index++;
						MQID++;

                    }
                    user.TrafficChunks = false;
                    Core.DebugLog("User " + owner.nickname + " messages " + MessageBuffer.Count.ToString());
                    foreach (MessageOrigin d in MessageBuffer)
                    {
                        ProtocolMain.Datagram text = new ProtocolMain.Datagram("SELFDG");
                        text._InnerText = d.text;
                        text.Parameters.Add("network", Server);
                        text.Parameters.Add("buffer", i.ToString());
                        text.Parameters.Add("time", d.time.ToBinary().ToString());
						text.Parameters.Add("MQID", MQID.ToString());
						MQID++;
                        i++;
                        user.Deliver(text);
                    }
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                user.TrafficChunks = false;
            }
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
                _writer.WriteLine(ms);
                _writer.Flush();
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

        /// <summary>
        /// /me style
        /// </summary>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="_priority"></param>
        /// <returns></returns>
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

        public override void Exit()
        {
            if (!_server.Connected)
            {
                return;
            }
            try
            {
                _writer.WriteLine("QUIT :" + _server.quit);
                _writer.Flush();
            }
            catch (Exception) { }
            _server.Connected = false;
            System.Threading.Thread.Sleep(200);
            deliveryqueue.Abort();
            keep.Abort();
            if (main.ThreadState == System.Threading.ThreadState.Running)
            {
                main.Abort();
            }
            return;
        }

        public override bool Open()
        {
            main = new System.Threading.Thread(Start);
            main.Start();
            buffer.protocol = this;
            th = new System.Threading.Thread(buffer.Run);
            th.Start();
            return true;
        }
    }
}
