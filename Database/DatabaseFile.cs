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
using System.Threading;
using System.Xml;
using System.Text;

namespace pidgeon_sv
{
    class DatabaseFile : DB
    {
        public class Index
        {
            public int mqid;

            public Index(int MQID)
            {
                mqid = MQID;
            }
        }
        private string db = "data";
        private Dictionary <string, bool> locked = new Dictionary<string,bool>();
        private Dictionary <string, Dictionary<int, Index>> Indexes = new Dictionary<string, Dictionary<int, Index>>();

        public string MessagePool(string network)
        {
                return db + System.IO.Path.DirectorySeparatorChar + network + "_messages.fs";
        }

        public DatabaseFile(SystemUser _client)
        {
            this.client = _client;
            db = Config._System.FileDBDefaultFolder + System.IO.Path.DirectorySeparatorChar + _client.username;
        }

        public override void Clear()
        {
            try
            {
                if (System.IO.Directory.Exists(db))
                {
                    System.IO.Directory.Delete(db, true);
                }
                System.IO.Directory.CreateDirectory(db);
                if (System.IO.Directory.Exists(db))
                {
                    Running = true;
                }
            }
            catch (Exception fail)
            {
                Running = false;
                Core.handleException(fail);
            }
            base.Clear();
        }

        public void Unlock(string network)
        {
            lock (locked)
            {
                if (!locked.ContainsKey(network))
                {
                    locked.Add(network, false);
                }
                if (!locked[network])
                {
                    throw new Exception("Tried to free a lock on item which wasn't locked - fix me!!");
                }
                locked[network] = false;
            }
        }

        public void Lock(string network)
        {
            lock (locked)
            {
                if (!locked.ContainsKey(network))
                {
                    locked.Add(network, false);
                }
            }
            while (locked[network])
            {
                System.Threading.Thread.Sleep(100);
            }
            locked[network] = true;
        }

        private void SendRange(ProtocolIrc.Buffer.Message message, ref int index, ProtocolMain protocol)
        {
            ProtocolMain.Datagram text = new ProtocolMain.Datagram(message.message._Datagram);
            text._InnerText = message.message._InnerText;
            foreach (KeyValuePair<string, string> current in message.message.Parameters)
            {
                text.Parameters.Add(current.Key, current.Value);
            }
            text.Parameters.Add("range", index.ToString());
            index++;
            protocol.Deliver(text);
            return;
        }

