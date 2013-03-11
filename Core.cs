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
using System.IO;
using System.Net;
using System.Xml;
using System.Threading;
using System.Text;

namespace pidgeon_sv
{
    class Core
    {
        public static bool running = true;

        public static DateTime StartedTime;

        public static List<Account> _accounts = new List<Account>();
        public static List<Thread> threads = new List<Thread>();

        public static void Quit()
        {
            SL("Killing all connections and running processes");
            foreach (Thread curr in threads)
            {
                curr.Abort();
            }
            SL("Exiting");
        }

        public static void handleException(Exception reason, bool ThreadOK = false)
        {
            if (reason.GetType() == typeof(ThreadAbortException) && ThreadOK)
            {
                return;
            }
            SL("Exception: " + reason.Message + " " + reason.StackTrace + " in: " + reason.Source);
        }

        public static void DebugLog(string text)
        {
            SL("DEBUG: " + text);
        }

        public static void SaveData()
        {
            try
            {
                lock (_accounts)
                {
                    System.Xml.XmlDocument config = new System.Xml.XmlDocument();
                    foreach (Account user in _accounts)
                    {
                        System.Xml.XmlNode xmlnode = config.CreateElement("user");
                        XmlAttribute name = config.CreateAttribute("name");
                        XmlAttribute pw = config.CreateAttribute("password");
                        XmlAttribute nickname = config.CreateAttribute("nickname");
                        XmlAttribute ident = config.CreateAttribute("ident");
                        XmlAttribute realname = config.CreateAttribute("realname");
                        XmlAttribute level = config.CreateAttribute("level");
                        XmlAttribute locked = config.CreateAttribute("locked");
                        name.Value = user.username;
                        pw.Value = user.password;
                        nickname.Value = user.nickname;
                        ident.Value = user.ident;
                        level.Value = user.Level.ToString();
                        locked.Value = user.Locked.ToString();
                        xmlnode.Attributes.Append(name);
                        xmlnode.Attributes.Append(pw);
                        xmlnode.Attributes.Append(nickname);
                        xmlnode.Attributes.Append(ident);
                        xmlnode.Attributes.Append(locked);
                        config.AppendChild(xmlnode);
                    }
                    config.Save(Config.UserFile);
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
        }


        /// <summary>
        /// Load all user data and info
        /// </summary>
        public static void LoadUser()
        {
            try
            {
                SL("Loading users");
                if (File.Exists(Config.UserFile))
                {
                    XmlDocument configuration = new XmlDocument();
                    configuration.Load(Config.UserFile);
                    foreach (XmlNode curr in configuration.ChildNodes[0].ChildNodes)
                    {
                        Account.UserLevel UserLevel = Account.UserLevel.User;
                        bool locked = false;
                        string name = null;
                        string password = null;
                        string nickname = null;
                        string ident = null;
                        string realname = null;
                        if (Config.Rooted)
                        {
                            UserLevel = Account.UserLevel.Root;
                        }
                        foreach (XmlAttribute configitem in curr.Attributes)
                        {
                            switch (configitem.Name.ToLower())
                            {
                                case "name":
                                    name = configitem.Value;
                                    break;
                                case "password":
                                    password = configitem.Value;
                                    break;
                                case "locked":
                                    locked = bool.Parse(configitem.Value);
                                    break;
                                case "nickname":
                                    nickname = configitem.Value;
                                    break;
                                case "ident":
                                    ident = configitem.Value;
                                    break;
                                case "realname":
                                    realname = configitem.Value;
                                    break;
                                case "level":
                                    switch (configitem.Value)
                                    {
                                        case "Root":
                                            UserLevel = Account.UserLevel.Root;
                                            break;
                                        case "Admin":
                                            UserLevel = Account.UserLevel.Admin;
                                            break;
                                        case "User":
                                            UserLevel = Account.UserLevel.User;
                                            break;
                                    }
                                    break;
                            }
                        }
                        if (name == null || password == null)
                        {
                            SL("Invalid record for some user, skipped");
                            continue;
                        }
                        Account line = new Account(name, password);
                        line.nickname = nickname;
                        line.Locked = locked;
                        line.ident = ident;
                        line.realname = realname;
                        line.Level = UserLevel;
                        _accounts.Add(line);
                    }
                    SL("Loaded users: " + _accounts.Count.ToString());
                }
                else
                {
                    SL("There is no userfile for this instance, create one using parameter --manage");
                }
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public static void SaveUser()
        {
            try
            {
                if (File.Exists(Config.UserFile))
                {
                    File.Copy(Config.UserFile, Config.UserFile + "~", true);
                }

            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                File.Copy(Config.UserFile + "~", Config.UserFile, true);
            }
        }

        public static void LoadConf()
        {
            if (!File.Exists(Config.File))
            {
                SL("WARNING: there is no configuration file");
                return;
            }
            else
            {
                XmlDocument config = new XmlDocument();
                config.Load(Config.File);
                foreach (XmlNode curr in config.ChildNodes[0].ChildNodes)
                {
                    int value = 0;
                    switch (curr.Name.ToLower())
                    {
                        case "databasefolder":
                            Config.DatabaseFolder = curr.InnerText;
                            break;
                        case "serverport":
                            Config.server_port = int.Parse(curr.InnerText);
                            break;
                        case "chunksize":
                            value = int.Parse(curr.InnerText);
                            if (value < 100)
                            {
                                SL("Invalid chunk size, using default: " + Config.ChunkSize);
                                break;
                            }
                            Config.ChunkSize = value;
                            break;
                        case "mode":
                            Config.Mode mode = Config.Mode.Core;
                            if (curr.InnerText == "bouncer")
                            {
                                mode = Config.Mode.Bouncer;
                            }
                            Config.mode = mode;
                            break;
                    }
                }
            }
        }

        public static void SL(string text)
        {
            Console.WriteLine(DateTime.Now.ToString() + ": " + text);
        }

        public static bool Init()
        {
            try
            {
                SL("Pidgeon services " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " loading");
                SL("OS: " + Environment.OSVersion.ToString());

                LoadConf();

                Config.UserFile = Config.DatabaseFolder + Path.DirectorySeparatorChar + "users";

                if (!Directory.Exists("db"))
                {
                    Directory.CreateDirectory("db");
                }

                SL("This instance of pidgeon services has following parameters:");
                SL("-----------------------------------------------------------");
                SL("Port: " + Config.server_port.ToString());
                if (Config.MaxFileChunkSize == 0)
                {
                    SL("Maximum file chunk size: unlimited");
                }
                else
                {
                    SL("Maximum file chunk size: " + Config.MaxFileChunkSize.ToString());
                }
                SL("Mode: " + Config.mode.ToString());
                SL("Minimum buffer size: " + Config.minbs.ToString());
                SL("Minimum chunk size: " + Config.ChunkSize.ToString());

                SL("-----------------------------------------------------------");

                LoadUser();

                return true;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                SL("Fatal error - exiting");
                return false;
            }
        }

        public static void Listen()
        {
            try
            {
                SL("Waiting for clients");

                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Any, Config.server_port);
                server.Start();

                while (running)
                {
                    try
                    {
                        System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                        Thread _client = new Thread(Connection.InitialiseClient);
                        threads.Add(_client);
                        _client.Start(connection);
                        System.Threading.Thread.Sleep(200);
                    }
                    catch (Exception fail)
                    {
                        Core.handleException(fail);
                    }
                }
            }
            catch (Exception fail)
            {
                handleException(fail);
                SL("Terminating");
                return;
            }
        }
    }
}
