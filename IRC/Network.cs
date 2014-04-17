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
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace pidgeon_sv
{
    public class Network : libirc.Network
    {
        public string NetworkID = null;
        private bool Locked = false;
        private int Current_ID = 0;

        public override bool __evt__IncomingData (libirc.Network.IncomingDataEventArgs args)
        {
            switch(args.Command)
            {
                case "PING":
                case "PONG":
                    return base.__evt__IncomingData(args);
            }
            ProtocolMain.Datagram dt = new ProtocolMain.Datagram("DATA", args.ServerLine);
            dt.Parameters.Add("network", _Protocol.Server);
            dt.Parameters.Add("MQID", getMQID().ToString());
            ProtocolIrc protocol = (ProtocolIrc)_Protocol;
            protocol.buffer.DeliverMessage(dt);
            return base.__evt__IncomingData(args);
        }
        
        public Network(string Server, libirc.Protocol sv) : base(Server, sv)
        {
            this.NetworkID = DateTime.Now.ToBinary ().ToString() + "~" + Server;
            this.Quit = "Pidgeon services - http://pidgeonclient.org";
        }

        ~Network()
        {
            SystemLog.DebugLog("Destructor called for network " + ServerName);
        }
        
        public int getMQID()
        {
            try
            {
                while (Locked)
                {
                    Thread.Sleep(10);
                }
                Locked = true;
                Current_ID++;
                int id = Current_ID;
                Locked = false;
                return id;
            }
            catch (Exception fail)
            {
                Locked = false;
                Core.handleException(fail);
                return Current_ID;
            }
        }
    }
}
