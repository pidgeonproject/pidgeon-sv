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
                if (string.IsNullOrEmpty(datagram))
                    return false;
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
                nick = _network.Nickname;
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

        /// <summary>
        /// Pointer to session that is using this protocol
        /// </summary>
        public Session session = null;

        public bool IsConnected
        {
            get
            {
                if (session != null)
                {
                    return session.IsConnected;
                }
                return false;
            }
        }

        public ProtocolMain(Session t)
        {
            session = t;
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

        public void Exit()
        {
            session = null;
        }

        /// <summary>
        /// Process the request
        /// </summary>
        /// <param name="node"></param>
        public void ParseXml(XmlNode node)
        {
            Datagram response = null;
            if (session.status == Session.Status.WaitingPW)
            {
                switch (node.Name.ToUpper())
                {
                    case "CHANNELINFO":
                    case "DEBUG":
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
                    case "DEBUG":
                        Responses.Debug(node, this);
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
                Core.handleException(fail);
                response = new ProtocolMain.Datagram("FAIL", "GENERIC");
                response.Parameters.Add("code", "6");
                response.Parameters.Add("description", "internal error: " + fail.Message.ToString());
                Deliver(response);
            }
        }

        public void Deliver(Datagram message)
        {
            if (!IsConnected)
            {
                SystemLog.Error("sending a text to closed connection " + session.IP);
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
            if (!IsConnected)
            {
                SystemLog.WriteLine("Error: sending a text to closed connection " + session.IP);
                return false;
            }
            try
            {
                lock (session._StreamWriter)
                {
                    if (!TrafficChunks)
                    {
                        session._StreamWriter.WriteLine(text);
                        if (TrafficChunk != "")
                        {
                            session._StreamWriter.WriteLine(TrafficChunk);
                            TrafficChunk = "";
                        }
                        session._StreamWriter.Flush();
                        return true;
                    }
                    else
                    {
                        lock (TrafficChunk)
                        {
                            TrafficChunk += text + "\n";
                            if (TrafficChunk.Length > 2000 || Enforced)
                            {
                                session._StreamWriter.WriteLine(TrafficChunk);
                                session._StreamWriter.Flush();
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
                session.Disconnect();
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
