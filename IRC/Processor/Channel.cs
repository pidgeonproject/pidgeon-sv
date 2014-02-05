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
    public partial class ProcessorIRC
    {
        private bool ChannelInfo(string[] code, string command, string source, string parameters, string _value)
        {
            if (code.Length > 3)
            {
                //string name = code[2];
                //string topic = _value;
                Channel channel = _Network.getChannel(code[3]);
                if (channel != null)
                {
                    channel._mode.ChangeMode(code[4]);
                    return true;
                }
            }
            return false;
        }

        private bool ChannelData(string command, string parameters, string value)
        {
            //string channel_name = parameters.Substring(parameters.IndexOf(" ") + 1);
            /*int user_count = 0;
            if (channel_name.Contains(" "))
            {
                if (!int.TryParse(channel_name.Substring(channel_name.IndexOf(" ") + 1), out user_count))
                {
                    user_count = 0;
                }

                channel_name = channel_name.Substring(0, channel_name.IndexOf(" "));
            }

            //_Network.DownloadingList = true;

            lock (_Network.ChannelList)
            {
                Network.ChannelData channel = _Network.ContainsChannel(channel_name);
                if (channel == null)
                {
                    channel = new Network.ChannelData(user_count, channel_name, value);
                    _Network.ChannelList.Add(channel);
                }
                else
                {
                    channel.UserCount = user_count;
                    channel.ChannelTopic = value;
                }
                if (_Network.SuppressData)
                {
                    return true;
                }
            }
            */
            return false;
        }

        private bool ChannelTopic(string[] code, string command, string source, string parameters, string value)
        {
            if (code.Length > 3)
            {
                string name = "";
                if (parameters.Contains("#"))
                {
                    name = parameters.Substring(parameters.IndexOf("#")).Replace(" ", "");
                }
                string topic = value;
                Channel channel = _Network.getChannel(name);
                if (channel != null)
                {
                    channel.Topic = topic;
                    return true;
                }
            }
            return false;
        }

        private bool FinishChan(string[] code)
        {
            return false;
        }

        private bool TopicInfo(string[] code, string parameters)
        {
            if (code.Length > 5)
            {
                string name = code[3];
                string user = code[4];
                string time = code[5];
                Channel channel = _Network.getChannel(name);
                if (channel != null)
                {
                    channel.TopicDate = int.Parse(time);
                    channel.TopicUser = user;
                }
            }
            return false;
        }

        private bool ParseUs(string[] code, string realname)
        {
            if (code.Length > 8)
            {
                Channel channel = _Network.getChannel(code[3]);
                string ident = code[4];
                string host = code[5];
                string nick = code[7];
                string server = code[6];
                if (realname != null & realname.Length > 2)
                {
                    realname = realname.Substring(2);
                }
                else if (realname == "0 ")
                {
                    realname = "";
                }
                char mode = '\0';
                bool IsAway = false;
                if (code[8].Length > 0)
                {
                    // if user is away we flag him
                    if (code[8].StartsWith("G"))
                    {
                        IsAway = true;
                    }
                    mode = code[8][code[8].Length - 1];
                    if (!_Network.UChars.Contains(mode))
                    {
                        mode = '\0';
                    }
                }
                if (channel != null)
                {
                    if (updated_text)
                    {
                        if (!channel.containsUser(nick))
                        {
                            User _user = null;
                            if (mode != '\0')
                            {
                                _user = new User(mode.ToString() + nick, host, _Network, ident, server);
                            }
                            else
                            {
                                _user = new User(nick, host, _Network, ident, server);
                            }
                            _user.LastAwayCheck = DateTime.Now;
                            _user.RealName = realname;
                            if (IsAway)
                            {
                                _user.AwayTime = DateTime.Now;
                            }
                            _user.Away = IsAway;
                            lock (channel.UserList)
                            {
                                channel.UserList.Add(_user);
                            }
                            return true;
                        }
                        lock (channel.UserList)
                        {
                            foreach (User u in channel.UserList)
                            {
                                if (u.Nick == nick)
                                {
                                    u.Ident = ident;
                                    u.Host = host;
                                    u.Server = server;
                                    u.RealName = realname;
                                    u.LastAwayCheck = DateTime.Now;
                                    if (!u.Away && IsAway)
                                    {
                                        u.AwayTime = DateTime.Now;
                                    }
                                    u.Away = IsAway;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool ParseInfo(string[] code, string[] data)
        {
            if (code.Length > 3)
            {
                string name = code[4];
                Channel channel = _Network.getChannel(name);
                if (channel != null)
                {
                    string[] _chan = data[2].Split(' ');
                    foreach (var user in _chan)
                    {
                        if (!channel.containsUser(user) && user != "")
                        {
                            lock (channel.UserList)
                            {
                                channel.UserList.Add(new User(user, "", _Network, ""));
                            }
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        private bool ChannelBans(string[] code)
        {
            if (code.Length > 6)
            {
                //string chan = code[3];
                Channel channel = _Network.getChannel(code[3]);
                if (channel != null)
                {
                    if (channel.Bans == null)
                    {
                        channel.Bans = new List<SimpleBan>();
                    }
                    if (!channel.containsBan(code[4]))
                    {
                        channel.Bans.Add(new SimpleBan(code[5], code[4], code[6]));
                    }
                }
            }
            return false;
        }

        private bool ProcessNick(string source, string parameters, string value)
        {
            string nick = source.Substring(0, source.IndexOf("!"));
            string _new = value;
            if (value == "" && parameters != "")
            {
                // server is fucked
                _new = parameters;
                // server is totally borked
                if (_new.Contains(" "))
                {
                    _new = _new.Substring(0, _new.IndexOf(" "));
                }
            }
            foreach (Channel item in _Network.Channels)
            {
                if (item.IsAlive)
                {
                    lock (item.UserList)
                    {
                        foreach (User curr in item.UserList)
                        {
                            if (curr.Nick == nick)
                            {
                                if (updated_text)
                                {
                                    curr.Nick = _new;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool Mode(string source, string parameters, string value)
        {
            if (parameters.Contains(" "))
            {
                string chan = parameters.Substring(0, parameters.IndexOf(" "));
                chan = chan.Replace(" ", "");
                string user = source;
                if (chan.StartsWith(_Network.Channel_Prefix))
                {
                    Channel channel = _Network.getChannel(chan);
                    if (channel != null)
                    {
                        string change = parameters.Substring(parameters.IndexOf(" "));

                        while (change.StartsWith(" "))
                        {
                            change = change.Substring(1);
                        }

                        channel._mode.ChangeMode(change);

                        while (change.EndsWith(" ") && change.Length > 1)
                        {
                            change = change.Substring(0, change.Length - 1);
                        }

                        if (change.Contains(" "))
                        {
                            string header = change.Substring(0, change.IndexOf(" "));
                            List<string> parameters2 = new List<string>();
                            parameters2.AddRange(change.Substring(change.IndexOf(" ") + 1).Split(' '));
                            int curr = 0;

                            char type = ' ';

                            foreach (char m in header)
                            {
                                if (m == '+')
                                {
                                    type = '+';
                                }
                                if (m == '-')
                                {
                                    type = '-';
                                }
                                if (type == ' ')
                                {
                                    continue;
                                }
                                if (_Network.CUModes.Contains(m) && curr <= parameters2.Count)
                                {
                                    User flagged_user = channel.userFromName(parameters2[curr]);
                                    if (flagged_user != null)
                                    {
                                        flagged_user.ChannelMode.ChangeMode(type.ToString() + m.ToString());
                                    }
                                    curr++;
                                }
                                if (parameters2.Count > curr)
                                {
                                    switch (m.ToString())
                                    {
                                        case "b":
                                            if (channel.Bans == null)
                                            {
                                                channel.Bans = new List<SimpleBan>();
                                            }
                                            lock (channel.Bans)
                                            {
                                                if (type == '-')
                                                {
                                                    SimpleBan br = null;
                                                    foreach (SimpleBan xx in channel.Bans)
                                                    {
                                                        if (xx.Target == parameters2[curr])
                                                        {
                                                            br = xx;
                                                            break;
                                                        }
                                                    }
                                                    if (br != null)
                                                    {
                                                        channel.Bans.Remove(br);
                                                    }
                                                    break;
                                                }
                                                channel.Bans.Add(new SimpleBan(user, parameters2[curr], ""));
                                            }
                                            curr++;
                                            break;
                                    }
                                }
                            }
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        private bool Part(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            //string _host;
            //_host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            Channel channel = _Network.getChannel(chan);

            if (channel != null)
            {
                User delete = null;

                if (channel.containsUser(user))
                {
                    lock (channel.UserList)
                    {
                        foreach (User _user in channel.UserList)
                        {
                            if (_user.Nick == user)
                            {
                                delete = _user;
                                break;
                            }
                        }
                    }

                    if (updated_text)
                    {
                        if (delete != null)
                        {
                            channel.UserList.Remove(delete);
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        private bool Topic(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            //string user = source.Substring(0, source.IndexOf("!"));
            Channel channel = _Network.getChannel(chan);
            if (channel != null)
            {
                channel.Topic = value;
            }
            return false;
        }

        private bool Quit(string source, string parameters, string value)
        {
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            //string _host;
            //_host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            //string _new = value;
            foreach (Channel item in _Network.Channels)
            {
                if (item.IsAlive)
                {
                    User target = null;
                    lock (item.UserList)
                    {
                        foreach (User curr in item.UserList)
                        {
                            if (curr.Nick == user)
                            {
                                target = curr;
                                break;
                            }
                        }
                    }
                    if (target != null)
                    {
                        lock (item.UserList)
                        {
                            item.UserList.Remove(target);
                        }
                    }
                }
            }
            return true;
        }

        private bool Kick(string source, string parameters, string value)
        {
            string user = parameters.Substring(parameters.IndexOf(" ") + 1);
            // petan!pidgeon@petan.staff.tm-irc.org KICK #support HelpBot :Removed from the channel
            Channel channel = _Network.getChannel(parameters.Substring(0, parameters.IndexOf(" ")));
            if (channel != null)
            {
                if (channel.containsUser(user))
                {
                    User delete = null;
                    delete = channel.userFromName(user);
                    if (delete != null)
                    {
                        channel.UserList.Remove(delete);
                    }
                }
            }
            return false;
        }

        private bool Join(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            if (chan == "")
            {
                chan = value;
            }
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            Channel channel = _Network.getChannel(chan);
            if (channel != null)
            {

                if (!channel.containsUser(user))
                {
                    lock (channel.UserList)
                    {
                        channel.UserList.Add(new User(user, _host, _Network, _ident));
                    }
                }
            }
            return false;
        }
    }
}
