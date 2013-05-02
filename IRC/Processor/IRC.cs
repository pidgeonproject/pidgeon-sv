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
    public class ProcessorIRC
    {
        /// <summary>
        /// Network
        /// </summary>
        public Network _Network = null;
        /// <summary>
        /// Protocol of this network
        /// </summary>
        public Protocol _Protocol = null;
        public string text;
        public DateTime pong;
        public long date = 0;
        public bool updated_text = true;

        private void Ping()
        {
            pong = DateTime.Now;
            return;
        }

        /// <summary>
        /// Retrieve information about the server
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="value">Text</param>
        /// <returns></returns>
        private bool Info(string command, string parameters, string value)
        {
            ProtocolMain.Datagram p = new ProtocolMain.Datagram("DATA", text);
            //p.Parameters.Add("network", Server);
            _Protocol.info.Add(p);
            return true;
        }

        private bool ChannelInfo(string[] code, string command, string source, string parameters, string _value)
        {
            if (code.Length > 3)
            {
                string name = code[2];
                string topic = _value;
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
            string channel_name = parameters.Substring(parameters.IndexOf(" ") + 1);
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

        private bool ParseUs(string[] code)
        {
            if (code.Length > 8)
            {
                Channel channel = _Network.getChannel(code[3]);
                string ident = code[4];
                string host = code[5];
                string nick = code[7];
                string server = code[6];
                string mode = "";
                if (code[8].Length > 0)
                {
                    mode = code[8][code[8].Length - 1].ToString();
                    if (mode == "G" || mode == "H")
                    {
                        mode = "";
                    }
                }
                if (channel != null)
                {
                    if (updated_text)
                    {
                        if (!channel.containsUser(nick))
                        {
                            User _user = new User(mode + nick, host, _Network, ident);
                            channel.UserList.Add(_user);
                            return true;
                        }
                        foreach (User u in channel.UserList)
                        {
                            if (u.Nick == nick)
                            {
                                u.Ident = ident;
                                u.Host = host;
                                break;
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

        private bool ProcessPM(string source, string parameters, string value)
        {

            return false;
        }

        private bool ChannelBans(string[] code)
        {
            if (code.Length > 6)
            {
                string chan = code[3];
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
                if (chan.StartsWith(_Network.channel_prefix))
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
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
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
            string user = source.Substring(0, source.IndexOf("!"));
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
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            string _new = value;
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

        private bool IdleTime(string source, string parameters, string value)
        {
            return false;
        }

        public bool ProcessThis(string source, string[] data, string _value)
        {
            if (source.StartsWith(_Network.nickname + "!"))
            {
                string[] _data2 = data[1].Split(' ');
                if (_data2.Length > 2)
                {
                    if (_data2[1].Contains("JOIN"))
                    {
                        string channel = _data2[2];
                        if (_data2[2].Contains("#") == false)
                        {
                            channel = data[2];
                        }
                        Channel curr = _Network.getChannel(channel);
                        if (curr == null)
                        {
                            curr = _Network.Join(channel);
                        }
                        return true;
                    }
                }
                if (_data2.Length > 2)
                {
                    if (_data2[1].Contains("NICK"))
                    {
                        string _new = _value;
                        if (_value == "" && _data2.Length > 1 && _data2[2] != "")
                        {
                            // server is fucked
                            _new = _data2[2];
                            // server is totally borked
                            if (_new.Contains(" "))
                            {
                                _new = _new.Substring(0, _new.IndexOf(" "));
                            }
                        }
                        _Network.nickname = _new;
                    }
                    if (_data2[1].Contains("PART"))
                    {
                        string channel = _data2[2];
                        if (_data2[2].Contains(_Network.channel_prefix))
                        {
                            channel = _data2[2];
                            Channel c = _Network.getChannel(channel);
                            if (c != null)
                            {
                                if (c != null)
                                {
                                    if (c.IsAlive)
                                    {
                                        c.ChannelWork = false;
                                    }
                                }
                                c.ChannelWork = false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Result()
        {
            try
            {
                if (text == null || text == "")
                {
                    return false;
                }
                if (text.StartsWith(":"))
                {
                    string[] data = text.Split(':');
                    if (data.Length > 1)
                    {
                        string command = "";
                        string parameters = "";
                        string command2 = "";
                        string source;
                        string _value;
                        source = text.Substring(1);
                        source = source.Substring(0, source.IndexOf(" "));
                        command2 = text.Substring(1);
                        command2 = command2.Substring(source.Length + 1);
                        if (command2.Contains(" :"))
                        {
                            command2 = command2.Substring(0, command2.IndexOf(" :"));
                        }
                        string[] _command = command2.Split(' ');
                        if (_command.Length > 0)
                        {
                            command = _command[0];
                        }
                        if (_command.Length > 1)
                        {
                            int curr = 1;
                            while (curr < _command.Length)
                            {
                                parameters += _command[curr] + " ";
                                curr++;
                            }
                            if (parameters.EndsWith(" "))
                            {
                                parameters = parameters.Substring(0, parameters.Length - 1);
                            }
                        }
                        _value = "";
                        if (text.Length > 3 + command2.Length + source.Length)
                        {
                            _value = text.Substring(3 + command2.Length + source.Length);
                        }
                        if (_value.StartsWith(":"))
                        {
                            _value = _value.Substring(1);
                        }
                        string[] code = data[1].Split(' ');
                        ProcessThis(source, data, _value);
                        switch (command)
                        {
                            case "001":
                                return true;
                            case "002":
                            case "003":
                            case "004":
                            case "005":
                                if (Info(command, parameters, _value))
                                { }
                                break;
                            case "322":
                                ChannelData(command, parameters, _value);
                                break;
                            case "PONG":
                                Ping();
                                return true;
                            case "INFO":
                                break;
                            case "NOTICE":
                                break;
                            case "PING":
                                _Protocol.Transfer("PONG ", ProtocolIrc.Priority.High);
                                return true;
                            case "NICK":
                                ProcessNick(source, parameters, _value);
                                break;
                            case "PRIVMSG":
                                ProcessPM(source, parameters, _value);
                                break;
                            case "TOPIC":
                                Topic(source, parameters, _value);
                                break;
                            case "MODE":
                                Mode(source, parameters, _value);
                                break;
                            case "PART":
                                Part(source, parameters, _value);
                                break;
                            case "QUIT":
                                Quit(source, parameters, _value);
                                break;
                            case "JOIN":
                                Join(source, parameters, _value);
                                break;
                            case "KICK":
                                Kick(source, parameters, _value);
                                break;
                        }
                        if (data[1].Contains(" "))
                        {
                            switch (command)
                            {
                                case "315":
                                    if (FinishChan(code))
                                    {
                                        break;
                                    }
                                    break;
                                case "324":
                                    ChannelInfo(code, command, source, parameters, _value);
                                    break;
                                case "332":
                                    ChannelTopic(code, command, source, parameters, _value);
                                    break;
                                case "333":
                                    TopicInfo(code, parameters);
                                    break;
                                case "352":
                                    ParseUs(code);
                                    break;
                                case "353":
                                    ParseInfo(code, data);
                                    break;
                                case "366":
                                    break;
                                case "367":
                                    ChannelBans(code);
                                    break;
                            }
                        }
                    }
                }
                ProtocolMain.Datagram dt = new ProtocolMain.Datagram("DATA", text);
                dt.Parameters.Add("network", _Protocol.Server);
                dt.Parameters.Add("MQID", _Protocol.getMQID().ToString());
                ProtocolIrc protocol = (ProtocolIrc)_Protocol;
                protocol.buffer.DeliverMessage(dt);
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
            }
            return true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_network"></param>
        /// <param name="_text"></param>
        /// <param name="_pong"></param>
        /// <param name="_date">Date of this message, if you specify 0 the current time will be used</param>
        /// <param name="updated">If true this text will be considered as newly obtained information</param>
        public ProcessorIRC(Network _network, string _text, ref DateTime _pong, long _date = 0, bool updated = true)
        {
            _Network = _network;
            _Protocol = _network._protocol;
            text = _text;
            pong = _pong;
            date = _date;
            updated_text = updated;
        }
    }

}
