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

namespace pidgeon_sv
{
    public class DB
    {
        /// <summary>
        /// Owner of this
        /// </summary>
        public SystemUser systemUser = null;
        /// <summary>
        /// Cache of message size
        /// </summary>
        public Dictionary<string, int> MessageSize = new Dictionary<string, int>();
        /// <summary>
        /// If database engine is still running
        /// </summary>
        public bool Running = false;

        /// <summary>
        /// Remove all data from DB
        /// </summary>
        public virtual void Clear()
        {
            Core.DebugLog("Clear() not implemented");
        }

        /// <summary>
        /// Delete cache
        /// </summary>
        /// <param name="network"></param>
        public virtual void DeleteCache(string network)
        {
            Core.DebugLog("DeleteCache(string network) not implemented");
        }

        public int GetMessageSize(string network)
        {
            lock (MessageSize)
            {
                if (!MessageSize.ContainsKey(network))
                {
                    MessageSize.Add(network, 0);
                }
            }
            return MessageSize[network];
        }

        /// <summary>
        /// This function is used to retrieve a backlog from storage and instantly send it to client as a stream of datagrams
        /// </summary>
        /// <param name="number">Size of backlog the user requested</param>
        /// <param name="no"></param>
        /// <param name="protocol">Protocol this user is connected through</param>
        /// <param name="network">Network name the user request backlog for</param>
        /// <param name="MQID">Initial MQID</param>
        public virtual void MessagePool_DeliverData(int number, ref int no, ProtocolMain protocol, string network, int MQID)
        {
            Core.DebugLog("MessagePool_DeliverData(int number, ref int no, ProtocolMain protocol, string network, int MQID) not implemented");
        }

        public virtual int MessagePool_Backlog(int size, int mqid, string network)
        {
            return 0;
        }

        public virtual void Store_SM(ProtocolMain.SelfData message)
        {
            Core.DebugLog("Store_SM(ProtocolMain.SelfData message) not implemented");
        }

        public virtual int MessagePool_Range(int from, int to, string network, ref int id, ProtocolMain protocol)
        {
            Core.DebugLog("MessagePool_Range(int from, int to, string network, ref int id, ProtocolMain protocol) not implemented");
            return 0;
        }

        public virtual void MessagePool_InsertData(ProtocolIrc.Buffer.Message message, string network)
        {
            Core.DebugLog("MessagePool_InsertData(ProtocolIrc.Buffer.Message message, string network) not implemented");
        }
    }
}
