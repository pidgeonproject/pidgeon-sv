using System;
using System.Collections.Generic;
using System.Text;

namespace pidgeon_sv
{
    public class IRC
    {
        public class ProcessorIRC
        {
            public Network _server = null;
            public Protocol protocol = null;
            public string text;
            public string sn;
            public DateTime pong;
            public long date = 0;
            public string system = "";
            public bool updated_text = true;

            private void Ping()
            {
                pong = DateTime.Now;
                return;
            }

            private bool Info(string command, string parameters, string value)
            {
                /*if (parameters.Contains("PREFIX=("))
                {
                    string cmodes = parameters.Substring(parameters.IndexOf("PREFIX=(") + 8);
                    cmodes = cmodes.Substring(0, cmodes.IndexOf(")"));
                    lock (_server.CUModes)
                    {
                        _server.CUModes.Clear();
                        _server.CUModes.AddRange(cmodes.ToArray<char>());
                    }
                    cmodes = parameters.Substring(parameters.IndexOf("PREFIX=(") + 8);
                    cmodes = cmodes.Substring(cmodes.IndexOf(")") + 1, _server.CUModes.Count);

                    _server.UChars.Clear();
                    _server.UChars.AddRange(cmodes.ToArray<char>());

                }
                if (parameters.Contains("CHANMODES="))
                {
                    string xmodes = parameters.Substring(parameters.IndexOf("CHANMODES=") + 11);
                    xmodes = xmodes.Substring(0, xmodes.IndexOf(" "));
                    string[] _mode = xmodes.Split(',');
                    _server.parsed_info = true;
                    if (_mode.Length == 4)
                    {
                        _server.PModes.Clear();
                        _server.CModes.Clear();
                        _server.XModes.Clear();
                        _server.SModes.Clear();
                        _server.PModes.AddRange(_mode[0].ToArray<char>());
                        _server.XModes.AddRange(_mode[1].ToArray<char>());
                        _server.SModes.AddRange(_mode[2].ToArray<char>());
                        _server.CModes.AddRange(_mode[3].ToArray<char>());
                    }

                }
                 * */
                return true;
            }

            private bool ChannelInfo(string[] code, string command, string source, string parameters, string _value)
            {
                if (code.Length > 3)
                {
                    string name = code[2];
                    string topic = _value;
                    Channel channel = _server.getChannel(code[3]);
                    if (channel != null)
                    {
                        channel._mode.mode(code[4]);
                        return true;
                    }
                }
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
                    Channel channel = _server.getChannel(name);
                    if (channel != null)
                    {
                        //Window curr = channel.retrieveWindow();
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
                    Channel channel = _server.getChannel(name);
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
                    Channel channel = _server.getChannel(code[3]);
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
                                User _user = new User(mode + nick, host, _server, ident);
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
                    if (!updated_text)
                    {
                        return true;
                    }
                    Channel channel = _server.getChannel(name);
                    if (channel != null)
                    {
                        string[] _chan = data[2].Split(' ');
                        foreach (var user in _chan)
                        {
                            if (!channel.containsUser(user) && user != "")
                            {
                                lock (channel.UserList)
                                {
                                    channel.UserList.Add(new User(user, "", _server, ""));
                                }
                            }
                        }
                        return true;
                    }
                }
                return false;
            }

            private bool ProcessPM(string source, string parameters, string value)
            {
                string _nick;
                string _ident;
                string _host;
                string chan;
                _nick = source.Substring(0, source.IndexOf("!"));
                _host = source.Substring(source.IndexOf("@") + 1);
                _ident = source.Substring(source.IndexOf("!") + 1);
                _ident = _ident.Substring(0, _ident.IndexOf("@"));
                chan = parameters.Replace(" ", "");
                string message = value;
                User user = new User(_nick, _host, _server, _ident);
                Channel channel = null;
                if (chan.StartsWith(_server.channel_prefix))
                {
                    channel = _server.getChannel(chan);
                    if (channel != null)
                    {

                        // here we handle a message to channel
                        return true;
                    }
                    return true;
                }
                if (chan == _server.nickname)
                {
                    chan = source.Substring(0, source.IndexOf("!"));
                    // here we handle private message
                    return true;
                }
                return false;
            }

