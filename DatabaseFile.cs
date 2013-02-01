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
using System.Xml;
using System.Text;

namespace pidgeon_sv
{
    class DatabaseFile : DB
    {
        public string db = "data";
        public Dictionary <string, bool> locked = new Dictionary<string,bool>();

        public string MessagePool(string network)
        {
                return db + System.IO.Path.DirectorySeparatorChar + network + "_messages.fs";
        }

        public DatabaseFile(Account _client)
        {
            this.client = _client;
            db = "data" + System.IO.Path.DirectorySeparatorChar + _client.username;
        }

        public override void Clear()
        {
            if (System.IO.Directory.Exists(db))
            {
                System.IO.Directory.Delete(db, true);
            }
            System.IO.Directory.CreateDirectory(db);
            base.Clear();
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


        private void SendData(ProtocolIrc.Buffer.Message message, ref int index, ProtocolMain protocol)
        {
            ProtocolMain.Datagram text = new ProtocolMain.Datagram(message.message._Datagram);
            text._InnerText = message.message._InnerText;
            foreach (KeyValuePair<string, string> current in message.message.Parameters)
            {
                text.Parameters.Add(current.Key, current.Value);
            }
            text.Parameters.Add("buffer", index.ToString());
            protocol.Deliver(text);
            index++;
            return;
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

        public override void MessagePool_DeliverData(int number, ref int no, ProtocolMain protocol, string network)
        {
            if (!System.IO.File.Exists(MessagePool(network)))
            {
                return;
            }
            Lock(network);
            try
            {
                int skip = 0;
                lock (MessageSize)
                { 
                    if (!MessageSize.ContainsKey(network))
                    {
                        MessageSize.Add(network, 0);
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
                while (((line = file.ReadLine()) != null) && current_line < number)
                {
                    if (current_line < skip)
                    {
                        current_line++;
                        continue;
                    }
                    if (line == "")
                    {
                        continue;
                    }
                    SendData(str2M(line), ref no, protocol);
                    current_line++;
                }

                locked[network] = false;
            }
            catch (Exception fail)
            {
                locked[network] = false;
                Core.handleException(fail);
            }
        }

        public override void MessagePool_InsertData(ProtocolIrc.Buffer.Message message, string network)
        {
            Lock(network);
            try
            {
                lock (MessageSize)
                {
                    if (!MessageSize.ContainsKey(network))
                    {
                        MessageSize.Add(network, 0);
                    }
                }
                System.IO.File.AppendAllText(MessagePool(network), message.ToDocumentXmlText() + "\n");
                MessageSize[network]= (MessageSize[network] + 1);
                locked[network] = false;
            }
            catch (Exception fail)
            {
                locked[network] = false;
                Core.handleException(fail);
            }
        }
    }
}