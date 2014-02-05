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
        public static void Status(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string info = protocol.connection.status.ToString();
            response = new ProtocolMain.Datagram("STATUS", info);
            protocol.Deliver(response);
        }

        public static void NetworkInfo(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            Network network = protocol.connection.User.retrieveServer(node.InnerText);
            if (network == null)
            {
                response = new ProtocolMain.Datagram("NETWORKINFO", "UNKNOWN");
                response.Parameters.Add("network", node.InnerText);
                protocol.Deliver(response);
                return;
            }
            if (network.Connected == false)
            {
                response = new ProtocolMain.Datagram("NETWORKINFO", "OFFLINE");
                response.Parameters.Add("network", node.InnerText);
                protocol.Deliver(response);
                return;
            }
            response = new ProtocolMain.Datagram("NETWORKINFO", "ONLINE");
            response.Parameters.Add("network", node.InnerText);
            protocol.Deliver(response);
        }

        public static void Raw(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 0)
            {
                string server = node.Attributes[0].Value;
                string priority = "Normal";
                ProtocolIrc.Priority Priority = ProtocolIrc.Priority.Normal;
                if (node.Attributes.Count > 1)
                {
                    priority = node.Attributes[1].Value;
                    switch (priority)
                    {
                        case "High":
                            Priority = ProtocolIrc.Priority.High;
                            break;
                        case "Low":
                            Priority = ProtocolIrc.Priority.Low;
                            break;
                    }
                }
                if (protocol.connection.User.containsNetwork(server))
                {
                    Network network = protocol.connection.User.retrieveServer(server);
                    network._Protocol.Transfer(node.InnerText, Priority);
                }
            }
        }

        public static void Nick(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            if (node.Attributes.Count > 0)
            {
                Network network = protocol.connection.User.retrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    response = new ProtocolMain.Datagram("NICK", network.nickname);
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
                Network network = protocol.connection.User.retrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    int from = int.Parse(node.Attributes[1].Value);
                    int to = int.Parse(node.Attributes[2].Value);
                    ProtocolIrc _protocol = (ProtocolIrc)network._Protocol;
                    _protocol.getRange(protocol, from, to);
                }
                else
                {
                    SystemLog.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
                }
            }
            else
            {
                SystemLog.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
            }
        }

        public static void BacklogSv(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 1)
            {
                Network network = protocol.connection.User.retrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    int mqid = 0;
                    if (node.Attributes.Count > 2)
                    {
                        mqid = int.Parse(node.Attributes[2].Value);
                    }
                    ProtocolIrc _protocol = (ProtocolIrc)network._Protocol;
                    _protocol.getDepth(int.Parse(node.Attributes[1].Value), protocol, mqid);
                    protocol.connection.User.MessageBacklog(mqid, _protocol, protocol);
                }
                else
                {
                    SystemLog.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
                }
            }
            else
            {
                SystemLog.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
            }
        }

        public static void Load(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            response = new ProtocolMain.Datagram("LOAD", "Pidgeon service version " + Configuration._System.PidgeonSvVersion + " I have " 
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
            if (!protocol.connection.User.IsApproved(Security.Permission.Connect))
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
            if (protocol.connection.User.containsNetwork(server))
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

            protocol.connection.User.ConnectIRC(server, port, ssl);
            Network network = protocol.connection.User.retrieveServer(server);
            SystemLog.WriteLine(protocol.connection.IP + ": Connecting to " + server);
            response = new ProtocolMain.Datagram("CONNECT", "OK");
            response.Parameters.Add("network", server);
            if (network != null)
            {
                response.Parameters.Add("id", network.NetworkID);
            }
            protocol.Deliver(response);
        }

        public static void DiscNw(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            Network network = protocol.connection.User.retrieveServer(node.InnerText);
            if (network == null)
            {
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
                SystemLog.Error("Unable to process disconnect request because there is null pointer to network._Protocol");
                protocol.Deliver(response);
                return;
            }
            IRC.Exit();
            lock (protocol.connection.User.ConnectedNetworks)
            {
                if (protocol.connection.User.ConnectedNetworks.Contains(network))
                {
                    protocol.connection.User.ConnectedNetworks.Remove(network);
                    response = new ProtocolMain.Datagram("REMOVE", node.InnerText);
                    protocol.Deliver(response);
                }
                else
                {
                    SystemLog.Error("Can't remove the network from system, error #2 " +
                                    "(protocol.connection.User.ConnectedNetworks doesn't contain the network)");
                }
            }
        }

        public static void GlobalIdent(XmlNode node, ProtocolMain protocol)
        {
            protocol.connection.User.Ident = node.InnerText;
            protocol.Deliver(new ProtocolMain.Datagram("GLOBALIDENT", node.InnerText));
        }

        public static void Message(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 2)
            {
                Network network = protocol.connection.User.retrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    string target = node.Attributes[2].Value;
                    string ff = node.Attributes[1].Value;
                    ProtocolIrc.Priority Priority = ProtocolIrc.Priority.Normal;
                    switch (ff)
                    {
                        case "Low":
                            Priority = ProtocolIrc.Priority.Low;
                            break;
                        case "High":
                            Priority = ProtocolIrc.Priority.High;
                            break;
                    }
                    ProtocolMain.SelfData data = new ProtocolMain.SelfData(network, node.InnerText, DateTime.Now, target, network._Protocol.getMQID());
                    protocol.connection.User.Message(data);
                    protocol.connection.User.MessageBack(data);
                    network._Protocol.Message(node.InnerText, target, Priority);
                }
                else
                {
                    SystemLog.DebugLog("Network was not found for " + protocol.connection.IP);
                }
            }
        }

        public static void GlobalNick(XmlNode node, ProtocolMain protocol)
        {
            if (node.InnerText == "")
            {
                protocol.Deliver(new ProtocolMain.Datagram("GLOBALNICK", protocol.connection.User.Nickname));
                return;
            }
            protocol.Deliver(new ProtocolMain.Datagram("GLOBALNICK", node.InnerText));
            protocol.connection.User.Nickname = node.InnerText;
            Core.SaveUser();
        }

        public static void Auth(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string username = node.Attributes [0].Value;
            string pw = node.Attributes [1].Value;
            bool encrypted = false;
            if (!encrypted)
            {
                pw = Core.CalculateMD5Hash(pw);
            }
            lock (Core.UserList)
            {
                foreach (SystemUser curr_user in Core.UserList)
                {
                    if (curr_user.UserName == username)
                    {
                        if (curr_user.Password == pw)
                        {
                            protocol.connection.User = curr_user;
                            lock (protocol.connection.User.Clients)
                            {
                                protocol.connection.User.Clients.Add(protocol);
                            }
                            SystemLog.WriteLine(protocol.connection.IP + ": Logged in as " + protocol.connection.User.UserName);
                            response = new ProtocolMain.Datagram("AUTH", "OK");
                            response.Parameters.Add("ls", "There is " + protocol.connection.User.Clients.Count.ToString() + " connections logged in to this account");
                            protocol.connection.status = Session.Status.Connected;
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
            Network nw = protocol.connection.User.retrieveServer(node.Attributes[0].Value);
            if (nw == null)
            {
                response = new ProtocolMain.Datagram("FAIL", "USERLIST");
                response.Parameters.Add("code", "3");
                response.Parameters.Add("description", "invalid network");
                protocol.Deliver(response);
                return;
            }
            Channel xx = nw.getChannel(node.Attributes[1].Value);
            if (xx == null)
            {
                response = new ProtocolMain.Datagram("FAIL", "USERLIST");
                response.Parameters.Add("code", "3");
                response.Parameters.Add("description", "invalid channel");
                protocol.Deliver(response);
                return;
            }
            response = new ProtocolMain.Datagram("USERLIST");
            lock (xx.UserList)
            {
                response.Parameters.Add("network", node.Attributes[0].Value);
                response.Parameters.Add("channel", node.Attributes[1].Value);
                response.Parameters.Add("uc", xx.UserList.Count.ToString());
                int id = 0;
            
                foreach (User i in xx.UserList)
                {
                    response.Parameters.Add("nickname" + id.ToString(), i.Nick);
                    response.Parameters.Add("realname" + id.ToString(), i.RealName);
                    response.Parameters.Add("away" + id.ToString(), i.Away.ToString());
                    response.Parameters.Add("awaymessage" + id.ToString(), i.AwayMessage);
                    id++;
                }
            }
            protocol.Deliver(response);
        }

        public static void NetworkList(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string networks = "";
            string id = "";
            lock (protocol.connection.User.ConnectedNetworks)
            {
                foreach (Network current_net in protocol.connection.User.ConnectedNetworks)
                {
                    networks += current_net.ServerName + "|";
                    id += current_net.NetworkID + "|";
                }
            }
            response = new ProtocolMain.Datagram("NETWORKLIST", networks);
            response.Parameters.Add("identification", id);
            protocol.Deliver(response);
        }

        public static void Manage(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            switch (node.Value)
            {
                case "LIST":
                    if (!protocol.connection.User.IsApproved(Security.Permission.ListUsers))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }
                    string users = "";
                    lock (Core.UserList)
                    {
                        foreach (SystemUser curr in Core.UserList)
                        {
                            users += curr.UserName + ":" + curr.Nickname + ":" + curr.IsLocked.ToString() + "&";
                        }
                    }
                    response = new ProtocolMain.Datagram("USERLIST", users);
                    break;
                case "CREATEUSER":
                    if (!protocol.connection.User.IsApproved(Security.Permission.CreateUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }

                    if (node.Attributes.Count < 4)
                    {
                        response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                        response.Parameters.Add("code", "2");
                        response.Parameters.Add("description", "invalid number of parameters");
                        protocol.Deliver(response);
                        return;
                    }

                    if (SystemUser.IsValid(node.Attributes[0].Value))
                    {
                        //SystemUser.CreateUser(node.Attributes[0].Value, node.Attributes[1].Value, node.Attributes[2].Value, level, node.Attributes[4].Value, node.Attributes[5].Value);

                        //response = new ProtocolMain.Datagram("SYSTEM", "CREATEUSER");
                        //response.Parameters.Add("name", node.Attributes[0].Value);
                        //response.Parameters.Add("result", "ok");
                        response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                        response.Parameters.Add("code", "6");
                        response.Parameters.Add("description", "not implemented");
                        protocol.Deliver(response);
                    }

                    response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                    response.Parameters.Add("code", "3");
                    response.Parameters.Add("description", "invalid name");
                    protocol.Deliver(response);

                    return;
                case "LOCKUSER":
                    if (!protocol.connection.User.IsApproved(Security.Permission.LockUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }

                    if (node.Attributes.Count > 0)
                    {
                        string user = node.Attributes[0].Value;
                        SystemUser target = SystemUser.getUser(user);
                        if (target != null)
                        {
                            target.Lock();
                            Core.SaveUser();
                            response = new ProtocolMain.Datagram("SYSTEM", "LOCKED");
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
                    if (!protocol.connection.User.IsApproved(Security.Permission.UnlockUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }

                    if (node.Attributes.Count > 0)
                    {
                        string user = node.Attributes[0].Value;
                        SystemUser target = SystemUser.getUser(user);
                        if (target != null)
                        {
                            target.Unlock();
                            Core.SaveUser();
                            response = new ProtocolMain.Datagram("SYSTEM", "UNLOCKED");
                            response.Parameters.Add("username", user);
                        }
                        else
                        {
                            response = new ProtocolMain.Datagram("SYSTEM", "FAIL");
                            response.Parameters.Add("action", "un");
                            response.Parameters.Add("explanation", "unknown user");
                        }
                    }
                    else
                    {
                        response = new ProtocolMain.Datagram("SYSTEM", "FAIL");
                        response.Parameters.Add("action", "un");
                        response.Parameters.Add("explanation", "invalid");
                    }
                    break;
                case "REMOVE":
                    if (!protocol.connection.User.IsApproved(Security.Permission.DeleteUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }

                    if (node.Attributes.Count > 0)
                    {
                        string user = node.Attributes[0].Value;
                        SystemUser target = SystemUser.getUser(user);
                        if (target != null)
                        {
                            response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                            response.Parameters.Add("code", "7");
                            response.Parameters.Add("explanation", "unknown user");
                            protocol.Deliver(response);
                            return;
                        }
                        else
                        {
                            SystemUser.DeleteUser(target);
                            response = new ProtocolMain.Datagram("SYSTEM", "REMOVE");
                            response.Parameters.Add("name", node.Attributes[0].Value);
                            protocol.Deliver(response);
                            return;
                        }
                    }

                    response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                    response.Parameters.Add("code", "7");
                    response.Parameters.Add("explanation", "unknown user");
                    protocol.Deliver(response);
                    return;
            }
            protocol.Deliver(response);
        }

        public static void ChannelInfo(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            Network nw = protocol.connection.User.retrieveServer(node.Attributes[0].Value);
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
                        foreach (Channel curr in nw.Channels)
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
                        Channel channel = nw.getChannel(node.Attributes[1].Value);
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
                        foreach (User l2 in channel.UserList)
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
