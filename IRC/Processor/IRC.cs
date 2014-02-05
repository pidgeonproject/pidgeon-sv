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
            _Protocol.NetworkInfo.Add(p);
            return true;
        }

        private bool ProcessPM(string source, string parameters, string value)
        {
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
                        if (_data2[2].Contains(_Network.Channel_Prefix))
                        {
                            channel = _data2[2];
                            Channel c = _Network.getChannel(channel);
                            if (c != null)
                            {
                                lock (_Network.Channels)
                                {
                                    if (_Network.Channels.Contains(c))
                                    {
                                        _Network.Channels.Remove(c);
                                    }
                                }
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
                                    ParseUs(code, _value);
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
            _Protocol = _network._Protocol;
            text = _text;
            pong = _pong;
            date = _date;
            updated_text = updated;
        }
    }

}
