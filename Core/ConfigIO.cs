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
                    if (!(configuration.ChildNodes.Count > 0))
                    {
                        Core.DebugLog("There is no proper information about users in config file");
                        return;
                    }
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
                                    switch (configitem.Value.ToLower())
                                    {
                                        case "root":
                                            UserLevel = Account.UserLevel.Root;
                                            break;
                                        case "admin":
                                            UserLevel = Account.UserLevel.Admin;
                                            break;
                                        case "user":
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
                XmlDocument configuration = new XmlDocument();

                XmlNode xmlnode = configuration.CreateElement("users");

                lock (_accounts)
                {
                    foreach (Account user in _accounts)
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
                configuration.Save(Config.UserFile);
                File.Delete(Config.UserFile + "~");
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
                            break;
                            Config.Mode mode = Config.Mode.Core;
                            if (curr.InnerText == "bouncer")
                            {
                                mode = Config.Mode.Bouncer;
                            }
                            Config.mode = mode;
                            break;
                        case "ssl":
                            Config.UsingSSL = bool.Parse(curr.InnerText);
                            break;
                    }
                }
            }
        }
    }
}
