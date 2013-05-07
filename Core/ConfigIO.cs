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
        public static FileSystemWatcher fs;

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Core.SL("Warning, the user file was changed, reloading it");
            LoadUser();
        }

        /// <summary>
        /// Load all user data and info
        /// </summary>
        public static void LoadUser(bool ro = false)
        {
            try
            {
                if (!ro)
                {
                    SL("Loading users");
                }
                if (File.Exists(Config._System.UserFile))
                {
					if (!ro)
					{
	                    fs = new FileSystemWatcher();
	                    fs.Path = Config._System.DatabaseFolder;
	                    fs.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
	                    fs.Filter = "users";
	                    fs.Changed += new FileSystemEventHandler(OnChanged);
	                    fs.Created += new FileSystemEventHandler(OnChanged);
	                    fs.Deleted += new FileSystemEventHandler(OnChanged);
	                    fs.EnableRaisingEvents = true;
					}
                    XmlDocument configuration = new XmlDocument();
                    configuration.Load(Config._System.UserFile);
                    if (!(configuration.ChildNodes.Count > 0))
                    {
                        Core.DebugLog("There is no proper information about users in config file");
                        return;
                    }
                    lock (_accounts)
                    {
                        Core.DebugLog("Loading users: " + configuration.ChildNodes[0].ChildNodes.Count.ToString(), 2);
                        foreach (XmlNode curr in configuration.ChildNodes[0].ChildNodes)
                        {
                            SystemUser.UserLevel UserLevel = SystemUser.UserLevel.User;
                            bool locked = false;
                            string name = null;
                            string password = null;
                            string nickname = null;
                            string ident = "pidgeon";
                            string realname = "http://pidgeonclient.org/wiki";
                            if (Config._System.Rooted)
                            {
                                UserLevel = SystemUser.UserLevel.Root;
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
                                        switch (configitem.Value.ToLower())
                                        {
                                            case "root":
                                                UserLevel = SystemUser.UserLevel.Root;
                                                break;
                                            case "admin":
                                                UserLevel = SystemUser.UserLevel.Admin;
                                                break;
                                            case "user":
                                                UserLevel = SystemUser.UserLevel.User;
                                                break;
                                        }
                                        break;
                                }
                            }
                            if (name == null || password == null)
                            {
                                Core.DebugLog("Invalid record for some user, skipped");
                                continue;
                            }
                            bool Nonexistent = false;
                            SystemUser line = SystemUser.getUser(name);
                            if (line == null)
                            {
                                Nonexistent = true;
                                line = new SystemUser(name, password, ro);
                            }
                            else
                            {
                                if (line.Locked != locked)
                                {
                                    if (locked)
                                    {
                                        // we need to lock this user
                                        Core.DebugLog("Locking user: " + name);
                                        line.Locked = locked;
                                        SystemUser.KickUser(line);
                                    }
                                }
                            }
                            line.password = password;
                            line.nickname = nickname;
                            line.Locked = locked;
                            line.ident = ident;
                            line.realname = realname;
                            line.Level = UserLevel;
                            if (Nonexistent)
                            {
                                _accounts.Add(line);
                            }
                        }
                        if (!ro)
                        {
                            SL("Loaded users: " + _accounts.Count.ToString());
                        }
                    }
                }
                else
                {
                    if (!ro)
                    {
                        SL("There is no userfile for this instance, create one using parameter -a");
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
                if (File.Exists(Config._System.UserFile))
                {
                    File.Copy(Config._System.UserFile, Config._System.UserFile + "~", true);
                }
                XmlDocument configuration = new XmlDocument();

                XmlNode xmlnode = configuration.CreateElement("users");

                lock (_accounts)
                {
                    foreach (SystemUser user in _accounts)
                    {
                        XmlNode item = configuration.CreateElement("user");
                        XmlAttribute name = configuration.CreateAttribute("name");
                        XmlAttribute password = configuration.CreateAttribute("password");
                        XmlAttribute nick = configuration.CreateAttribute("nickname");
                        XmlAttribute ident = configuration.CreateAttribute("ident");
                        XmlAttribute realname = configuration.CreateAttribute("realname");
                        XmlAttribute locked = configuration.CreateAttribute("locked");
                        XmlAttribute level = configuration.CreateAttribute("level");
                        name.Value = user.username;
                        password.Value = user.password;
                        nick.Value = user.nickname;
                        ident.Value = user.ident;
                        realname.Value = user.realname;
                        locked.Value = user.Locked.ToString();
                        level.Value = user.Level.ToString();
                        item.Attributes.Append(name);
                        item.Attributes.Append(password);
                        item.Attributes.Append(nick);
                        item.Attributes.Append(ident);
                        item.Attributes.Append(realname);
                        item.Attributes.Append(locked);
                        item.Attributes.Append(level);
                        xmlnode.AppendChild(item);
                    }
                }

                configuration.AppendChild(xmlnode);
                configuration.Save(Config._System.UserFile);
                File.Delete(Config._System.UserFile + "~");
                if (fs != null)
                {
                    fs.EnableRaisingEvents = true;
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                File.Copy(Config._System.UserFile + "~", Config._System.UserFile, true);
            }
        }

        public static void LoadConf()
        {
            if (!File.Exists(Config._System.ConfigurationFile))
            {
                SL("WARNING: there is no configuration file");
                return;
            }
            else
            {
                XmlDocument config = new XmlDocument();
                config.Load(Config._System.ConfigurationFile);
                foreach (XmlNode curr in config.ChildNodes[0].ChildNodes)
                {
                    int value = 0;
                    switch (curr.Name.ToLower())
                    {
                        case "databasefolder":
                            Config._System.DatabaseFolder = curr.InnerText;
                            break;
						case "server_port":
                            Config.Network.server_port = int.Parse(curr.InnerText);
                            break;
						case "ChunkSize":
                            value = int.Parse(curr.InnerText);
                            if (value < 100)
                            {
                                SL("Invalid chunk size, using default: " + Config._System.ChunkSize);
                                break;
                            }
                            Config._System.ChunkSize = value;
                            break;
                        case "ssl":
                            Config.Network.UsingSSL = bool.Parse(curr.InnerText);
                            break;
						case "server_ssl":
							Config.Network.server_ssl = int.Parse(curr.InnerText);
							break;
                    }
                }
            }
        }
    }
}
