//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Xml;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace pidgeon_sv
{
    public partial class ProtocolSv
    {
        private static class ResponsesSv
        {
            public static void sDebug(XmlNode curr, ProtocolSv protocol)
            {
                if (curr.InnerText == "THREAD")
                {
                    Console.WriteLine("+---------------------------------------------------------------------------+");
                    Console.WriteLine("|ID:   |name:          |priority:        |status:                           |");
                    Console.WriteLine("+---------------------------------------------------------------------------+");
                    foreach (XmlAttribute thread in curr.Attributes)
                    {
                        List<string> info = new List<string>(thread.Value.Split(':'));
                        if (info.Count < 3)
                            continue;
                        Console.WriteLine("|" + Terminal.FormatToSpecSize(thread.Name, 6) + "|" + Terminal.FormatToSpecSize(info[0], 14)
                                          + "|" + Terminal.FormatToSpecSize(info[1], 26)  + "|" + Terminal.FormatToSpecSize(info[2], 26) + "|");
                    }
                    Console.WriteLine("+---------------------------------------------------------------------------+");
                }
            }

            /// <summary>
            /// Load
            /// </summary>
            /// <param name="curr">Node</param>
            /// <param name="protocol">Protocol</param>
            public static void sLoad(XmlNode curr, ProtocolSv protocol)
            {
                SystemLog.WriteLine(curr.InnerText);
            }

            public static void sMaintenance(XmlNode curr, ProtocolSv protocol)
            {
                protocol.Respond = true;
                if (curr.InnerText == "REMOVE")
                {
                    if (curr.Attributes.Count > 0)
                    {
                        Console.WriteLine("User was deleted from system: " + curr.Attributes [0].Value);
                    }
                    return;
                }
                if (curr.InnerText == "CREATEUSER")
                {
                    if (curr.Attributes.Count > 0)
                    {
                        Console.WriteLine("User was created: " + curr.Attributes [0].Value);
                    }
                    return;
                }
                if (curr.InnerText == "UNLOCK")
                {
                    if (curr.Attributes.Count > 0)
                    {
                        Console.WriteLine("User was unlocked: " + curr.Attributes [0].Value);
                    }
                    return;
                }
                if (curr.InnerText == "LOCK")
                {
                    if (curr.Attributes.Count > 0)
                    {
                        Console.WriteLine("User was locked: " + curr.Attributes [0].Value);
                    }
                    return;
                }
                if (curr.InnerText == "KILL")
                {
                    if (curr.Attributes.Count > 0)
                    {
                        Console.WriteLine("Session was terminated: " + curr.Attributes [0].Value);
                    }
                    return;
                }
                if (curr.InnerText == "SESSION")
                {
                    protocol.Respond = true;
                    Console.WriteLine("\nSession list:\n");
                    List<string> Sessions = new List<string>(curr.Attributes["list"].Value.Split('|'));
                    if (Sessions.Count == 0)
                    {
                        Console.WriteLine("There are no sessions on this instance of services (weird heh)");
                        return;
                    }
                    Console.WriteLine("+---------------------------------------------------------------------------+");
                    Console.WriteLine("|ID:   |Username:     |Logged since:             |IP:                       |");
                    Console.WriteLine("+---------------------------------------------------------------------------+");
                    foreach (string session in Sessions)
                    {
                        if (session == "")
                        {
                            continue;
                        }
                        List<string> info = new List<string>(session.Split('&'));
                        Console.WriteLine("|" + Terminal.FormatToSpecSize(info[0] ,6) + "|" + Terminal.FormatToSpecSize(info[2], 14)
                                          + "|" + Terminal.FormatToSpecSize(DateTime.FromBinary(long.Parse(info[1])).ToString(), 26)
                                          + "|" + Terminal.FormatToSpecSize(info[3], 26) + "|");
                    }
                    Console.WriteLine("+---------------------------------------------------------------------------+");
                    }
            }

            public static void sUserList(XmlNode curr, ProtocolSv protocol)
            {
                protocol.Respond = true;
                Console.WriteLine("\nUser list:\n");
                List<string> Users = new List<string>(curr.InnerText.Split('&'));
                if (Users.Count == 0)
                {
                    Console.WriteLine("There are no users on this instance of services");
                    return;
                }
                Console.WriteLine("+---------------------------------------------------------------------------+");
                Console.WriteLine("|Username:              |Nickname:         |Status:  |Roles:                |");
                Console.WriteLine("+---------------------------------------------------------------------------+");
                foreach (string user in Users)
                {
                    if (user == "")
                    {
                        continue;
                    }
                    List<string> info = new List<string>(user.Split(':'));
                    if (info.Count < 4)
                    {
                        SystemLog.Error("Corrupted record for user: " + user);
                        return;
                    }
                    string username = info[0];
                    string nick = info[1];
                    string status = info[2];
                    if (status.ToLower() == "true")
                    {
                        status = "Locked";
                    } else
                    {
                        status = "Normal";
                    }
                    string roles = info[3];
                    Console.WriteLine("|" + Terminal.FormatToSpecSize(username, 23) + "|" +
                                      Terminal.FormatToSpecSize(nick, 18) + "|" +
                                      Terminal.FormatToSpecSize(status, 9) + "|" +
                                      Terminal.FormatToSpecSize(roles, 22) + "|");
                }
                Console.WriteLine("+---------------------------------------------------------------------------+");
            }

            /// <summary>
            /// Status
            /// </summary>
            /// <param name="curr">XmlNode</param>
            /// <param name="protocol">Protocol which owns this request</param>
            public static void sStatus(XmlNode curr, ProtocolSv protocol)
            {
                switch (curr.InnerText)
                {
                    case "Connected":
                        //protocol.ConnectionStatus = Status.Connected;
                        break;
                    case "WaitingPW":
                        //protocol.ConnectionStatus = Status.WaitingPW;
                        break;
                }
            }

            /// <summary>
            /// Error
            /// </summary>
            /// <param name="curr">XmlNode</param>
            /// <param name="protocol">Protocol which owns this request</param>
            public static void sError(XmlNode curr, ProtocolSv protocol)
            {
                string error = "Error ocurred on services: ";
                string code = "unknown";
                string description = "this is an unknown error";
                if (curr.Attributes != null)
                {
                    foreach (XmlAttribute xx in curr.Attributes)
                    {
                        switch (xx.Name.ToLower())
                        {
                            case "description":
                                description = xx.Value;
                                break;
                            case "code":
                                code = xx.Value;
                                break;
                        }
                    }
                }
                protocol.Respond = true;
                error += "code (" + code + ") description: " + description;
                SystemLog.Warning(error);
            }

            /// <summary>
            /// Auth
            /// </summary>
            /// <param name="curr">XmlNode</param>
            /// <param name="protocol">Protocol which owns this request</param>
            public static void sAuth(XmlNode curr, ProtocolSv protocol)
            {
                if (curr.InnerText == "INVALID")
                {
                    SystemLog.WriteLine("You have supplied wrong password, connection closed");
                    protocol.Disconnect();
                    protocol.Exit();
                }
                if (curr.InnerText == "OK")
                {
                    //protocol.ConnectionStatus = Status.Connected;
                    SystemLog.WriteLine("You are now logged in to pidgeon bnc");
                    SystemLog.WriteLine(curr.Attributes[0].Value);
                }
                return;
            }
        }
    }
}
