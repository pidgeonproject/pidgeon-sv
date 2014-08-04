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
    public partial class ProtocolIrc : libirc.Protocols.ProtocolIrc
    {
        public class Buffer
        {
            public class Message
            {
                public libirc.Defs.Priority Priority = libirc.Defs.Priority.Normal;
                public DateTime MessageTime;
                public ProtocolMain.Datagram Data = null;

                public Message()
                {
                    MessageTime = DateTime.Now;
                }

                public string ToDocumentXmlText()
                {
                    XmlDocument datagram = new XmlDocument();
                    XmlNode b1 = datagram.CreateElement("message");
                    Dictionary<string, string> Parameters = new Dictionary<string, string>();
                    Parameters.Add("priority", Priority.ToString());
                    Parameters.Add("time", MessageTime.ToBinary().ToString());
                    foreach (KeyValuePair<string, string> curr in Parameters)
                    {
                        XmlAttribute b2 = datagram.CreateAttribute(curr.Key);
                        b2.Value = curr.Value;
                        b1.Attributes.Append(b2);
                    }
                    b1.InnerText = Data.ToDocumentXmlText();
                    datagram.AppendChild(b1);
                    return datagram.InnerXml;
                }
            }

            public SystemUser ParentUser = null;
            public string Network = null;
            protected internal List<Message> messages = new List<Message>();
            protected internal List<Message> oldmessages = new List<Message>();
            public ProtocolIrc protocol = null;

            public Buffer(SystemUser _account, string server)
            {
                Network = server;
                ParentUser = _account;
            }

            public void DeliverMessage(ProtocolMain.Datagram Message, libirc.Defs.Priority Pr = libirc.Defs.Priority.Normal)
            {
                Message text = new Message();
                text.Priority = Pr;
                text.Data = Message;
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
                    while (Core.IsRunning)
                    {
                        try
                        {
                            if (messages.Count == 0)
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                            List<Message> newmessages = new List<Message>();
                            lock (messages)
                            {
                                if (messages.Count > 0)
                                {
                                    newmessages.AddRange(messages);
                                    messages.Clear();
                                }
                            }

                            if (newmessages.Count > 0)
                            {
                                if (protocol.systemUser != null)
                                {
                                    foreach (Message message in newmessages)
                                    {
                                        message.Data.Parameters.Add("time", message.MessageTime.ToBinary().ToString());
                                        protocol.systemUser.Deliver(message.Data);
                                    }
                                }

                                lock (oldmessages)
                                {
                                    if (oldmessages.Count > Configuration._System.MaximumBufferSize)
                                    {
                                        FlushOld();
                                    }

                                    oldmessages.AddRange(newmessages);
                                }
                            }
                            newmessages.Clear();
                            System.Threading.Thread.Sleep(200);
                        }
                        catch (System.Threading.ThreadAbortException)
                        {
                            ThreadPool.UnregisterThis();
                            return;
                        }
                    }
                }
                catch (Exception fail)
                {
                    ThreadPool.UnregisterThis();
                    Core.handleException(fail, true);
                }
            }

            public void FlushOld()
            {
                int Count = 0;
                while (oldmessages.Count > Configuration._System.MinimumBufferSize)
                {
                    Count++;
                    ParentUser.DatabaseEngine.MessagePool_InsertData(oldmessages[0], Network);
                    oldmessages.RemoveAt(0);
                }
                SystemLog.DebugLog("Stored " + Count.ToString());
            }
        }
    }
}
