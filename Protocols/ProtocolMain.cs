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
using System.IO;
using System.Xml;
using System.Text;

namespace pidgeon_sv
{
    public class ProtocolMain
    {
        public class Datagram
        {
            public string _InnerText;
            public string _Datagram;
            public Dictionary<string, string> Parameters = new Dictionary<string, string>();

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

            public static bool IsValid(string datagram)
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
                    SystemLog.DebugLog("Invalid xml for datagram: " + text);
                    return null;
                }

                ProtocolMain.Datagram datagram = new ProtocolMain.Datagram(gram.ChildNodes[0].Name, gram.ChildNodes[0].InnerText);

                foreach (XmlAttribute parameter in gram.ChildNodes[0].Attributes)
                {
                    datagram.Parameters.Add(parameter.Name, parameter.Value);
                }

                return datagram;
            }
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
                    throw new Exception("Constructor of SelfData failed, because of null network");
                }
                nick = _network.nickname;
                text = _text;
                target = _target;
                time = date;
                network = _network;
                MQID = curr;
            }

            public string ToDocumentXmlText()
            {
                XmlDocument datagram = new XmlDocument();
                XmlNode b1 = datagram.CreateElement("SM");
                XmlAttribute Nick = datagram.CreateAttribute("nick");
                Nick.Value = nick;
                b1.Attributes.Append(Nick);
                XmlAttribute Time = datagram.CreateAttribute("time");
                Time.Value = time.ToBinary().ToString();
                b1.Attributes.Append(Time);
                XmlAttribute nw = datagram.CreateAttribute("network");
                nw.Value = network.ServerName;
                b1.Attributes.Append(nw);
                XmlAttribute Target = datagram.CreateAttribute("tg");
                Target.Value = target;
                b1.Attributes.Append(Target);
                XmlAttribute Mqid = datagram.CreateAttribute("mqid");
                Mqid.Value = MQID.ToString();
                b1.Attributes.Append(Mqid);
                b1.InnerText = text;
                datagram.AppendChild(b1);
                return datagram.InnerXml;
            }
        }

        /// <summary>
        /// If this is true the buffer will be flushed only when there is a large amount of data
        /// this was should be faster when there is something huge being transfered
        /// </summary>
        public bool TrafficChunks = false;
        private string TrafficChunk = "";
        private bool isDestroyed = false;

        /// <summary>
        /// Pointer to client
        /// </summary>
        public Session connection = null;

        public bool Connected = false;

        public ProtocolMain(Session t)
        {
            Connected = true;
            connection = t;
        }

        ~ProtocolMain()
        {
            SystemLog.DebugLog("Destructor called for ProtocolMain of " + connection.IP);
        }

        public void ParseCommand(string data)
        {
            XmlDocument datagram = new XmlDocument();
            datagram.LoadXml(data);
            foreach (System.Xml.XmlNode curr in datagram.ChildNodes)
            {
                ParseXml(curr);
            }
        }

        public void Disconnect()
        {
            try
            {
                if (!Connected)
                {
                    return;
                }
                Connected = false;

                if (connection != null)
                {
                    connection.Disconnect();
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }

        public void Exit()
        {
            if (isDestroyed)
            {
                return;
            }
            Disconnect();
            if (connection != null)
            {
                if (connection.User != null)
                {
                    if (connection.User.Clients != null)
                    {
                        lock (connection.User.Clients)
                        {
                            if (connection.User.Clients.Contains(this))
                            {
                                connection.User.Clients.Remove(this);
                            }
                        }
                    }
                }
            }
            isDestroyed = true;
            connection = null;
        }

        /// <summary>
        /// Process the request
        /// </summary>
        /// <param name="node"></param>
        public void ParseXml(XmlNode node)
        {
            Datagram response = null;
            if (connection.status == Session.Status.WaitingPW)
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
                    case "BACKLOGRANGE":
                    case "KICK":
                    case "NETWORKLIST":
                        response = new Datagram(node.Name.ToUpper(), "PERMISSIONDENY");
                        Deliver(response);
                        return;
                }
            }
            try
            {
                switch (node.Name.ToUpper())
                {
                    case "STATUS":
                        Responses.Status(node, this);
                        return;
                    case "NETWORKINFO":
                        Responses.NetworkInfo(node, this);
                        return;
                    case "RAW":
                        Responses.Raw(node, this);
                        return;
                    case "NICK":
                        Responses.Nick(node, this);
                        return;
                    case "CHANNELINFO":
                        Responses.ChannelInfo(node, this);
                        return;
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
                        return;
                    case "GLOBALIDENT":
                        Responses.GlobalIdent(node, this);
                        return;
                    case "MESSAGE":
                        Responses.Message(node, this);
                        return;
                    case "GLOBALNICK":
                        Responses.GlobalNick(node, this);
                        return;
                    case "AUTH":
                        Responses.Auth(node, this);
                        return;
                    case "SYSTEM":
                        Responses.Manage(node, this);
                        return;
                    case "REMOVE":
                        Responses.DiscNw(node, this);
                        return;
                    case "BACKLOGRANGE":
                        Responses.BacklogRange(node, this);
                        return;
                    case "USERLIST":
                        Responses.UserList(node, this);
                        return;
                    case "FAIL":
                        return;
                    case "PING":
                        return;
                }

                response = new ProtocolMain.Datagram("FAIL", "GENERIC");
                response.Parameters.Add("code", "4");
                response.Parameters.Add("description", "invalid data: " + node.Name);
                Deliver(response);
            }
            catch (Exception fail)
            {
                SystemLog.DebugLog(fail.ToString());
                response = new ProtocolMain.Datagram("FAIL", "GENERIC");
                response.Parameters.Add("code", "6");
                response.Parameters.Add("description", "internal error: " + fail.Message.ToString());
                Deliver(response);
            }
        }

        public void Deliver(Datagram message)
        {
            if (!Connected)
            {
                SystemLog.WriteLine("Error: sending a text to closed connection " + connection.IP);
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

        public bool Send(string text, bool Enforced = false)
        {
            if (!Connected)
            {
                SystemLog.WriteLine("Error: sending a text to closed connection " + connection.IP);
                return false;
            }
            try
            {
                lock (connection._StreamWriter)
                {
                    if (!TrafficChunks)
                    {
                        connection._StreamWriter.WriteLine(text);
                        if (TrafficChunk != "")
                        {
                            connection._StreamWriter.WriteLine(TrafficChunk);
                            TrafficChunk = "";
                        }
                        connection._StreamWriter.Flush();
                        return true;
                    }
                    else
                    {
                        lock (TrafficChunk)
                        {
                            TrafficChunk += text + "\n";
                            if (TrafficChunk.Length > 2000 || Enforced)
                            {
                                connection._StreamWriter.WriteLine(TrafficChunk);
                                connection._StreamWriter.Flush();
                                TrafficChunk = "";
                            }
                        }
                        return true;
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                return false;
            }
            catch (IOException fail)
            {
                SystemLog.WriteLine("Connection closed: " + fail.ToString());
                Disconnect();
                return false;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                return false;
            }
        }
    }
}