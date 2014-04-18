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
    public partial class Core
    {
        /// <summary>
        /// Filesystem watcher
        /// </summary>
        public static FileSystemWatcher fs;

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            SystemLog.Warning("the user file was changed, reloading it");
            LoadUser();
        }

        /// <summary>
        /// Load all user data and info
        /// </summary>
        public static void LoadUser(bool QietMode = false)
        {
            try
            {
                if (!QietMode)
                {
                    SystemLog.WriteLine("Loading users");
                }
                if (File.Exists(Configuration._System.UserFile))
                {
                    if (!QietMode)
                    {
                        fs = new FileSystemWatcher();
                        fs.Path = Configuration._System.ConfigurationFolder;
                        fs.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                        fs.Filter = Configuration._System.UserFile;
                        fs.Changed += new FileSystemEventHandler(OnChanged);
                        fs.Created += new FileSystemEventHandler(OnChanged);
                        fs.Deleted += new FileSystemEventHandler(OnChanged);
                        fs.EnableRaisingEvents = true;
                    }
                    XmlDocument configuration = new XmlDocument();
                    configuration.Load(Configuration._System.UserFile);
                    if (!(configuration.ChildNodes.Count > 0))
                    {
                        SystemLog.Warning("There is no proper information about users in config file");
                        return;
                    }
                    lock (UserList)
                    {
                        SystemLog.DebugLog("Loading users: " + configuration.ChildNodes[0].ChildNodes.Count.ToString(), 2);
                        foreach (XmlNode curr in configuration.ChildNodes[0].ChildNodes)
                        {
                            bool locked = false;
                            string name = null;
                            string password = null;
                            string nickname = null;
                            string ident = "pidgeon";
                            string realname = "http://pidgeonclient.org/wiki";
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
                                }
                            }
                            if (name == null || password == null)
                            {
                                SystemLog.DebugLog("Invalid record for some user, skipped");
                                continue;
                            }
                            bool Nonexistent = false;
                            SystemUser user = SystemUser.getUser(name);
                            if (user == null)
                            {
                                Nonexistent = true;
                                user = new SystemUser(name, password, QietMode);
                            }
                            else
                            {
                                if (user.IsLocked != locked)
                                {
                                    if (locked)
                                    {
                                        // we need to lock this user
                                        SystemLog.DebugLog("Locking user: " + name);
                                        user.Lock();
                                    }
                                    else
                                    {
                                        user.Unlock();
                                    }
                                }
                            }
                            user.Password = password;
                            user.Nickname = nickname;
                            user.Ident = ident;
                            user.RealName = realname;
                            if (curr.ChildNodes.Count > 0)
                            {
                                // read the roles this user has
                                foreach (XmlNode role in curr.ChildNodes)
                                {
                                    if (role.Name == "role")
                                    {
                                        user.Role = role.InnerText;
                                    }
                                }
                            } else
                            {
                                if (Configuration._System.Rooted)
                                {
                                    SystemLog.Warning("User " + user.UserName + " doesn't have any roles and because system is running in rooted mode, he was permanently granted root");
                                    user.Role = "root";
                                } else
                                {
                                    SystemLog.Warning("User " + user.UserName + " doesn't have any roles");
                                }
                            }
                            if (Nonexistent)
                            {
                                UserList.Add(user);
                            }
                        }
                        if (!QietMode)
                        {
                            SystemLog.WriteLine("Loaded users: " + UserList.Count.ToString());
                        }
                    }
                }
                else
                {
                    if (!QietMode)
                    {
                        SystemLog.Warning("There is no userfile for this instance, create one using parameter --install");
                    }
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
                if (fs != null)
                {
                    fs.EnableRaisingEvents = false;
                }
                if (File.Exists(Configuration._System.UserFile))
                {
                    File.Copy(Configuration._System.UserFile, Configuration._System.UserFile + "~", true);
                }
                XmlDocument configuration = new XmlDocument();

                XmlNode xmlnode = configuration.CreateElement("users");
                lock (UserList)
                {
                    foreach (SystemUser user in UserList)
                    {
                        XmlNode item = configuration.CreateElement("user");
                        XmlAttribute name = configuration.CreateAttribute("name");
                        XmlAttribute password = configuration.CreateAttribute("password");
                        XmlAttribute nick = configuration.CreateAttribute("nickname");
                        XmlAttribute ident = configuration.CreateAttribute("ident");
                        XmlAttribute realname = configuration.CreateAttribute("realname");
                        XmlAttribute locked = configuration.CreateAttribute("locked");
                        name.Value = user.UserName;
                        password.Value = user.Password;
                        nick.Value = user.Nickname;
                        ident.Value = user.Ident;
                        realname.Value = user.RealName;
                        locked.Value = user.IsLocked.ToString();
                        item.Attributes.Append(name);
                        item.Attributes.Append(password);
                        item.Attributes.Append(nick);
                        item.Attributes.Append(ident);
                        item.Attributes.Append(realname);
                        item.Attributes.Append(locked);
                        XmlNode role = configuration.CreateElement("role");
                        role.InnerText = user.Role;
                        item.AppendChild(role);
                        xmlnode.AppendChild(item);
                    }
                }

                configuration.AppendChild(xmlnode);
                configuration.Save(Configuration._System.UserFile);
                File.Delete(Configuration._System.UserFile + "~");
                if (fs != null)
                {
                    fs.EnableRaisingEvents = true;
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                File.Copy(Configuration._System.UserFile + "~", Configuration._System.UserFile, true);
            }
        }

        public static void LoadConf()
        {
            Security.Initialize();
            if (!Directory.Exists(Configuration._System.ConfigurationFolder))
            {
                Directory.CreateDirectory(Configuration._System.ConfigurationFolder);
            }
            if (!File.Exists(Configuration._System.ConfigurationFile))
            {
                SystemLog.Warning("there is no configuration file");
                return;
            }
            else
            {
                XmlDocument config = new XmlDocument();
                config.Load(Configuration._System.ConfigurationFile);
                foreach (XmlNode curr in config.ChildNodes[0].ChildNodes)
                {
                    int value = 0;
                    switch (curr.Name.ToLower())
                    {
                        case "databasefolder":
                            Configuration._System.ConfigurationFolder = curr.InnerText;
                            break;
                        case "server_port":
                            Configuration.Network.ServerPort = int.Parse(curr.InnerText);
                            break;
                        case "chunksize":
                            value = int.Parse(curr.InnerText);
                            if (value < 100)
                            {
                                SystemLog.Warning("Invalid chunk size, using default: "
                                           + Configuration._System.ChunkSize);
                                break;
                            }
                            Configuration._System.ChunkSize = value;
                            break;
                        case "ssl":
                            Configuration.Network.UsingSSL = bool.Parse(curr.InnerText);
                            break;
                        case "server_ssl":
                            Configuration.Network.ServerSSL = int.Parse(curr.InnerText);
                            break;
                        case "verbosity":
                            Configuration.Debugging.Verbosity += int.Parse(curr.InnerText);
                            break;
                        case "log":
                            Configuration.Logging.Log = curr.InnerText;
                            break;
                    }
                }
            }
        }
    }
}