            private bool ChannelBans(string[] code)
            {
                if (code.Length > 6)
                {
                    string chan = code[3];
                    Channel channel = _server.getChannel(code[3]);
                    if (channel != null)
                    {
                        if (channel.Bl == null)
                        {
                            channel.Bl = new List<SimpleBan>();
                        }
                        if (!channel.containsBan(code[4]))
                        {
                            channel.Bl.Add(new SimpleBan(code[5], code[4], code[6]));
                        }
                    }
                }
                return false;
            }

            private bool ProcessNick(string source, string parameters, string value)
            {
                string nick = source.Substring(0, source.IndexOf("!"));
                string _new = value;
                foreach (Channel item in _server.Channels)
                {
                    if (item.ok)
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
                    if (chan.StartsWith(_server.channel_prefix))
                    {
                        Channel channel = _server.getChannel(chan);
                        if (channel != null)
                        {
                            string change = parameters.Substring(parameters.IndexOf(" "));

                            while (change.StartsWith(" "))
                            {
                                change = change.Substring(1);
                            }

                            channel._mode.mode(change);

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
                                    if (_server.CUModes.Contains(m) && curr <= parameters2.Count)
                                    {
                                        User flagged_user = channel.userFromName(parameters2[curr]);
                                        if (flagged_user != null)
                                        {
                                            flagged_user.ChannelMode.mode(type.ToString() + m.ToString());
                                        }
                                        curr++;
                                    }
                                    if (parameters2.Count > curr)
                                    {
                                        switch (m.ToString())
                                        {
                                            case "b":
                                                if (channel.Bl == null)
                                                {
                                                    channel.Bl = new List<SimpleBan>();
                                                }
                                                lock (channel.Bl)
                                                {
                                                    if (type == '-')
                                                    {
                                                        SimpleBan br = null;
                                                        foreach (SimpleBan xx in channel.Bl)
                                                        {
                                                            if (xx.Target == parameters2[curr])
                                                            {
                                                                br = xx;
                                                                break;
                                                            }
                                                        }
                                                        if (br != null)
                                                        {
                                                            channel.Bl.Remove(br);
                                                        }
                                                        break;
                                                    }
                                                    channel.Bl.Add(new SimpleBan(user, parameters2[curr], ""));
                                                }
                                                curr++;
                                                break;
                                        }
                                    }
                                }
                            }
                            return true;
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
                Channel channel = _server.getChannel(chan);

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
                        return true;
                    }
                }
                return false;
            }

