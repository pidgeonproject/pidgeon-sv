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
    public class Account
    {
        public List<ProtocolIrc> networks = new List<ProtocolIrc>();
        public List<ProtocolMain> ClientsOK = new List<ProtocolMain>();
        public List<ProtocolMain> Clients = new List<ProtocolMain>();
        public string username = null;
        public string password = "";
        public string nickname = "PidgeonUser";
        public string ident = "pidgeon";
        public string realname = "http://pidgeonclient.org";
        public List<Network> ConnectedNetworks = new List<Network>();
        public List<ProtocolMain.SelfData> Messages = new List<ProtocolMain.SelfData>();
        public DB data = null;
        public UserLevel Level = UserLevel.User;
        public bool Locked = false;

        public Account(string user, string pw)
        {
            username = user;
            password = pw;
            data = new DatabaseFile(this);
            Core.DebugLog("Cleaning DB for " + username);
            data.Clear();
        }


        public void MessageBack(ProtocolMain.SelfData text, ProtocolMain connection = null)
        {
            try
            {
                if (text.network == null)
                {
                    Core.DebugLog("network is bad");
                    return;
                }
                ProtocolMain.Datagram data = new ProtocolMain.Datagram("MESSAGE", text.text);
                data.Parameters.Add("nick", text.nick);
                data.Parameters.Add("network", text.network.server);
                data.Parameters.Add("time", text.time.ToBinary().ToString());
                data.Parameters.Add("target", text.target);
                data.Parameters.Add("MQID", text.MQID.ToString());
                if (connection == null)
                {
                    lock (Clients)
                    {
                        foreach (ProtocolMain pidgeon in Clients)
                        {
                            if (pidgeon != null)
                            {
                                if (pidgeon.Connected)
                                {
                                    pidgeon.Deliver(data);
                                }
                            }
                        }
                    }
                }
                else
                {
                    connection.Deliver(data);
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }

        public Network retrieveServer(string name)
        {
            lock (ConnectedNetworks)
            {
                foreach (Network servername in ConnectedNetworks)
                {
                    if (servername.server == name)
                    {
                        return servername;
                    }
                }
            }
            return null;
        }

        public bool Deliver(ProtocolMain.Datagram text)
        {
            lock (Clients)
            {
                try
                {
                    if (Clients.Count == 0)
                    {
                        return true;
                    }
                    foreach (ProtocolMain ab in Clients)
                    {
                        ab.Deliver(text);
                    }
                    lock (ClientsOK)
                    {
                        foreach (ProtocolMain i in ClientsOK)
                        {
                            Clients.Remove(i);
                        }
                    }
                }
                catch (Exception fail)
                {
                    Core.handleException(fail);
                }
            }
            return true;
        }

        public bool ConnectIRC(string network, int port = 6667)
        {
            try
            {
                ProtocolIrc server = new ProtocolIrc();
                Network networkid = new Network(network, server);
                networkid.nickname = nickname;
                networkid.ident = ident;
                networkid.username = realname;
                networkid.quit = "http://pidgeonclient.org";
                lock (ConnectedNetworks)
                {
                    ConnectedNetworks.Add(networkid);
                }
                server.Server = network;
                server.Port = port;
                server._server = networkid;
                server.owner = this;
                server.buffer = new ProtocolIrc.Buffer(this, network);
                lock (networks)
                {
                    networks.Add(server);
                }
                server.Open();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            return true;
        }

        public static void CreateEntry(string name, string password, string nick, UserLevel level)
        {
            Account user = new Account(name, password);
            user.Level = level;
            user.nickname = nick;
            Core._accounts.Add(user);
            Core.SaveUser();
        }

        public static Account getUser(string name)
        {
            lock (Core._accounts)
            {
                foreach (Account account in Core._accounts)
                {
                    if (account.username == name)
                    {
                        return account;
                    }
                }
            }
            return null;
        }

        private static void CreateUser()
        { 
            
        }

        public static void Manage()
        {
            bool OK = true;
            while (OK)
            {
                Console.WriteLine("Select one option:");
                Console.WriteLine("1. Create new user");
                Console.WriteLine("ctrl + c - quit");
                ConsoleKeyInfo data = Console.ReadKey();
                switch (data.Key)
                { 
                    case ConsoleKey.D1:
                        CreateUser();
                        break;
                    default:
                        Console.WriteLine("This is unknown option for me");
                        break;
                }
            }
        }

        public bool containsNetwork(string network)
        {
            lock (ConnectedNetworks)
            {
                foreach (Network curr in ConnectedNetworks)
                {
                    if (network == curr.server)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public enum UserLevel
        {
            Root,
            Admin,
            User
        }
    }
}
