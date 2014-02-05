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
    public class SystemUser
    {
        /// <summary>
        /// List of active connections to services that are logged in as this user
        /// </summary>
        public List<ProtocolMain> Clients = new List<ProtocolMain>();
        /// <summary>
        /// The username
        /// </summary>
        public string UserName = null;
        /// <summary>
        /// The password
        /// </summary>
        public string Password = "";
        /// <summary>
        /// The nickname
        /// </summary>
        public string Nickname = "PidgeonUser";
        /// <summary>
        /// The ident
        /// </summary>
        public string Ident = "pidgeon";
        /// <summary>
        /// The realname
        /// </summary>
        public string RealName = "http://pidgeonclient.org";
        /// <summary>
        /// List of networks this user is connected to
        /// </summary>
        public List<Network> ConnectedNetworks = new List<Network>();
        /// <summary>
        /// The messages
        /// </summary>
        public List<ProtocolMain.SelfData> Messages = new List<ProtocolMain.SelfData>();
        /// <summary>
        /// Pointer to database engine used by this user
        /// </summary>
        public DB DatabaseEngine = null;
        /// <summary>
        /// The permission list.
        /// </summary>
        public List<Security.SecurityRole> Roles = new List<pidgeon_sv.Security.SecurityRole>();
        private bool Locked = false;
        /// <summary>
        /// Whether this user is locked
        /// </summary>
        public bool IsLocked
        {
            get
            {
                return Locked;
            }
        }
        
        // number of self messages of this user
        private int MessageCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="pidgeon_sv.SystemUser"/> class.
        /// </summary>
        /// <param name='user'>
        /// User
        /// </param>
        /// <param name='pw'>
        /// Password
        /// </param>
        public SystemUser(string user, string pw, bool ro = false)
        {
            UserName = user;
            Password = pw;
            DatabaseEngine = new DatabaseFile(this);
            if (ro == false)
            {
                SystemLog.DebugLog("Cleaning buffer for " + UserName);
                DatabaseEngine.Clear();
            }
        }

        public bool IsApproved(Security.Permission permission)
        {
            lock (this.Roles)
            {
                foreach (Security.SecurityRole role in this.Roles)
                {
                    if (role.Name == "Root")
                    {
                        return true;
                    }
                    if (role.HasPermission(permission))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Lock the user
        /// </summary>
        public void Lock()
        {
            if (!IsLocked)
            {
                Locked = true;
                KickUser(this);
            }
        }

        /// <summary>
        /// Unlock the user
        /// </summary>
        public void Unlock()
        {
            if (IsLocked)
            {
                Locked = false;
            }
        }

        /// <summary>
        /// Send message back to user
        /// </summary>
        /// <param name="text"></param>
        /// <param name="connection"></param>
        public void MessageBack(ProtocolMain.SelfData text, ProtocolMain connection = null)
        {
            try
            {
                if (text.network == null)
                {
                    SystemLog.DebugLog("network is bad");
                    return;
                }
                ProtocolMain.Datagram data = new ProtocolMain.Datagram("MESSAGE", text.text);
                data.Parameters.Add("nick", text.nick);
                data.Parameters.Add("network", text.network.ServerName);
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
                    if (servername.ServerName == name)
                    {
                        return servername;
                    }
                }
            }
            return null;
        }

        public bool FailSafeDeliver(ProtocolMain.Datagram text)
        {
            try
            {
                Deliver(text);
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            return true;
        }

        public bool Deliver(ProtocolMain.Datagram text)
        {
            lock (Clients)
            {
                foreach (ProtocolMain client in Clients)
                {
                    client.Deliver(text);
                }
            }
            return true;
        }

        public static bool IsValid(string name)
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

        /// <summary>
        /// Connect to irc network
        /// </summary>
        /// <param name="network"></param>
        /// <param name="port"></param>
        /// <param name="ssl"></param>
        /// <returns></returns>
        public bool ConnectIRC(string network, int port = 6667, bool ssl = false)
        {
            ProtocolIrc server = new ProtocolIrc();
            server.SSL = ssl;
            Network networkid = new Network(network, server);
            networkid.nickname = Nickname;
            networkid.Ident = Ident;
            networkid.UserName = RealName;
            networkid.Quit = "http://pidgeonclient.org";
            lock (ConnectedNetworks)
            {
                ConnectedNetworks.Add(networkid);
            }
            server.Server = network;
            server.Port = port;
            server._network = networkid;
            server.owner = this;
            server.buffer = new ProtocolIrc.Buffer(this, network);
            server.Open();
            return true;
        }

        /// <summary>
        /// Retrieve all messages that were sent somewhere by this user
        /// </summary>
        /// <param name="mqid"></param>
        /// <param name="_protocol"></param>
        /// <param name="protocol"></param>
        public void MessageBacklog(int mqid, Protocol _protocol, ProtocolMain protocol)
        {
            lock (Messages)
            {
                foreach (ProtocolMain.SelfData message in Messages)
                {
                    if (message.MQID > mqid)
                    {
                        if (message.network.ServerName == _protocol.Server)
                        {
                            MessageBack(message, protocol);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Send a message to some irc network
        /// </summary>
        /// <param name="data"></param>
        public void Message(ProtocolMain.SelfData data)
        {
            MessageCount++;
            lock (Messages)
            {
                Messages.Add(data);
            }
        }

        /// <summary>
        /// Kicks the user
        /// </summary>
        /// <param name='user'>
        /// User
        /// </param>
        public static void KickUser(SystemUser user)
        {
            lock (user.Messages)
            {
                user.Messages.Clear();
            }
            lock (user.ConnectedNetworks)
            {
                List<Network> networks = new List<Network>();
                networks.AddRange(user.ConnectedNetworks);
                foreach (Network network in networks)
                {
                    // disconnect from all irc networks
                    network._Protocol.Exit();
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

        public static void DeleteUser(SystemUser user)
        {
            lock (Core.UserList)
            {
                if (Core.UserList.Contains(user))
                {
                    user.Lock();
                    user.DatabaseEngine.Clear();
                    user.ConnectedNetworks.Clear();
                    user.Clients.Clear();
                    user.Messages.Clear();
                    Core.UserList.Remove(user);
                }
            }
            Core.SaveUser();
        }

        public static void CreateUser(string name, string password, string nick, List<pidgeon_sv.Security.SecurityRole> RoleList, string realname, string ident)
        {
            SystemUser user = new SystemUser(name, password);
            user.Roles = RoleList;
            user.Nickname = nick;
            user.RealName = realname;
            user.Ident = ident;
            lock (Core.UserList)
            {
                Core.UserList.Add(user);
            }
            Core.SaveUser();
        }

        public static SystemUser getUser(string name)
        {
            lock (Core.UserList)
            {
                foreach (SystemUser account in Core.UserList)
                {
                    if (account.UserName == name)
                    {
                        return account;
                    }
                }
            }
            return null;
        }

        public bool containsNetwork(string network)
        {
            lock (ConnectedNetworks)
            {
                foreach (Network curr in ConnectedNetworks)
                {
                    if (network == curr.ServerName)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