            private bool Topic(string source, string parameters, string value)
            {
                string chan = parameters;
                chan = chan.Replace(" ", "");
                string user = source.Substring(0, source.IndexOf("!"));
                Channel channel = _server.getChannel(chan);
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
                foreach (Channel item in _server.Channels)
                {
                    if (item.ok)
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
                            if (updated_text)
                            {
                                lock (item.UserList)
                                {
                                    item.UserList.Remove(target);
                                }
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
                Channel channel = _server.getChannel(parameters.Substring(0, parameters.IndexOf(" ")));
                if (channel != null)
                {
                    if (updated_text && channel.containsUser(user))
                    {
                        User delete = null;
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
                            if (delete != null)
                            {
                                channel.UserList.Remove(delete);
                            }
                        }
                    }
                    return true;
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
                Channel channel = _server.getChannel(chan);
                if (channel != null)
                {
                    if (!channel.containsUser(user))
                    {
                        lock (channel.UserList)
                        {
                            channel.UserList.Add(new User(user, _host, _server, _ident));
                        }
                    }
                }
                return false;
            }

            private bool IdleTime(string source, string parameters, string value)
            {
                if (parameters.Contains(" "))
                {
                    string name = parameters.Substring(parameters.IndexOf(" ") + 1);
                    if (name.Contains(" ") != true)
                    {
                        return false;
                    }
                    string idle = name.Substring(name.IndexOf(" ") + 1);
                    if (idle.Contains(" ") != true)
                    {
                        return false;
                    }
                    string uptime = idle.Substring(idle.IndexOf(" ") + 1);
                    name = name.Substring(0, name.IndexOf(" "));
                    idle = idle.Substring(0, idle.IndexOf(" "));
                    return true;
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
                            switch (command)
                            {
                                case "001":
                                    return true;
                                case "002":
                                case "003":
                                case "004":
                                case "005":
                                    if (Info(command, parameters, _value))
                                    {
                                        //return true;
                                    }
                                    break;
                                case "317":

                                    break;
                                case "PONG":
                                    Ping();
                                    return true;
                                case "INFO":

                                    return true;
                                case "NOTICE":
                                    if (parameters.Contains(_server.channel_prefix))
                                    {

                                    }
                                    return true;
                                case "PING":
                                    protocol.Transfer("PONG ", ProtocolIrc.Priority.High);
                                    return true;
                                case "NICK":
                                    if (ProcessNick(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "PRIVMSG":
                                    if (ProcessPM(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "TOPIC":
                                    if (Topic(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "MODE":
                                    if (Mode(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "PART":
                                    if (Part(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "QUIT":
                                    if (Quit(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "JOIN":
                                    if (Join(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "KICK":
                                    if (Kick(source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                            }
                            if (data[1].Contains(" "))
                            {
                                switch (command)
                                {
                                    case "315":
                                        if (FinishChan(code))
                                        {
                                            return true;
                                        }
                                        break;
                                    case "324":
                                        if (ChannelInfo(code, command, source, parameters, _value))
                                        {
                                            return true;
                                        }
                                        break;
                                    case "332":
                                        if (ChannelTopic(code, command, source, parameters, _value))
                                        {
                                            return true;
                                        }
                                        break;
                                    case "333":
                                        if (TopicInfo(code, parameters))
                                        {
                                            return true;
                                        }
                                        break;
                                    case "352":
                                        if (ParseUs(code))
                                        {
                                            return true;
                                        }
                                        break;
                                    case "353":
                                        if (ParseInfo(code, data))
                                        {
                                            return true;
                                        }
                                        break;
                                    case "366":
                                        return true;
                                    case "367":
                                        if (ChannelBans(code))
                                        {
                                            return true;
                                        }
                                        break;
                                }
                            }
                            if (source.StartsWith(_server.nickname + "!"))
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
                                        Channel curr = _server.getChannel(channel);
                                        if (curr == null)
                                        {
                                            curr = _server.Join(channel);
                                        }
                                        return true;
                                    }
                                }
                                if (_data2.Length > 2)
                                {
                                    if (_data2[1].Contains("NICK"))
                                    {
                                        _server.nickname = _value;
                                    }
                                    if (_data2[1].Contains("PART"))
                                    {
                                        string channel = _data2[2];
                                        if (_data2[2].Contains(_server.channel_prefix))
                                        {
                                            channel = _data2[2];
                                            Channel c = _server.getChannel(channel);
                                            if (c != null)
                                            {
                                                c.ok = false;
                                            }
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    // unhandled

                }
                catch (Exception fail)
                {
                    Core.handleException(fail);
                }
                return true;
            }

            public ProcessorIRC(Network server, Protocol _protocol, string _text, string _sn, string ws, ref DateTime _pong, long d = 0, bool updated = true)
            {
                _server = server;
                protocol = _protocol;
                text = _text;
                sn = _sn;
                system = ws;
                pong = _pong;
                date = d;
                updated_text = updated;
            }
        }
    }
}
