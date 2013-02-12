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
using System.IO;
using System.Xml;
using System.Text;

namespace pidgeon_sv
{
    public class ProtocolMain
    {
        public class Datagram
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Name">Name of a datagram</param>
            /// <param name="Text">Value</param>
            public Datagram(string Name, string Text = "")
            {
                _Datagram = Name;
                _InnerText = Text;
            }

            public string ToDocumentXmlText()
            {
                XmlDocument datagram = new XmlDocument();
                XmlNode b1 = datagram.CreateElement(_Datagram.ToUpper());
                foreach (KeyValuePair<string, string> curr in Parameters)
                {
                    XmlAttribute b2 = datagram.CreateAttribute(curr.Key);
                    b2.Value = curr.Value;
                    b1.Attributes.Append(b2);
                }
                b1.InnerText = this._InnerText;
                datagram.AppendChild(b1);
                return datagram.InnerXml;
            }

            public static Datagram FromText(string text)
            {
                XmlDocument gram = new XmlDocument();
                gram.LoadXml(text);

                if (gram.ChildNodes.Count < 1)
                {
                    Core.DebugLog("Invalid xml for data gram");
                    return null;
                }

                ProtocolMain.Datagram datagram = new ProtocolMain.Datagram(gram.ChildNodes[0].Name, gram.ChildNodes[0].InnerText);

                foreach (XmlAttribute parameter in gram.ChildNodes[0].Attributes)
                {
                    datagram.Parameters.Add(parameter.Name, parameter.Value);
                }

                return datagram;
            }

            public string _InnerText;
            public string _Datagram;
            public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        }
        
        public class SelfData
        {
            public string text = null;
            public string nick = null;
            public DateTime time;
            public Network network = null;
            public string target = null;
            public SelfData(Network _network, string _text, DateTime date, string _target)
            {
                if (_network == null)
                {
                    Core.DebugLog("Constructor of SelfData failed, because of null network");
                    throw new Exception("Constructor of SelfData failed, because of null network");
                }
                nick = _network.nickname;
                text = _text;
                target = _target;
                time = date;
                network = _network;
            }
        }

        /// <summary>
        /// Pointer to client
        /// </summary>
        public Connection connection = null;

        public bool Connected = false;

        public static bool Valid(string datagram)
        {
            if (datagram == null)
            {
                return false;
            }
            if (datagram == "")
            {
                return false;
            }
            if (datagram.StartsWith("<") && datagram.EndsWith(">"))
            {
                return true;
            }
            return false;
        }

        public void parseCommand(string data)
        {
            XmlDocument datagram = new XmlDocument();
            datagram.LoadXml(data);
            foreach (System.Xml.XmlNode curr in datagram.ChildNodes)
            {
                parseXml(curr);
            }
        }

