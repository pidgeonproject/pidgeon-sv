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
            catch (Exception)
            { }
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
                    /* if (text.StartsWith(":"))
                    {
                        string[] data = text.Split(':');
                        if (data.Length > 1)
                        {
                            string command = "";
                            string parameters = "";
                            string command2 = "";
                            string source;
                            string _value;
                            source = text.Substring(1);
                            source = source.Substring(0, source.IndexOf(" "));
                            command2 = text.Substring(1);
                            command2 = command2.Substring(source.Length + 1);
                            if (command2.Contains(" :"))
                            {
                                command2 = command2.Substring(0, command2.IndexOf(" :"));
                            }
                            string[] _command = command2.Split(' ');
                            if (_command.Length > 0)
                            {
                                command = _command[0];
                            }
                            if (_command.Length > 1)
                            {
                                int curr = 1;
                                while (curr < _command.Length)
                                {
                                    parameters += _command[curr] + " ";
                                    curr++;
                                }
                                if (parameters.EndsWith(" "))
                                {
                                    parameters = parameters.Substring(0, parameters.Length - 1);
                                }
                            }
                            _value = "";
                            if (text.Length > 3 + command2.Length + source.Length)
                            {
                                _value = text.Substring(3 + command2.Length + source.Length);
                            }
                            if (_value.StartsWith(":"))
                            {
                                _value = _value.Substring(1);
                            }

                            if (command == "PONG")
                            {
                                pong = DateTime.Now;
                            }

                            if (data[1].Contains(" "))
                            {
                                string[] code = data[1].Split(' ');
                                switch (command)
                                {
                                    case "1":
                                    case "2":
                                    case "3":
                                    case "4":
                                    case "5":
                                        ProtocolMain.Datagram p = new ProtocolMain.Datagram("DATA", text);
                                        //p.Parameters.Add("network", Server);
                                        info.Add(p);
                                        break;
                                    case "313":
                                    //whois
                                    case "318":
                                        break;
                                    case "332":
                                        if (code.Length > 3)
                                        {
                                            string name = "";
                                            if (parameters.Contains("#"))
                                            {
                                                name = parameters.Substring(parameters.IndexOf("#")).Replace(" ", "");
                                            }
                                            string topic = _value;
                                            Channel channel = _server.getChannel(name);
                                            if (channel != null)
                                            {
                                                channel.Topic = topic;

                                            }
                                        }
                                        break;

                                    case "352":
                                        // cameron.freenode.net 352 petan2 #debian thelineva nikita.tnnet.fi kornbluth.freenode.net t0h H :0 Tommi Helineva
                                        if (code.Length > 6)
                                        {
                                            Channel channel = _server.getChannel(code[3]);
                                            string ident = code[4];
                                            string host = code[5];
                                            string nick = code[7];
                                            string server = code[6];
                                            if (channel != null)
                                            {
                                                if (!channel.containsUser(nick))
                                                {
                                                    channel.UserList.Add(new User(nick, host, _server, ident));
                                                    break;
                                                }
                                                foreach (User u in channel.UserList)
                                                {
                                                    if (u.Nick == nick)
                                                    {
                                                        u.Ident = ident;
                                                        u.Host = host;

                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case "353":
                                        if (code.Length > 3)
                                        {
                                            string name = code[4];
                                            Channel channel = _server.getChannel(name);
                                            if (channel != null)
                                            {
                                                string[] _chan = data[2].Split(' ');
                                                foreach (var user in _chan)
                                                {
                                                    if (!channel.containsUser(user) && user != "")
                                                    {
                                                        lock (channel.UserList)
                                                        {
                                                            channel.UserList.Add(new User(user, "", _server, ""));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case "005":
                                        // PREFIX=(qaohv)~&@%+ CHANMODES=beI,k
                                        if (parameters.Contains("PREFIX=("))
                                        {

                                        }
                                        if (parameters.Contains("CHANMODES="))
                                        {
                                            string xmodes = parameters.Substring(parameters.IndexOf("CHANMODES=") + 11);
                                        }
                                        break;
                                    case "366":
                                        break; ;
                                    case "324":
                                        if (code.Length > 3)
                                        {
                                            string name = code[2];
                                            string topic = _value;
                                            //Channel channel = _server.getChannel(code[3]);

                                        }
                                        break;
                                    //  367 petan # *!*@173.45.238.81
                                    case "367":
                                        if (code.Length > 6)
                                        {
                                            string chan = code[3];
                                            Channel channel = _server.getChannel(code[3]);
                                            if (channel != null)
                                            {
                                                if (channel.Bl == null)
                                                {
                                                    channel.Bl = new List<SimpleBan>();
                                                }
                                                if (!channel.containsBan(code[4]))
                                                {
                                                    channel.Bl.Add(new SimpleBan(code[5], code[4], code[6]));
                                                }
                                            }
                                        }
                                        break;
                                    case "556":
                                        break;
                                }
                            }
                            if (command == "INFO")
                            {

                            }

                            if (command == "NOTICE")
                            {

                            }
                            if (source.StartsWith(_server.nickname + "!"))
                            {
                                string[] _data2 = data[1].Split(' ');
                                if (_data2.Length > 2)
                                {
                                    if (_data2[1].Contains("JOIN"))
                                    {
                                        string channel = _data2[2];
                                        if (_data2[2].Contains("#") == false)
                                        {
                                            channel = data[2];
                                        }
                                        Channel curr = _server.Join(channel);
                                    }
                                }
                                if (_data2.Length > 2)
                                {
                                    if (_data2[1].Contains("NICK"))
                                    {
                                        _server.nickname = _value;
                                    }
                                    if (_data2[1].Contains("PART"))
                                    {
                                        string channel = _data2[2];
                                        if (_data2[2].Contains("#") == false)
                                        {
                                            channel = data[2];
                                            Channel c = _server.getChannel(channel);
                                            if (c != null)
                                            {
                                                _server.Channels.Remove(c);
                                                c.ok = false;
                                            }
                                        }
                                    }
                                }
                            }

                            if (command == "PING")
                            {
                                Transfer("PONG ", Priority.High);
                            }

                            if (command == "NICK")
                            {
                                string nick = source.Substring(0, source.IndexOf("!"));
                                string _new = _value;
                                foreach (Channel item in _server.Channels)
                                {
                                    if (item.ok)
                                    {
                                        lock (item.UserList)
                                        {
                                            foreach (User curr in item.UserList)
                                            {
                                                if (curr.Nick == nick)
                                                {
                                                    curr.Nick = _new;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (command == "PRIVMSG")
                            {
                                string _nick;
                                string _ident;
                                string _host;
                                string chan;
                                _nick = source.Substring(0, source.IndexOf("!"));
                                _host = source.Substring(source.IndexOf("@") + 1);
                                _ident = source.Substring(source.IndexOf("!") + 1);
                                _ident = _ident.Substring(0, _ident.IndexOf("@"));
                                chan = parameters.Replace(" ", "");
                                string message = _value;
                                if (!chan.Contains(_server.channel_prefix))
                                {
                                    owner.Deliver(new ProtocolMain.Datagram("CTCP", message));
                                }
                                User user = new User(_nick, _host, _server, _ident);
                                Channel channel = null;
                                if (chan.StartsWith(_server.channel_prefix))
                                {
                                    channel = _server.getChannel(chan);
                                }
                                chan = source.Substring(source.IndexOf("!"));

                            }

                            if (command == "TOPIC")
                            {
                                string chan = parameters;
                                chan = chan.Replace(" ", "");
                                string user = source.Substring(0, source.IndexOf("!"));
                                Channel channel = _server.getChannel(chan);
                                if (channel != null)
                                {
                                    channel.Topic = _value;
                                }
                            }

                            if (command == "MODE")
                            {
                                if (parameters.Contains(" "))
                                {
                                    string chan = parameters.Substring(0, parameters.IndexOf(" "));
                                    chan = chan.Replace(" ", "");
                                    string user = source;
                                    if (chan.StartsWith(_server.channel_prefix))
                                    {
                                        Channel channel = _server.getChannel(chan);
                                        if (channel != null)
                                        {

                                            string change = parameters.Substring(parameters.IndexOf(" "));


                                            while (change.StartsWith(" "))
                                            {
                                                change = change.Substring(1);
                                            }

                                            channel._mode.mode(change);

                                            while (change.EndsWith(" ") && change.Length > 1)
                                            {
                                                change = change.Substring(0, change.Length - 1);
                                            }

                                            if (change.Contains(" "))
                                            {
                                                string header = change.Substring(0, change.IndexOf(" "));
                                                List<string> parameters2 = new List<string>();
                                                parameters2.AddRange(change.Substring(change.IndexOf(" ") + 1).Split(' '));
                                                int curr = 0;

                                                char type = ' ';

                                                foreach (char m in header)
                                                {

                                                    if (m == '+')
                                                    {
                                                        type = '+';
                                                    }
                                                    if (m == '-')
                                                    {
                                                        type = '-';
                                                    }
                                                    if (type == ' ')
                                                    {

                                                    }
                                                    if (CUModes.Contains(m) && curr <= parameters2.Count)
                                                    {
                                                        User flagged_user = channel.userFromName(parameters2[curr]);
                                                        if (flagged_user != null)
                                                        {
                                                            flagged_user.ChannelMode.mode(type.ToString() + m.ToString());
                                                        }
                                                        curr++;
                                                    }
                                                    if (parameters2.Count > curr)
                                                    {
                                                        switch (m.ToString())
                                                        {
                                                            case "b":
                                                                if (channel.Bl == null)
                                                                {
                                                                    channel.Bl = new List<SimpleBan>();
                                                                }
                                                                lock (channel.Bl)
                                                                {
                                                                    if (type == '-')
                                                                    {
                                                                        SimpleBan br = null;
                                                                        foreach (SimpleBan xx in channel.Bl)
                                                                        {
                                                                            if (xx.Target == parameters2[curr])
                                                                            {
                                                                                br = xx;
                                                                                break;
                                                                            }
                                                                        }
                                                                        if (br != null)
                                                                        {
                                                                            channel.Bl.Remove(br);
                                                                        }
                                                                        break;
                                                                    }
                                                                    channel.Bl.Add(new SimpleBan(user, parameters2[curr], ""));
                                                                }
                                                                curr++;
                                                                break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (command == "PART")
                            {
                                string chan = parameters;
                                chan = chan.Replace(" ", "");
                                string user = source.Substring(0, source.IndexOf("!"));
                                Channel channel = _server.getChannel(chan);
                                if (channel != null)
                                {
                                    User delete = null;
                                    if (channel.containsUser(user))
                                    {
                                        lock (channel.UserList)
                                        {
                                            foreach (User _user in channel.UserList)
                                            {
                                                if (_user.Nick == user)
                                                {
                                                    delete = _user;
                                                    break;
                                                }
                                            }
                                        }

                                        if (delete != null)
                                        {
                                            channel.UserList.Remove(delete);
                                        }
                                    }
                                }
                            }

                            if (command == "PONG")
                            {
                                continue;
                            }

                            if (command == "QUIT")
                            {
                                string nick = source.Substring(0, source.IndexOf("!"));
                                string _new = _value;
                                foreach (Channel item in _server.Channels)
                                {
                                    if (item.ok)
                                    {
                                        User target = null;
                                        lock (item.UserList)
                                        {
                                            foreach (User curr in item.UserList)
                                            {
                                                if (curr.Nick == nick)
                                                {
                                                    target = curr;
                                                    break;
                                                }
                                            }
                                        }
                                        if (target != null)
                                        {
                                            lock (item.UserList)
                                            {
                                                item.UserList.Remove(target);
                                            }
                                        }
                                    }
                                }
                            }

                            if (command == "KICK")
                            {
                                string nick = _command[1];
                                string _new = _value;

                                //continue;
                            }

                            if (command == "JOIN")
                            {
                                string chan = parameters;
                                chan = chan.Replace(" ", "");
                                string user = source.Substring(0, source.IndexOf("!"));
                                string _ident;
                                string _host;
                                _host = source.Substring(source.IndexOf("@") + 1);
                                if (chan == "")
                                {
                                    chan = _value;
                                }
                                _ident = source.Substring(source.IndexOf("!") + 1);
                                _ident = _ident.Substring(0, _ident.IndexOf("@"));
                                Channel channel = _server.getChannel(chan);
                                if (channel != null)
                                {
                                    if (!channel.containsUser(user))
                                    {
                                        lock (channel.UserList)
                                        {
                                            channel.UserList.Add(new User(user, _host, _server, _ident));
                                        }
                                    }
                                }
                            }
                        }
                        ProtocolMain.Datagram dt = new ProtocolMain.Datagram("DATA", text);
                        dt.Parameters.Add("network", Server);
                        buffer.DeliverMessage(dt);
                    }*/
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
            Core.DebugLog("User " + owner.nickname + " requested a backlog of data");
            int i = 0;
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
                    owner.data.MessagePool_DeliverData(total_count - buffer.oldmessages.Count, ref index, user, Server);
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
                    user.Deliver(text);
                    i = i + 1;
                    index++;

                }
                Core.DebugLog("User " + owner.nickname + " messages " + MessageBuffer.Count.ToString());
                foreach (MessageOrigin d in MessageBuffer)
                {
                    ProtocolMain.Datagram text = new ProtocolMain.Datagram("SELFDG");
                    text._InnerText = d.text;
                    text.Parameters.Add("network", Server);
                    text.Parameters.Add("buffer", i.ToString());
                    text.Parameters.Add("time", d.time.ToBinary().ToString());
                    i++;
                    user.Deliver(text);
                }
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
