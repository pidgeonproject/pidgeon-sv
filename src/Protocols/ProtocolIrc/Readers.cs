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
        /// <summary>
        /// Get a size of backlog that starts from given id and has a specific maximal size
        /// </summary>
        /// <param name="mqid"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public int getBacklogSize(int mqid, int size)
        {
            // check memory first (quickly)
            SystemLog.DebugLog("Retrieving size of backlog for " + Server);
            DateTime start_time = DateTime.Now;
            bool FoundNewer = false;
            int backlog_size = 0;
            lock (buffer.oldmessages)
            {
                foreach (Buffer.Message message in buffer.oldmessages)
                {
                    if (int.Parse(message.Data.Parameters["MQID"]) > mqid)
                    {
                        FoundNewer = true;
                        backlog_size++;
                    }
                }
            }
            if (!FoundNewer)
            {
                SystemLog.DebugLog("No backlog data");
                return 0;
            }
            // now search the disk
            backlog_size += systemUser.DatabaseEngine.MessagePool_Backlog(size, mqid, Server);
            SystemLog.DebugLog("Parsed size " + backlog_size.ToString() + " in " + (DateTime.Now - start_time).ToString());
            return backlog_size;
        }

        public void getDepth(int RequestedSize, ProtocolMain user, int mqid)
        {
            try
            {
                SystemLog.DebugLog("User " + systemUser.Nickname + " requested a backlog of data starting from " + mqid.ToString());
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
                        SystemLog.DebugLog("User " + systemUser.Nickname + " requested a backlog, there are no data");
                        ProtocolMain.Datagram size = new ProtocolMain.Datagram("BACKLOG", "0");
                        size.Parameters.Add("network", Server);
                        user.Deliver(size);
                        return;
                    }
                    if (buffer.oldmessages.Count < RequestedSize)
                    {
                        // the backlog needs to be parsed from file
                        SystemLog.DebugLog("User " + systemUser.Nickname + " requested a backlog of " + RequestedSize.ToString() + " datagrams, but there are not so many in memory as they requested, recovering some from storage");
                        // we get the total size of memory and disk
                        total_count = buffer.oldmessages.Count + systemUser.DatabaseEngine.GetMessageSize(Server);
                        if (total_count < RequestedSize)
                        {
                            SystemLog.DebugLog("User " + systemUser.Nickname + " requested a backlog of " + RequestedSize.ToString() + " datagrams, but there are not so many in memory neither in the storage in total only " + total_count.ToString() + " right now :o");
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
                            backlog_size = total_requested_size;
                        SystemLog.DebugLog("Delivering backlog messages to peer: " + backlog_size.ToString());
                        ProtocolMain.Datagram count = new ProtocolMain.Datagram("BACKLOG", backlog_size.ToString());
                        count.Parameters.Add("network", Server);
                        user.Deliver(count);
                        // we send the data using the storage
                        systemUser.DatabaseEngine.MessagePool_DeliverData(total_count - buffer.oldmessages.Count, ref index, user, Server, mqid);
                        if (index < 0)
                        {
                            // this makes no sense, the datafile was probably corrupted
                            SystemLog.DebugLog("Something went wrong");
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
                        SystemLog.DebugLog("For some reason the backlog size was bigger than number of all messages in memory");
                        backlog_size = buffer.oldmessages.Count;
                    }

                    while (i < backlog_size)
                    {
                        if (int.Parse(buffer.oldmessages[i].Data.Parameters["MQID"]) > mqid)
                        {
                            ProtocolMain.Datagram text = new ProtocolMain.Datagram(buffer.oldmessages[i].Data._Datagram);
                            text._InnerText = buffer.oldmessages[i].Data._InnerText;
                            foreach (KeyValuePair<string, string> current in buffer.oldmessages[i].Data.Parameters)
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
            SystemLog.DebugLog("User " + systemUser.Nickname + " requested a range of data starting from " + from.ToString());
            int index = 0;
            lock (buffer.oldmessages)
            {
                foreach (Buffer.Message curr in buffer.oldmessages)
                {
                    int mq = int.Parse(curr.Data.Parameters["MQID"]);
                    if (from >= mq && last <= mq)
                    {
                        ProtocolMain.Datagram text = new ProtocolMain.Datagram(curr.Data._Datagram);
                        text._InnerText = curr.Data._InnerText;
                        foreach (KeyValuePair<string, string> current in curr.Data.Parameters)
                        {
                            text.Parameters.Add(current.Key, current.Value);
                        }
                        text.Parameters.Add("range", index.ToString());
                        index++;
                        user.Deliver(text);
                    }
                }
            }

            systemUser.DatabaseEngine.MessagePool_Range(from, last, Server, ref index, user);
        }
    }
}