        public override void DeleteCache(string network)
        {
            try
            {
                Lock(network);
                if (MessageSize.ContainsKey(network))
                {
                    MessageSize.Remove(network);
                }
                if (!System.IO.File.Exists(MessagePool(network)))
                {
                    return;
                }
                System.IO.File.Delete(MessagePool(network));
                if (Indexes.ContainsKey(network))
                {
                    Indexes.Remove(network);
                }
                Unlock(network);
                lock (locked)
                {
                    if (locked.ContainsKey(network))
                    {
                        locked.Remove(network);
                    }
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                Unlock(network);
            }
        }

        private void SendData(ProtocolIrc.Buffer.Message message, ref int index, ProtocolMain protocol)
        {
            ProtocolMain.Datagram text = new ProtocolMain.Datagram(message.message._Datagram);
            text._InnerText = message.message._InnerText;
            foreach (KeyValuePair<string, string> current in message.message.Parameters)
            {
                text.Parameters.Add(current.Key, current.Value);
            }
            index++;
            text.Parameters.Add("buffer", index.ToString());
            protocol.Deliver(text);
            return;
        }

        public override void Store_SM(ProtocolMain.SelfData message)
        {
            
        }

        private ProtocolIrc.Buffer.Message str2M(string data)
        {
            ProtocolIrc.Buffer.Message message = new ProtocolIrc.Buffer.Message();
            XmlDocument text = new XmlDocument();
            text.LoadXml(data);
            if (text.ChildNodes.Count < 1)
            {
                Core.DebugLog("Invalid xml for message");
                return null;
            }

            if (text.ChildNodes[0].Name != "message")
            {
                Core.DebugLog("Invalid xml for message");
                return null;
            }

            message._Priority = ProtocolIrc.Priority.Normal;
            message.time = DateTime.FromBinary(long.Parse(text.ChildNodes[0].Attributes[1].Value));
            message.message = ProtocolMain.Datagram.FromText(text.ChildNodes[0].InnerText);
            
            return message;
        }

        public override void MessagePool_DeliverData(int number, ref int no, ProtocolMain protocol, string network, int MQID)
        {
            if (!Running)
            {
                return;
            }
            if (!System.IO.File.Exists(MessagePool(network)))
            {
                return;
            }
            Lock(network);
            int sent = 0;
            try
            {
                int skip = 0;
                lock (MessageSize)
                { 
                    if (!MessageSize.ContainsKey(network))
                    {
                        MessageSize.Add(network, 0);
                    }
                    if (!Indexes.ContainsKey(network))
                    {
                        Indexes.Add(network, new Dictionary<int, Index>());
                    }
                }
                if (MessageSize[network] < number)
                {
                    number = MessageSize[network];
                    skip = 0;
                } else
                {
                    skip = MessageSize[network] - number;
                }
                int current_line = 0;
                
                System.IO.StreamReader file = new System.IO.StreamReader(MessagePool(network));
                string line = null;
                Dictionary<int, Index> index = Indexes[network];
                while (((line = file.ReadLine()) != null) && current_line < number)
                {
                    if (skip > 0)
                    {
                        skip--;
                        continue;
                    }
                    if (line == "")
                    {
                        continue;
                    }

                    if ((current_line + 1) < Indexes[network].Count)
                    {
                        if (MQID < index[current_line].mqid)
                        {
                            ProtocolIrc.Buffer.Message message = str2M(line);
                            if (MQID < int.Parse(message.message.Parameters["MQID"]))
                            {
                                SendData(message, ref no, protocol);
                                sent++;
                            }
                        }
                    }
                    else
                    {
                        Core.DebugLog("Invalid index (browsing slowly) for " + network);
                        ProtocolIrc.Buffer.Message message = str2M(line);
                        if (MQID < int.Parse(message.message.Parameters["MQID"]))
                        {
                            SendData(message, ref no, protocol);
                            sent++;
                        }
                    }
                    current_line++;
                }
                Core.DebugLog("Sent messages: " + sent.ToString());
                Unlock(network);
            }
            catch (Exception fail)
            {
                Unlock(network);
                Core.handleException(fail);
            }
        }

        public override int MessagePool_Range(int from, int to, string network, ref int id, ProtocolMain protocol)
        {
            if (!Running)
            {
                return 0;
            }
            try
            {
                Core.DebugLog("Getting range from disk");
                int messages = 0;
                Lock(network);
                lock (MessageSize)
                {
                    if (!MessageSize.ContainsKey(network))
                    {
                        MessageSize.Add(network, 0);
                    }
                    if (!Indexes.ContainsKey(network))
                    {
                        Indexes.Add(network, new Dictionary<int, Index>());
                    }
                }
                int current_line = 0;

                if (!File.Exists(MessagePool(network)))
                {
                    Unlock(network);
                    Core.DebugLog("There is no datafile for " + network);
                    return 0;
                }
                System.IO.StreamReader file = new System.IO.StreamReader(MessagePool(network));
                string line = null;

                Dictionary<int, Index> index = Indexes[network];

                while ((current_line + 1) < Indexes[network].Count)
                {
                    if (line == "")
                    {
                        continue;
                    }
                    if (from <= index[current_line].mqid && to >= index[current_line].mqid)
                    {
                        ProtocolIrc.Buffer.Message message = str2M(line);
                        SendRange(message, ref id, protocol);
                        messages++;
                    }
                    current_line++;
                }
                Unlock(network);
                return messages;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                Unlock(network);
            }
            return 0;
        }

        public override int MessagePool_Backlog(int size, int mqid, string network)
        {
            if (!Running)
            {
                return 0;
            }
            try
            {
                Core.DebugLog("Getting size from disk");
                int messages = 0;
                Lock(network);
                int skip = 0;
                lock (MessageSize)
                {
                    if (!MessageSize.ContainsKey(network))
                    {
                        MessageSize.Add(network, 0);
                    }
                    if (!Indexes.ContainsKey(network))
                    {
                        Indexes.Add(network, new Dictionary<int, Index>());
                    }
                }
                if (MessageSize[network] < size)
                {
                    size = MessageSize[network];
                    skip = 0;
                }
                else
                {
                    skip = MessageSize[network] - size;
                }
                int current_line = 0;

                if (!File.Exists(MessagePool(network)))
                {
                    Unlock(network);
                    return 0;
                }
                System.IO.StreamReader file = new System.IO.StreamReader(MessagePool(network));
                string line = null;

                Dictionary<int, Index> index = Indexes[network];

                while (current_line < size && (current_line + 1) < Indexes[network].Count)
                {
                    if (skip > 0)
                    {
                        skip--;
                        continue;
                    }
                    if (line == "")
                    {
                        continue;
                    }
                    if (mqid < index[current_line].mqid)
                    {
                        messages++;
                    }
                    current_line++;
                }
                Unlock(network);
                Core.DebugLog("size retrieved for " + network);
                return messages;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                Unlock(network);
            }
            return 0;
        }

        public override void MessagePool_InsertData(ProtocolIrc.Buffer.Message message, string network)
        {
            if (!Running)
            {
                return;
            }
            Lock(network);
            try
            {
                lock (MessageSize)
                {
                    if (!MessageSize.ContainsKey(network))
                    {
                        MessageSize.Add(network, 0);
                    }
                    if (!Indexes.ContainsKey(network))
                    {
                        Indexes.Add(network, new Dictionary<int, Index>());
                    }
                }
                System.IO.File.AppendAllText(MessagePool(network), message.ToDocumentXmlText() + "\n");
                Indexes[network].Add(MessageSize[network], new Index(int.Parse(message.message.Parameters["MQID"])));
                MessageSize[network]= (MessageSize[network] + 1);
                Unlock(network);
            }
            catch (Exception fail)
            {
                Unlock(network);
                Core.handleException(fail);
            }
        }
    }
}