        public void Exit()
        {
            Connected = false;
            if (connection.account != null)
            {
                lock (connection.account.Clients)
                {
                    if (!connection.account.ClientsOK.Contains(this))
                    {
                        lock (connection.account.ClientsOK)
                        {
                            connection.account.ClientsOK.Add(this);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process the request
        /// </summary>
        /// <param name="node"></param>
        public void parseXml(XmlNode node)
        {
            Datagram response = null;
            if (connection.status == Connection.Status.WaitingPW)
            {
                switch (node.Name.ToUpper())
                {
                    case "CHANNELINFO":
                    case "RAW":
                    case "GLOBALNICK":
                    case "GLOBALIDENT":
                    case "MESSAGE":
                    case "CONNECT":
                    case "NICK":
                    case "NETWORKINFO":
                    case "MAINTLISTUSER":
                    case "MAINTADDUSER":
                    case "MAINTDELUSER":
                    case "JOIN":
                    case "PART":
                    case "BACKLOGSV":
                    case "KICK":
                    case "NETWORKLIST":
                        response = new Datagram(node.Name.ToUpper(), "PERMISSIONDENY");
                        Deliver(response);
                        return;
                }
            }

            switch (node.Name.ToUpper())
            {
                case "STATUS":
                    Responses.Status(node, this);
                    break;
                case "NETWORKINFO":
                    Responses.NetworkInfo(node, this);
                    return;
                case "RAW":
                    Responses.Raw(node, this);
                    break;
                case "NICK":
                    Responses.Nick(node, this);
                    break;
                case "CHANNELINFO":
                    Responses.ChannelInfo(node, this);
                    break;
                case "NETWORKLIST":
                    string networks = "";
                    lock (connection.account.ConnectedNetworks)
                    {
                        foreach (Network current_net in connection.account.ConnectedNetworks)
                        {
                            networks += current_net.server + "|";
                        }
                    }
                    response = new Datagram("NETWORKLIST", networks);
                    Deliver(response);
                    return;
                case "LOAD":
                    response = new Datagram("LOAD", "Pidgeon service version " + Config.version + " supported mode=ns I have " + Connection.ActiveUsers.Count.ToString() + " connections, process info: memory usage " + ((double)System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64 / 1024).ToString() + "kb");
                    Deliver(response);
                    return;
                case "BACKLOGSV":
                    if (node.Attributes.Count > 1)
                    {
                        Network network = connection.account.retrieveServer(node.Attributes[0].Value);
                        if (network != null)
                        {
                            ProtocolIrc protocol = (ProtocolIrc)network._protocol;
                            protocol.getDepth(int.Parse(node.Attributes[1].Value), this);
                            lock (connection.account.Messages)
                            {
                                foreach (SelfData xx in connection.account.Messages)
                                {
                                    if (xx.network.server == protocol.Server)
                                    {
                                        connection.account.MessageBack(xx, this);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Core.DebugLog("User " + this.connection.IP + " requested log of unknown network");
                        }
                    }
                    return;
                case "CONNECT":
                    if (connection.account.containsNetwork(node.InnerText))
                    {
                        response = new Datagram("CONNECT", "CONNECTED");
                        response.Parameters.Add("network", node.InnerText);
                        Deliver(response);
                        return;
                    }
                    int port = 6667;
                    if (node.Attributes.Count > 0)
                    {
                        port = int.Parse(node.Attributes[0].Value);
                    }
                    connection.account.ConnectIRC(node.InnerText, port);
                    Core.SL(connection.IP + ": Connecting to " + node.InnerText);
                    response = new Datagram("CONNECT", "OK");
                    response.Parameters.Add("network", node.InnerText);
                    Deliver(response);
                    break;
                case "GLOBALIDENT":
                    connection.account.ident = node.InnerText;
                    Deliver(new Datagram("GLOBALIDENT", node.InnerText));
                    break;
                case "MESSAGE":
                    if (node.Attributes.Count > 2)
                    {
                        Network network4 = connection.account.retrieveServer(node.Attributes[0].Value);
                        if (network4 != null)
                        {
                            string target = node.Attributes[2].Value;
                            string ff = node.Attributes[1].Value;
                            ProtocolIrc.Priority Priority = ProtocolIrc.Priority.Normal;
                            switch (ff)
                            {
                                case "Low":
                                    Priority = ProtocolIrc.Priority.Low;
                                    break;
                                case "High":
                                    Priority = ProtocolIrc.Priority.High;
                                    break;
                            }
                            SelfData data = new SelfData(network4, node.InnerText, DateTime.Now, target);
                            lock (connection.account.Messages)
                            {
                                connection.account.Messages.Add(data);
                            }
                            connection.account.MessageBack(data);
                            network4._protocol.Message(node.InnerText, target, Priority);
                            //ProtocolIrc prot = (ProtocolIrc)network4._protocol;
                        }
                        else
                        {
                            Core.DebugLog("Network was not found for " + connection.IP);
                        }
                    }
                    break;
                case "GLOBALNICK":
                    if (node.InnerText == "")
                    {
                        Deliver(new Datagram("GLOBALNICK", connection.account.nickname));
                        break;
                    }
                    Deliver(new Datagram("GLOBALNICK", node.InnerText));
                    connection.account.nickname = node.InnerText;
                    Core.SaveData();
                    break;
                case "AUTH":
                    string username = node.Attributes[0].Value;
                    string pw = node.Attributes[1].Value;
                    lock (Core._accounts)
                    {
                        foreach (Account curr_user in Core._accounts)
                        {
                            if (curr_user.username == username)
                            {
                                if (curr_user.password == pw)
                                {
                                    connection.account = curr_user;
                                    lock (connection.account.Clients)
                                    {
                                        connection.account.Clients.Add(this);
                                    }
                                    Core.SL(connection.IP + ": Logged in as " + connection.account.username);
                                    response = new Datagram("AUTH", "OK");
                                    response.Parameters.Add("ls", "There is " + connection.account.Clients.Count.ToString() + " connections logged in to this account");
                                    connection.status = Connection.Status.Connected;
                                    Deliver(response);
                                    return;
                                }
                            }
                        }
                    }
                    response = new Datagram("AUTH", "INVALID");
                    Deliver(response);
                    return;
            }
        }

        public ProtocolMain(Connection t)
        {
            Connected = true;
            connection = t;
        }

        public void Deliver(Datagram message)
        {
            try
            {
                if (!Connected)
                {
                    return;
                }
                XmlDocument datagram = new XmlDocument();
                XmlNode b1 = datagram.CreateElement("S" + message._Datagram.ToUpper());
                foreach (KeyValuePair<string, string> curr in message.Parameters)
                {
                    XmlAttribute b2 = datagram.CreateAttribute(curr.Key);
                    b2.Value = curr.Value;
                    b1.Attributes.Append(b2);
                }
                b1.InnerText = message._InnerText;
                datagram.AppendChild(b1);

                Send(datagram.InnerXml);
            }
            catch (Exception blah)
            {
                Core.handleException(blah);
            }
        }

        public bool Send(string text)
        {
            if (!Connected)
            {
                return false;
            }
            try
            {
                connection._w.WriteLine(text);
                connection._w.Flush();
            }
            catch (IOException)
            {
                Exit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace + ex.Message);
            }
            return true;
        }
    }
}
