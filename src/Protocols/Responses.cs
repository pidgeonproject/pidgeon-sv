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
using System.Xml;
using System.IO;
using System.Text;

namespace pidgeon_sv
{
    class Responses
    {
        public static void Debug(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response;
            if (!protocol.session.User.IsApproved(Permission.DebugCore))
            {
                response = new ProtocolMain.Datagram("DENIED", "DEBUG");
                protocol.Deliver(response);
                return;
            }
            // get a list of all threads we use
            response = new ProtocolMain.Datagram("DEBUG", "THREAD");
            List<System.Threading.Thread> threads = ThreadPool.Threads;
            foreach (System.Threading.Thread thread in threads)
                response.Parameters.Add("t" + thread.ManagedThreadId.ToString(), thread.Name + ";" + thread.Priority.ToString() + ";" + thread.ThreadState.ToString());

            protocol.Deliver(response);
        }

        public static void Status(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string info = protocol.session.status.ToString();
            response = new ProtocolMain.Datagram("STATUS", info);
            protocol.Deliver(response);
        }

        public static void NetworkInfo(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            Network network = protocol.session.User.RetrieveServer(node.InnerText);
            if (network == null)
            {
                response = new ProtocolMain.Datagram("NETWORKINFO", "UNKNOWN");
                response.Parameters.Add("network", node.InnerText);
                protocol.Deliver(response);
                return;
            }
            if (!network.IsConnected)
            {
                response = new ProtocolMain.Datagram("NETWORKINFO", "OFFLINE");
                response.Parameters.Add("network", node.InnerText);
                protocol.Deliver(response);
                return;
            }
            response = new ProtocolMain.Datagram("NETWORKINFO", "ONLINE");
            response.Parameters.Add("network", node.InnerText);
            response.Parameters.Add("nick", network.Nickname);
            protocol.Deliver(response);
        }

        public static void Raw(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 0)
            {
                string server = node.Attributes[0].Value;
                string priority = "Normal";
                libirc.Defs.Priority Priority = libirc.Defs.Priority.Normal;
                if (node.Attributes.Count > 1)
                {
                    priority = node.Attributes[1].Value;
                    switch (priority)
                    {
                        case "High":
                            Priority = libirc.Defs.Priority.High;
                            break;
                        case "Low":
                            Priority = libirc.Defs.Priority.Low;
                            break;
                    }
                }
                if (protocol.session.User.ContainsNetwork(server))
                {
                    Network network = protocol.session.User.RetrieveServer(server);
                    network.Transfer(node.InnerText, Priority);
                }
            }
        }

