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
        public List<ProtocolMain> Clients
        {
            get
            {
                List<ProtocolMain> protocols = new List<ProtocolMain>();
                foreach (Session session in Sessions)
                {
                        
                    protocols.Add(session.Protocol);
                }
                return protocols;
            }
        }

        /// <summary>
        /// Gets the sessions that are open by this user
        /// </summary>
        /// <value>
        /// The sessions.
        /// </value>
        public List<Session> Sessions
        {
            get
            {
                List<Session> s = new List<Session>();
                lock (Session.ConnectedUsers)
                {
                    foreach (Session session in Session.ConnectedUsers)
                    {
                        if (session.User == this)
                        {
                            s.Add(session);
                        }
                    }
                }
                return s;
            }
        }

        /// <summary>
        /// The buffered list of protocols, so that we don't need to call Clients::get so often
        /// </summary>
        public List<ProtocolMain> ClientsBuffer = new List<ProtocolMain>();
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
        public string Role;
        /// <summary>
        /// Pointer to database engine used by this user
        /// </summary>
        public DB DatabaseEngine = null;
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

        /// <summary>
        /// Updates the client buffer
        /// </summary>
        public void UpdateCB()
        {
            this.ClientsBuffer = this.Clients;
        }

        public bool IsApproved(string permission)
        {
             return Security.HasPermission(this.Role, permission);
        }

        /// <summary>
        /// Lock the user
        /// </summary>
        public void Lock()
        {
            if (!IsLocked)
            {
                Locked = true;
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
                    foreach (ProtocolMain pidgeon in ClientsBuffer)
                    {
                        if (pidgeon != null)
                        {
                            if (pidgeon.IsConnected)
                            {
                                pidgeon.Deliver(data);
                            }
                        }
                    }
                } else
                {
                    connection.Deliver(data);
                }
            } catch (Exception fail)
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
            } catch (Exception fail)
            {
                Core.handleException(fail);
            }
            return true;
        }

        public bool Deliver(ProtocolMain.Datagram text)
        {
            foreach (ProtocolMain client in ClientsBuffer)
            {
                client.Deliver(text);
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
            networkid.Nickname = Nickname;
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
            foreach (ProtocolMain clients in user.Clients)
            {
                ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("FAIL", "KILLED");
                datagram.Parameters.Add("code", "62");
                clients.TrafficChunks = false;
                clients.Deliver(datagram);
            }
            List<Session> session = new List<Session>();
            lock (Session.ConnectedUsers)
            {
                session.AddRange(Session.ConnectedUsers);
            }
            foreach (Session s in Session.ConnectedUsers)
            {
                if (s.User == user)
                {
                    s.Kill();
                }
            }
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
        }

        public static bool DeleteUser(SystemUser user)
        {
            lock (Core.UserList)
            {
                if (Core.UserList.Contains(user))
                {
                    user.Lock();
                    KickUser(user);
                    user.DatabaseEngine.Clear();
                    user.ConnectedNetworks.Clear();
                    user.ClientsBuffer.Clear();
                    user.Messages.Clear();
                    Core.UserList.Remove(user);
                    Core.SaveUser();
                    return true;
                }
            }
            return false;
        }

        public static bool CreateUser(string name, string password, string nick, string role, string realname, string ident)
        {
            SystemUser user = getUser(name);
            if (user != null)
            {
                return false;
            }
            user = new SystemUser(name, Core.CalculateMD5Hash(password));
            user.Role = role;
            user.Nickname = nick;
            user.RealName = realname;
            user.Ident = ident;
            lock (Core.UserList)
            {
                Core.UserList.Add(user);
            }
            Core.SaveUser();
            return true;
        }

        public static SystemUser getUser(string name)
        {
            name = name.ToLower();
            lock (Core.UserList)
            {
                foreach (SystemUser account in Core.UserList)
                {
                    if (account.UserName.ToLower() == name)
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
