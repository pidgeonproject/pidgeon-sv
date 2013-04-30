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
            Network network = protocol.connection.account.retrieveServer(node.InnerText);
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
                int depth = 0;
                if (node.Attributes.Count > 2)
                {
                    depth = int.Parse(node.Attributes[2].Value);
                }
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
                if (protocol.connection.account.containsNetwork(server))
                {
                    Network network = protocol.connection.account.retrieveServer(server);
                    if (depth > 0)
                    {

                    }
                    network._protocol.Transfer(node.InnerText, Priority);
                }
            }
        }

        public static void Nick(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            if (node.Attributes.Count > 0)
            {
                Network b008 = protocol.connection.account.retrieveServer(node.Attributes[0].Value);
                if (b008 != null)
                {
                    response = new ProtocolMain.Datagram("NICK", b008.nickname);
                    response.Parameters.Add("network", b008.server);
                    protocol.Deliver(response);
                    return;
                }
                response = new ProtocolMain.Datagram("NICK", "UNKNOWN");
                response.Parameters.Add("network", b008.server);
                response.Parameters.Add("failure", "failure");
                protocol.Deliver(response);
            }
        }

        public static void BacklogRange(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 2)
            {
                Network network = protocol.connection.account.retrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    int from = int.Parse(node.Attributes[1].Value);
                    int to = int.Parse(node.Attributes[2].Value);
                    ProtocolIrc _protocol = (ProtocolIrc)network._protocol;
                    _protocol.getRange(protocol, from, to);
                }
                else
                {
                    Core.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
                }
            }
            else
            {
                Core.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
            }
        }

        public static void BacklogSv(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 1)
            {
                Network network = protocol.connection.account.retrieveServer(node.Attributes[0].Value);
                if (network != null)
                {
                    int mqid = 0;
                    if (node.Attributes.Count > 2)
                    {
                        mqid = int.Parse(node.Attributes[2].Value);
                    }
                    ProtocolIrc _protocol = (ProtocolIrc)network._protocol;
                    _protocol.getDepth(int.Parse(node.Attributes[1].Value), protocol, mqid);
                    protocol.connection.account.MessageBacklog(mqid, _protocol, protocol);
                }
                else
                {
                    Core.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
                }
            }
            else
            {
                Core.DebugLog("User " + protocol.connection.IP + " requested log of unknown network");
            }
        }

        public static void Load(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            response = new ProtocolMain.Datagram("LOAD", "Pidgeon service version " + Config.version + " supported mode=ns I have " + Connection.ActiveUsers.Count.ToString() + " connections, process info: memory usage " + ((double)System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1024).ToString() + "kb private and " + ((double)System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64 / 1024).ToString() + "kb virtual, uptime: " + (DateTime.Now - Core.StartedTime).ToString());

            protocol.Deliver(response);
        }

        public static void Connect(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            bool ssl = false;
            string server = node.InnerText;
            if (server.StartsWith("$"))
            {
                server = server.Substring(1);
                ssl = true;
            }
            if (protocol.connection.account.containsNetwork(server))
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

            protocol.connection.account.ConnectIRC(server, port, ssl);
            Network network = protocol.connection.account.retrieveServer(server);
            Core.SL(protocol.connection.IP + ": Connecting to " + server);
            response = new ProtocolMain.Datagram("CONNECT", "OK");
            response.Parameters.Add("network", server);
            if (network != null)
            {
                response.Parameters.Add("id", network.id);
            }
            protocol.Deliver(response);
        }

        public static void DiscNw(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            Network network = protocol.connection.account.retrieveServer(node.InnerText);
            if (network == null)
            {
                response = new ProtocolMain.Datagram("REMOVE", "FAIL");
                response.Parameters.Add("network", node.InnerText);
                protocol.Deliver(response);
                return;
            }
            ProtocolIrc IRC = (ProtocolIrc)network._protocol;
            IRC.Exit();
            lock (protocol.connection.account.ConnectedNetworks)
            {
                if (protocol.connection.account.ConnectedNetworks.Contains(network))
                {
                    protocol.connection.account.ConnectedNetworks.Remove(network);
                    response = new ProtocolMain.Datagram("REMOVE", node.InnerText);
                    protocol.Deliver(response);
                }
                else
                {
                    Core.DebugLog("Can't remove the protocol from system, error #2");
                }
            }
        }

        public static void GlobalIdent(XmlNode node, ProtocolMain protocol)
        {
            protocol.connection.account.ident = node.InnerText;
            protocol.Deliver(new ProtocolMain.Datagram("GLOBALIDENT", node.InnerText));
        }

        public static void Message(XmlNode node, ProtocolMain protocol)
        {
            if (node.Attributes.Count > 2)
            {
                Network network = protocol.connection.account.retrieveServer(node.Attributes[0].Value);
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
                    ProtocolMain.SelfData data = new ProtocolMain.SelfData(network, node.InnerText, DateTime.Now, target, network._protocol.getMQID());
                    protocol.connection.account.Message(data);
                    protocol.connection.account.MessageBack(data);
                    network._protocol.Message(node.InnerText, target, Priority);
                }
                else
                {
                    Core.DebugLog("Network was not found for " + protocol.connection.IP);
                }
            }
        }

        public static void GlobalNick(XmlNode node, ProtocolMain protocol)
        {
            if (node.InnerText == "")
            {
                protocol.Deliver(new ProtocolMain.Datagram("GLOBALNICK", protocol.connection.account.nickname));
                return;
            }
            protocol.Deliver(new ProtocolMain.Datagram("GLOBALNICK", node.InnerText));
            protocol.connection.account.nickname = node.InnerText;
            Core.SaveUser();
        }

        public static void Auth(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string username = node.Attributes[0].Value;
            string pw = node.Attributes[1].Value;
            lock (Core._accounts)
            {
                foreach (SystemUser curr_user in Core._accounts)
                {
                    if (curr_user.username == username)
                    {
                        if (curr_user.password == pw)
                        {
                            protocol.connection.account = curr_user;
                            lock (protocol.connection.account.Clients)
                            {
                                protocol.connection.account.Clients.Add(protocol);
                            }
                            Core.SL(protocol.connection.IP + ": Logged in as " + protocol.connection.account.username);
                            response = new ProtocolMain.Datagram("AUTH", "OK");
                            response.Parameters.Add("ls", "There is " + protocol.connection.account.Clients.Count.ToString() + " connections logged in to this account");
                            protocol.connection.status = Connection.Status.Connected;
                            protocol.Deliver(response);
                            return;
                        }
                    }
                }
            }
            response = new ProtocolMain.Datagram("AUTH", "INVALID");
            protocol.Deliver(response);
        }

        public static void NetworkList(XmlNode node, ProtocolMain protocol)
        {
            ProtocolMain.Datagram response = null;
            string networks = "";
            string id = "";
            lock (protocol.connection.account.ConnectedNetworks)
            {
                foreach (Network current_net in protocol.connection.account.ConnectedNetworks)
                {
                    networks += current_net.server + "|";
                    id += current_net.id + "|";
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
                    if (!SecurityLayers.isAuthorized(protocol.connection.account, SecurityLayers.ReadUser))
                    {
                        response = new ProtocolMain.Datagram("DENIED", "LIST");
                        break;
                    }
                    string users = "";
                    lock (Core._accounts)
                    {
                        foreach (SystemUser curr in Core._accounts)
                        {
                            users += curr.username + ":" + curr.nickname + ":" + curr.Locked.ToString() + "&";
                        }
                    }
                    response = new ProtocolMain.Datagram("USERLIST", users);
                    break;
                case "CREATEUSER":
                    if (!SecurityLayers.isAuthorized(protocol.connection.account, SecurityLayers.CreateUser))
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

                    if (SystemUser.isValid(node.Attributes[0].Value))
                    {
                        SystemUser.UserLevel level = SystemUser.UserLevel.User;

                        switch (node.Attributes[3].Value.ToUpper())
                        {
                            case "USER":
                                level = SystemUser.UserLevel.User;
                                break;
                            case "ADMIN":
                                level = SystemUser.UserLevel.Admin;
                                break;
                            case "ROOT":
                                level = SystemUser.UserLevel.Root;
                                break;
                        }

                        SystemUser.CreateEntry(node.Attributes[0].Value, node.Attributes[1].Value, node.Attributes[2].Value, level, node.Attributes[4].Value, node.Attributes[5].Value);

                        response = new ProtocolMain.Datagram("SYSTEM", "CREATEUSER");
                        response.Parameters.Add("name", node.Attributes[0].Value);
                        response.Parameters.Add("result", "ok");
                        protocol.Deliver(response);
                    }

                    response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                    response.Parameters.Add("code", "3");
                    response.Parameters.Add("description", "invalid name");
                    protocol.Deliver(response);

                    return;
                case "LOCKUSER":
                    if (!SecurityLayers.isAuthorized(protocol.connection.account, SecurityLayers.ModifyUser))
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
                            target.Locked = true;
                            Core.SaveUser();
                            response = new ProtocolMain.Datagram("SYSTEM", "LK");
                            response.Parameters.Add("username", user);
                            protocol.Deliver(response);
                            return;
                        }
                        else
                        {
                            response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                            response.Parameters.Add("code", "5");
                            response.Parameters.Add("description", "LK: invalid name");
                            protocol.Deliver(response);
                            return;
                        }
                    }
                    else
                    {
                        response = new ProtocolMain.Datagram("FAIL", "SYSTEM");
                        response.Parameters.Add("code", "5");
                        response.Parameters.Add("description", "LK: invalid name");
                        protocol.Deliver(response);
                        return;
                    }
                case "UNLOCK":
                    if (!SecurityLayers.isAuthorized(protocol.connection.account, SecurityLayers.ModifyUser))
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
                            target.Locked = false;
                            Core.SaveUser();
                            response = new ProtocolMain.Datagram("SYSTEM", "UN");
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
                    if (!SecurityLayers.isAuthorized(protocol.connection.account, SecurityLayers.DeleteUser))
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
            Network b002 = protocol.connection.account.retrieveServer(node.Attributes[0].Value);
            switch (node.InnerText)
            {
                case "LIST":
                    if (b002 == null)
                    {
                        protocol.Deliver(new ProtocolMain.Datagram("CHANNELINFO", "EMPTY"));
                        return;
                    }
                    string list = "";
                    lock (b002.Channels)
                    {
                        foreach (Channel curr in b002.Channels)
                        {
                            if (curr.ok)
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
                    if (b002 == null)
                    {
                        protocol.Deliver(new ProtocolMain.Datagram("CHANNELINFO", "EMPTY"));
                        return;
                    }
                    if (node.Attributes.Count > 1)
                    {
                        Channel channel = b002.getChannel(node.Attributes[1].Value);
                        if (channel == null)
                        {
                            Console.WriteLine(b002.Channels.Count.ToString());
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