        public static void Nick(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            if (node.Attributes.Count > 0)
            {
                Network network = protocol.session.User.RetrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    response = new ProtocolMain.Datagram("NICK", network.Nickname);
                    response.Parameters.Add("network", network.ServerName);
                    protocol.Deliver(response);
                    return;
                }
                response = new ProtocolMain.Datagram("NICK", "UNKNOWN");
                response.Parameters.Add("network", network.ServerName);
                response.Parameters.Add("failure", "failure");
                protocol.Deliver(response);
            }
        }

        public static void BacklogRange(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 2)
            {
                Network network = protocol.session.User.RetrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    int from = int.Parse(node.Attributes[1].Value);
                    int to = int.Parse(node.Attributes[2].Value);
                    ProtocolIrc _protocol = (ProtocolIrc)network._Protocol;
                    _protocol.getRange(protocol, from, to);
                }
                else
                {
                    SystemLog.DebugLog("User " + protocol.session.IP + " requested log of unknown network");
                }
            }
            else
            {
                SystemLog.DebugLog("User " + protocol.session.IP + " requested log of unknown network");
            }
        }

        public static void BacklogSv(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 1)
            {
                Network network = protocol.session.User.RetrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    int mqid = 0;
                    if (node.Attributes.Count > 2)
                    {
                        mqid = int.Parse(node.Attributes[2].Value);
                    }
                    ProtocolIrc _protocol = (ProtocolIrc)network._Protocol;
                    _protocol.getDepth(int.Parse(node.Attributes[1].Value), protocol, mqid);
                    protocol.session.User.MessageBacklog(mqid, _protocol, protocol);
                }
                else
                {
                    SystemLog.DebugLog("User " + protocol.session.IP + " requested log of unknown network");
                }
            }
            else
            {
                SystemLog.DebugLog("User " + protocol.session.IP + " requested log of unknown network");
            }
        }

        public static void Load(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            response = new ProtocolMain.Datagram("LOAD", "Pidgeon service version " + Configuration.Services.PidgeonSvVersion + " I have " 
                + Session.ConnectedUsers.Count.ToString() + " connections, process info: memory usage " 
                + ((double)System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1024).ToString() 
                + "kb private and " 
                + ((double)System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64 / 1024).ToString() 
                + "kb virtual, uptime: " + (DateTime.Now - Core.StartTime).ToString());

            protocol.Deliver(response);
        }

        public static void Connect(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            if (!protocol.session.User.IsApproved(Permission.Connect))
            {
                // this user is not approved to create new connection to the system
                response = new ProtocolMain.Datagram("CONNECT", "PERMISSIONDENY");
                protocol.Deliver(response);
                return;
            }
            bool ssl = false;
            string server = node.InnerText;
            if (server.StartsWith("$"))
            {
                server = server.Substring(1);
                ssl = true;
            }
            if (protocol.session.User.ContainsNetwork(server))
            {
                response = new ProtocolMain.Datagram("CONNECT", "CONNECTED");
                response.Parameters.Add("network", server);
                protocol.Deliver(response);
                return;
            }
            int port = 6667;

            if (node.Attributes.Count > 0)
            {
                port = int.Parse(node.Attributes[0].Value);
            }

            if (server.Contains(":"))
            {
                port = int.Parse(server.Substring(server.IndexOf(":") + 1));
                server = server.Substring(0, server.IndexOf(":"));
            }

            protocol.session.User.ConnectIRC(server, port, ssl);
            Network network = protocol.session.User.RetrieveServer(server);
            SystemLog.WriteLine(protocol.session.IP + ": Connecting to " + server);
            response = new ProtocolMain.Datagram("CONNECT", "OK");
            response.Parameters.Add("network", server);
            if (network != null)
            {
                response.Parameters.Add("id", network.NetworkID);
            }
            protocol.Deliver(response);
        }

        public static void Remove(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            Network network = protocol.session.User.RetrieveServer(node.InnerText);
            if (network == null)
            {
                SystemLog.DebugLog("User " + protocol.session.User.UserName + " requested to disconnect a network, which is unknown: " + node.InnerText);
                response = new ProtocolMain.Datagram("FAIL", "REMOVE");
                response.Parameters.Add("network", node.InnerText);
                response.Parameters.Add("code", "20");
                response.Parameters.Add("description", "no such a network");
                protocol.Deliver(response);
                return;
            }
            ProtocolIrc IRC = (ProtocolIrc)network._Protocol;
            if (IRC == null)
            {
                response = new ProtocolMain.Datagram("FAIL", "REMOVE");
                response.Parameters.Add("network", node.InnerText);
                response.Parameters.Add("code", "20");
                response.Parameters.Add("description", "internal error (null pointer to network._Protocol)");
                SystemLog.Error("Unable to process disconnect request because there is null pointer to network._Protocol, requested network: " + node.InnerText);
                protocol.Deliver(response);
                return;
            }
            SystemLog.DebugLog("User " + protocol.session.User.UserName + " requested to disconnect a network: " + node.InnerText);
            IRC.Exit();
            lock (protocol.session.User.ConnectedNetworks)
            {
                if (protocol.session.User.ConnectedNetworks.Contains(network))
                {
                    SystemLog.DebugLog("User " + protocol.session.User.UserName + " removing network from memory: " + node.InnerText);
                    protocol.session.User.ConnectedNetworks.Remove(network);
                }
                else
                {
                    SystemLog.Error("Can't remove the network from system, error #2 (protocol.connection.User.ConnectedNetworks doesn't contain this network)");
                    return;
                }
            }
            response = new ProtocolMain.Datagram("REMOVE", node.InnerText);
            protocol.Deliver(response);
        }

        public static void GlobalIdent(XmlNode node, ProtocolMain protocol)
        {
            protocol.session.User.Ident = node.InnerText;
            protocol.Deliver(new ProtocolMain.Datagram("GLOBALIDENT", node.InnerText));
        }

        public static void Message(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 2)
            {
                Network network = protocol.session.User.RetrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    string target = node.Attributes[2].Value;
                    string ff = node.Attributes[1].Value;
                    libirc.Defs.Priority Priority = libirc.Defs.Priority.Normal;
                    switch (ff)
                    {
                        case "Low":
                            Priority = libirc.Defs.Priority.Low;
                            break;
                        case "High":
                            Priority = libirc.Defs.Priority.High;
                            break;
                    }
                    ProtocolMain.SelfData data = new ProtocolMain.SelfData(network, node.InnerText, DateTime.Now, target, network.getMQID());
                    protocol.session.User.Message(data);
                    protocol.session.User.MessageBack(data);
                    network.Message(node.InnerText, target, Priority);
                }
                else
                {
                    SystemLog.DebugLog("Network was not found for " + protocol.session.IP);
                }
            }
        }

        public static void GlobalNick(XmlNode node, ProtocolMain protocol)
        {
            if (String.IsNullOrEmpty(node.InnerText))
            {
                protocol.Deliver(new ProtocolMain.Datagram("GLOBALNICK", protocol.session.User.Nickname));
                return;
            }
            protocol.Deliver(new ProtocolMain.Datagram("GLOBALNICK", node.InnerText));
            protocol.session.User.Nickname = node.InnerText;
            Core.SaveUser();
        }

        public static void Auth(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string username = node.Attributes [0].Value;
            string pw = node.Attributes [1].Value;
            bool encrypted = false;
            if (!encrypted)
                pw = Core.CalculateMD5Hash(pw);
            lock (Core.UserList)
            {
                foreach (SystemUser curr_user in Core.UserList)
                {
                    if (curr_user.UserName == username)
                    {
                        if (curr_user.IsLocked)
                        {
                            response = new ProtocolMain.Datagram("AUTH", "LOCKED");
                            protocol.Deliver(response);
                            return;
                        }
                        if (curr_user.Password == pw)
                        {
                            protocol.session.User = curr_user;
                            SystemLog.WriteLine(protocol.session.IP + ": Logged in as " + protocol.session.User.UserName);
                            response = new ProtocolMain.Datagram("AUTH", "OK");
                            response.Parameters.Add("ls", "There is " + protocol.session.User.Clients.Count.ToString() + " connections logged in to this account");
                            protocol.session.status = Session.Status.Connected;
                            protocol.Deliver(response);
                            return;
                        }
                    }
                }
            }
            response = new ProtocolMain.Datagram("AUTH", "INVALID");
            protocol.Deliver(response);
        }

        public static void UserList(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response;
            Network nw = protocol.session.User.RetrieveServer(node.Attributes[0].Value);
            if (nw == null)
            {
                response = new ProtocolMain.Datagram("FAIL", "USERLIST");
                response.Parameters.Add("code", "3");
                response.Parameters.Add("description", "invalid network");
                protocol.Deliver(response);
                return;
            }
            libirc.Channel xx = nw.GetChannel(node.Attributes[1].Value);
            if (xx == null)
            {
                response = new ProtocolMain.Datagram("FAIL", "USERLIST");
                response.Parameters.Add("code", "3");
                response.Parameters.Add("description", "invalid channel");
                protocol.Deliver(response);
                return;
            }
            List<libirc.User> userlist = new List<libirc.User>(xx.RetrieveUL().Values);
            response = new ProtocolMain.Datagram("USERLIST");
            response.Parameters.Add("network", node.Attributes[0].Value);
            response.Parameters.Add("channel", node.Attributes[1].Value);
            response.Parameters.Add("uc", userlist.Count.ToString());
            int id = 0;
            foreach (libirc.User i in userlist)
            {
                response.Parameters.Add("nickname" + id.ToString(), i.Nick);
                response.Parameters.Add("realname" + id.ToString(), i.RealName);
                response.Parameters.Add("away" + id.ToString(), i.Away.ToString());
                response.Parameters.Add("awaymessage" + id.ToString(), i.AwayMessage);
                id++;
            }
            protocol.Deliver(response);
        }

        public static void NetworkList(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string networks = "";
            string id = "";
            lock (protocol.session.User.ConnectedNetworks)
            {
                foreach (Network current_net in protocol.session.User.ConnectedNetworks)
                {
                    networks += current_net.ServerName + "|";
                    id += current_net.NetworkID + "|";
                }
            }
            response = new ProtocolMain.Datagram("NETWORKLIST", networks);
            response.Parameters.Add("identification", id);
            protocol.Deliver(response);
        }

        private static void InvalidParameters(ProtocolMain protocol)
        {
            var response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
            response.Parameters.Add("code", "2");
            response.Parameters.Add("description", "invalid number of parameters");
            protocol.Deliver(response);
            return;
        }

        public static void Manage(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            switch (node.InnerText)
            {
                case "LIST":
                    if (!protocol.session.User.IsApproved(Permission.ListUsers))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }
                    string users = "";
                    lock (Core.UserList)
                    {
                        foreach (SystemUser curr in Core.UserList)
                        {
                            users += curr.UserName + ":" + curr.Nickname + ":" +curr.IsLocked.ToString() + 
                                     ":" +  curr.Role + "&";
                        }
                    }
                    response = new ProtocolMain.Datagram("USERLIST", users);
                    break;
                case "CREATEUSER":
                    if (!protocol.session.User.IsApproved(Permission.CreateUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }
                    if (node.Attributes.Count < 4)
                    {
                        InvalidParameters(protocol);
                        return;
                    }
                    if (SystemUser.IsValid(node.Attributes[0].Value))
                    {
                        string role = node.Attributes[3].Value;
                        if (SystemUser.CreateUser(node.Attributes[0].Value, node.Attributes[1].Value,
                                                  node.Attributes[2].Value, role,
                                                  node.Attributes[4].Value,
                                                  node.Attributes[5].Value))
                        {
                            response = new ProtocolMain.Datagram("SYSTEM", "CREATEUSER");
                            response.Parameters.Add("name", node.Attributes[0].Value);
                            response.Parameters.Add("result", "ok");
                            protocol.Deliver(response);
                            return;
                        }
                        response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                        response.Parameters.Add("code", "36");
                        response.Parameters.Add("description", "user already exist");
                        protocol.Deliver(response);
                        return;
                    }
                    response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                    response.Parameters.Add("code", "3");
                    response.Parameters.Add("description", "invalid name");
                    protocol.Deliver(response);
                    return;
                case "KILL":
                    if (!protocol.session.User.IsApproved(Permission.Kill))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }
                    if (node.Attributes.Count < 1)
                    {
                        InvalidParameters(protocol);
                        return;
                    }
                    ulong sessionID;
                    if (!ulong.TryParse(node.Attributes [0].Value, out sessionID))
                    {
                        InvalidParameters(protocol);
                        return;
                    }
                    Session session = Session.FromSID(sessionID);
                    if (session == null)
                    {
                        response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                        response.Parameters.Add("code", "21");
                        response.Parameters.Add("description", "there is no such a session");
                        protocol.Deliver(response);
                        return;
                    }
                    session.Kill();
                    response = new ProtocolMain.Datagram("SYSTEM", "KILL");
                    response.Parameters.Add("sid", node.Attributes [0].Value);
                    protocol.Deliver(response);
                    return;
                case "LOCK":
                    if (!protocol.session.User.IsApproved(Permission.LockUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }

                    if (node.Attributes.Count > 0)
                    {
                        string user = node.Attributes[0].Value;
                        SystemUser target = SystemUser.GetUser(user);
                        if (target == protocol.session.User)
                        {
                            response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                            response.Parameters.Add("code", "32");
                            response.Parameters.Add("description", "you can't lock yourself from system");
                            protocol.Deliver(response);
                            return;
                        }
                        if (target != null)
                        {
                            target.Lock();
                            Core.SaveUser();
                            response = new ProtocolMain.Datagram("SYSTEM", "LOCK");
                            response.Parameters.Add("username", user);
                            protocol.Deliver(response);
                            return;
                        }
                        else
                        {
                            response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                            response.Parameters.Add("code", "5");
                            response.Parameters.Add("description", "LOCK: invalid name");
                            protocol.Deliver(response);
                            return;
                        }
                    }
                    else
                    {
                        response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                        response.Parameters.Add("code", "5");
                        response.Parameters.Add("description", "LOCK: invalid name");
                        protocol.Deliver(response);
                        return;
                    }
                case "UNLOCK":
                    if (!protocol.session.User.IsApproved(Permission.UnlockUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }

                    if (node.Attributes.Count > 0)
                    {
                        string user = node.Attributes[0].Value;
                        SystemUser target = SystemUser.GetUser(user);
                        if (target != null)
                        {
                            target.Unlock();
                            Core.SaveUser();
                            response = new ProtocolMain.Datagram("SYSTEM", "UNLOCK");
                            response.Parameters.Add("username", user);
                        }
                        else
                        {
                            response = new ProtocolMain.Datagram("SYSTEM", "FAIL");
                            response.Parameters.Add("action", "un");
                            response.Parameters.Add("description", "unknown user");
                        }
                    }
                    else
                    {
                        response = new ProtocolMain.Datagram("SYSTEM", "FAIL");
                        response.Parameters.Add("action", "un");
                        response.Parameters.Add("description", "invalid");
                    }
                    break;
                case "REMOVE":
                    if (!protocol.session.User.IsApproved(Permission.DeleteUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }

                    if (node.Attributes.Count > 0 && node.Attributes[0].Name == "id")
                    {
                        string user = node.Attributes[0].Value;
                        SystemUser target = SystemUser.GetUser(user);
                        if (target == protocol.session.User)
                        {
                            response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                            response.Parameters.Add("code", "32");
                            response.Parameters.Add("description", "you can't remove yourself from users");
                            protocol.Deliver(response);
                            return;
                        }

                        if (target == null)
                        {
                            response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                            response.Parameters.Add("code", "7");
                            response.Parameters.Add("description", "unknown user");
                            protocol.Deliver(response);
                            return;
                        }
                        else
                        {
                            if (SystemUser.DeleteUser(target))
                            {
                                response = new ProtocolMain.Datagram("SYSTEM", "REMOVE");
                                response.Parameters.Add("id", node.Attributes[0].Value);
                                protocol.Deliver(response);
                            } else
                            {
                                response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                                response.Parameters.Add("code", "40");
                                response.Parameters.Add("description", "failed to delete user");
                                protocol.Deliver(response);
                            }
                            return;
                        }
                    }

                    response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                    response.Parameters.Add("code", "7");
                    response.Parameters.Add("description", "unknown user");
                    protocol.Deliver(response);
                    return;
                case "SESSION":
                    if (!protocol.session.User.IsApproved(Permission.DisplaySystemData))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }
                    response = new ProtocolMain.Datagram("SYSTEM", "SESSION");
                    string list = "";
                    lock (Session.ConnectedUsers)
                    {
                        foreach (Session si in Session.ConnectedUsers)
                        {
                            list += si.SessionID.ToString() + "&" + si.CreatedTime.ToBinary().ToString() + "&";
                            if (si.User == null)
                                list += "unknown";
                            else
                                list += si.User.UserName;
                            list += "&" + si.IP;
                            list += "&" + si.status.ToString();
                            list += "|";
                        }
                    }
                    response.Parameters.Add("list", list);
                    protocol.Deliver(response);
                    return;
                default:
                    response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                    response.Parameters.Add("code", "30");
                    response.Parameters.Add("description", "unknown request");
                    protocol.Deliver(response);
                    return;
            }
            protocol.Deliver(response);
        }

        public static void ChannelInfo(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            Network nw = protocol.session.User.RetrieveServer(node.Attributes[0].Value);
            switch (node.InnerText)
            {
                case "LIST":
                    if (nw == null)
                    {
                        protocol.Deliver(new ProtocolMain.Datagram("CHANNELINFO", "EMPTY"));
                        return;
                    }
                    string list = "";
                    lock (nw.Channels)
                    {
                        foreach (libirc.Channel curr in nw.Channels.Values)
                        {
                            if (curr.ChannelWork)
                            {
                                list += curr.Name + "!";
                            }
                        }
                    }
                    response = new ProtocolMain.Datagram("CHANNELINFO", "");
                    response.Parameters.Add("network", node.Attributes[0].Value);
                    response.Parameters.Add("channels", list);
                    protocol.Deliver(response);
                    break;
                case "INFO":
                    if (nw == null)
                    {
                        protocol.Deliver(new ProtocolMain.Datagram("CHANNELINFO", "EMPTY"));
                        return;
                    }
                    if (node.Attributes.Count > 1)
                    {
                        libirc.Channel channel = nw.GetChannel(node.Attributes[1].Value);
                        if (channel == null)
                        {
                            Console.WriteLine(nw.Channels.Count.ToString());
                            response = new ProtocolMain.Datagram("CHANNELINFO", "EMPTY");
                            response.Parameters.Add("network", node.Attributes[0].Value);
                            response.Parameters.Add("channel", node.Attributes[1].Value);
                            protocol.Deliver(response);
                            return;

                        }
                        string userli = "";
                        foreach (libirc.User l2 in channel.RetrieveUL().Values)
                        {
                            userli += l2.Nick + "!" + l2.Ident + "@" + l2.Host + l2.ChannelMode.ToString() + ":";
                        }
                        response = new ProtocolMain.Datagram("CHANNELINFO", "USERLIST");
                        response.Parameters.Add("network", node.Attributes[0].Value);
                        response.Parameters.Add("channel", node.Attributes[1].Value);
                        response.Parameters.Add("ul", userli);
                        Console.WriteLine("can't find " + node.Attributes[1].Value);
                        protocol.Deliver(response);
                    }
                    break;
            }
            return;
        }
    }
}
