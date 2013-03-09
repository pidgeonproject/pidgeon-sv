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
        public Account client;
        public Dictionary<string, int> MessageSize = new Dictionary<string, int>();
        public bool Running = false;

        public virtual void Clear()
        {

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

        public virtual void MessagePool_DeliverData(int number, ref int no, ProtocolMain protocol, string network, ref int MQID)
        {

        }

        public virtual void MessagePool_InsertData(ProtocolIrc.Buffer.Message message, string network)
        {

        }
    }
}
