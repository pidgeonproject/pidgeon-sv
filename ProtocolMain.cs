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
        public bool TrafficChunks = false;
        private string TrafficChunk = "";

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
            public int MQID;
            public SelfData(Network _network, string _text, DateTime date, string _target, int curr)
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
                MQID = curr;
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
                    case "SYSTEM":
                    case "REMOVE":
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
                    Responses.NetworkList(node, this);
                    return;
                case "LOAD":
                    Responses.Load(node, this);
                    return;
                case "BACKLOGSV":
                    Responses.BacklogSv(node, this);
                    return;
                case "CONNECT":
                    Responses.Connect(node, this);
                    break;
                case "GLOBALIDENT":
                    Responses.GlobalIdent(node, this);
                    break;
                case "MESSAGE":
                    Responses.Message(node, this);
                    break;
                case "GLOBALNICK":
                    Responses.GlobalNick(node, this);
                    break;
                case "AUTH":
                    Responses.Auth(node, this);
                    return;
                case "SYSTEM":
                    Responses.Manage(node, this);
                    return;
                case "REMOVE":
                    Responses.DiscNw(node, this);
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

        public bool Send(string text, bool Enforced = false)
        {
            if (!Connected)
            {
                return false;
            }
            try
            {
                if (!TrafficChunks)
                {
                    connection._w.WriteLine(text);
                    connection._w.Flush();
                    if (TrafficChunk != "")
                    {
                        connection._w.WriteLine(TrafficChunk);
                        connection._w.Flush();
                        TrafficChunk = "";
                    }
                    return true;
                }
                else
                {
                    lock (TrafficChunk)
                    {
                        TrafficChunk += text + "\n";
                        if (TrafficChunk.Length > 2000 || Enforced)
                        {
                            connection._w.WriteLine(TrafficChunk);
                            connection._w.Flush();
                            TrafficChunk = "";
                        }
                    }
                    return false;
                }
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
