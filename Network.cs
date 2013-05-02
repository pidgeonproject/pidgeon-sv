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
    public class SimpleMode
    {
        private char _char;
        private string _Parameter = null;
        /// <summary>
        /// Character of this mode
        /// </summary>
        public char Mode
        {
            get
            {
                return _char;
            }
        }
        /// <summary>
        /// Parameter of this mode
        /// </summary>
        public string Parameter
        {
            get
            {
                return _Parameter;
            }
        }
        public bool ContainsParameter
        {
            get
            {
                return !(_Parameter == null);
            }
        }

        public SimpleMode(char mode, string parameter)
        {
            _char = mode;
            _Parameter = parameter;
        }

        public override string ToString()
        {
            if (ContainsParameter)
            {
                return "+" + _char.ToString() + " " + Parameter;
            }
            return "+" + _char.ToString();
        }
    }

    public class WindowObject
    {
        public string name = null;
        public bool writable = false;
    }
    
    public class Network
    {
        public bool Connected = false;
        public List<User> PrivateChat = new List<User>();
        public string server = null;
        public Protocol.NetworkMode usermode = new Protocol.NetworkMode();
        public string username = null;
        public List<Channel> Channels = new List<Channel>();
        public string nickname = null;
        public string ident = null;
        public string quit;
        public Protocol _protocol = null;
        public List<WindowObject> windows = new List<WindowObject>();
        public List<char> UModes = new List<char> { 'i', 'w', 'o', 'Q', 'r', 'A' };
        public List<char> UChars = new List<char> { '~', '&', '@', '%', '+' };
        public List<char> CUModes = new List<char> { 'q', 'a', 'o', 'h', 'v' };
        public List<char> CModes = new List<char> { 'n', 'r', 't', 'm' };
        public List<char> SModes = new List<char> { 'k', 'L' };
        public List<char> XModes = new List<char> { 'l' };
        public List<char> PModes = new List<char> { 'b', 'I', 'e' };
		public string id = null;
        private bool destroyed = false;

        public string channel_prefix = "#";
		
		public bool IsConnected
		{
			get
			{
				return Connected;
			}
		}
		
        public Channel getChannel(string name)
        {
            lock (Channels)
            {
                foreach (Channel cu in Channels)
                {
                    if (cu.Name == name)
                    {
                        return cu;
                    }
                }
            }
            return null;
        }

        public void CreateChat(string _name, bool _Focus, bool _writable = false, bool channelw = false)
        {
            WindowObject w = new WindowObject();
            w.name = _name;
            w.writable = _writable;
            lock (windows)
            {
                windows.Add(w);
            }
        }

        /// <summary>
        /// Create pm
        /// </summary>
        /// <param name="user"></param>
        public void Private(string user)
        {
            User u = new User(user, "", this, "");
            PrivateChat.Add(u);
            CreateChat(user, true, true);
            return;
        }

        public Channel Join(string channel)
        {
            Channel _channel = new Channel();
            _channel.Name = channel;
            _channel._Network = this;
            lock (Channels)
            {
                Channels.Add(_channel);
            }
            CreateChat(channel, true, true, true);
            return _channel;
        }

        public void Destroy()
        {
            if (destroyed)
            {
                return;
            }
            Disconnect();
            lock (Channels)
            {
				foreach (Channel xx in Channels)
                {
                    xx.Destroy();
                }
                Channels.Clear();
            }
            _protocol = null;
            destroyed = true;
        }

        public bool ShowChat(string name)
        {
            return true;
        }

        public Network(string Server, Protocol sv)
        {
            server = Server;
            _protocol = sv;
			id = DateTime.Now.ToBinary ().ToString() + "~" + Server;
            quit = "Pidgeon service - http://pidgeonclient.org";
            CreateChat("!system", true);
        }

        ~Network()
        {
            Core.DebugLog("Destructor called for network " + server);
        }

        public void Disconnect()
        {
            lock (windows)
            {
                windows.Clear();
            }
        }
    }
}
