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
		/// <summary>
		/// List of active connections to services
		/// </summary>
        public List<ProtocolMain> Clients = new List<ProtocolMain>();
		/// <summary>
		/// The username.
		/// </summary>
        public string username = null;
		/// <summary>
		/// The password.
		/// </summary>
        public string password = "";
		/// <summary>
		/// The nickname.
		/// </summary>
        public string nickname = "PidgeonUser";
		/// <summary>
		/// The ident.
		/// </summary>
        public string ident = "pidgeon";
		/// <summary>
		/// The realname.
		/// </summary>
        public string realname = "http://pidgeonclient.org";
		/// <summary>
		/// The connected networks.
		/// </summary>
        public List<Network> ConnectedNetworks = new List<Network>();
		/// <summary>
		/// The messages.
		/// </summary>
        public List<ProtocolMain.SelfData> Messages = new List<ProtocolMain.SelfData>();
        public DB data = null;
        public UserLevel Level = UserLevel.User;
        public bool Locked = false;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="pidgeon_sv.Account"/> class.
		/// </summary>
		/// <param name='user'>
		/// User
		/// </param>
		/// <param name='pw'>
		/// Password
		/// </param>
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
                }
                catch (Exception fail)
                {
                    Core.handleException(fail);
                }
            }
            return true;
        }

        public static bool isValid(string name)
        {
            if (name.Contains("&") ||
                name.Contains("|") ||
                name.Contains(" ") ||
                name.Contains(":"))
            {
                return false;
            }
            return true;
        }

        public bool ConnectIRC(string network, int port = 6667, bool ssl = false)
        {
            try
            {
                ProtocolIrc server = new ProtocolIrc();
                server.SSL = ssl;
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
                server.Open();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            return true;
        }
		
		/// <summary>
		/// Kicks the user
		/// </summary>
		/// <param name='user'>
		/// User.
		/// </param>
        public static void KickUser(Account user)
        {
            user.Locked = true;
            lock (user.ConnectedNetworks)
            {
                List<Network> networks = new List<Network>();
                networks.AddRange(user.ConnectedNetworks);
                foreach (Network network in networks)
                {
                    // disconnect from all irc networks
                    network._protocol.Exit();
                }
            }

            lock (user.Clients)
            {
                List<ProtocolMain> connections = new List<ProtocolMain>();
                connections.AddRange(user.Clients);
                foreach (ProtocolMain clients in connections)
                {
                    ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("FAIL", "KILLED");
                    datagram.Parameters.Add("code", "6");
                    clients.TrafficChunks = false;
                    clients.Deliver(datagram);
                    clients.Exit();
                }
            }
        }

        public static void DeleteUser(Account user)
        {
            lock (Core._accounts)
            {
                if (Core._accounts.Contains(user))
                {
                    KickUser(user);
                    user.data.Clear();
                    user.ConnectedNetworks.Clear();
                    user.Clients.Clear();
                    user.Messages.Clear();
                    Core._accounts.Remove(user);
                }
            }
            Core.SaveUser();
        }

        public static void CreateEntry(string name, string password, string nick, UserLevel level, string realname, string ident)
        {
            Account user = new Account(name, password);
            user.Level = level;
            user.nickname = nick;
            user.realname = realname;
            user.ident = ident;
            lock (Core._accounts)
            {
                Core._accounts.Add(user);
            }
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
