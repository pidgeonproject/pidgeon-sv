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
    
    public class Network
    {
        /// <summary>
        /// Message that is shown to users when you are away
        /// </summary>
        public string AwayMessage = null;
        /// <summary>
        /// User modes
        /// </summary>
        public List<char> UModes = new List<char> { 'i', 'w', 'o', 'Q', 'r', 'A' };
        /// <summary>
        /// Channel user symbols (oper and such)
        /// </summary>
        public List<char> UChars = new List<char> { '~', '&', '@', '%', '+' };
        /// <summary>
        /// Channel user modes
        /// </summary>
        public List<char> CUModes = new List<char> { 'q', 'a', 'o', 'h', 'v' };
        /// <summary>
        /// Channel modes
        /// </summary>
        public List<char> CModes = new List<char> { 'n', 'r', 't', 'm' };
        /// <summary>
        /// Special channel modes with parameter as a string
        /// </summary>
        public List<char> SModes = new List<char> { 'k', 'L' };
        /// <summary>
        /// Special channel modes with parameter as a number
        /// </summary>
        public List<char> XModes = new List<char> { 'l' };
        /// <summary>
        /// Special channel user modes with parameters as a string
        /// </summary>
        public List<char> PModes = new List<char> { 'b', 'I', 'e' };
        /// <summary>
        /// Descriptions for channel and user modes
        /// </summary>
        public Dictionary<char, string> Descriptions = new Dictionary<char, string>();
        /// <summary>
        /// Check if the info is parsed
        /// </summary>
        public bool parsed_info = false;
        /// <summary>
        /// Symbol prefix of channels
        /// </summary>
        public string channel_prefix = "#";
        /// <summary>
        /// List of private message windows
        /// </summary>
        public List<User> PrivateChat = new List<User>();
        /// <summary>
        /// Host name of server
        /// </summary>
        public string ServerName = null;
        /// <summary>
        /// User mode of current user
        /// </summary>
        public Protocol.NetworkMode usermode = new Protocol.NetworkMode();
        /// <summary>
        /// User name (real name)
        /// </summary>
        public string UserName = null;
        /// <summary>
        /// Randomly generated ID for this network to make it unique in case some other network would share the name
        /// </summary>
        public string randomuqid = null;
        /// <summary>
        /// List of all channels on network
        /// </summary>
        public List<Channel> Channels = new List<Channel>();
        /// <summary>
        /// Currently rendered channel on main window
        /// </summary>
        public Channel RenderedChannel = null;
        /// <summary>
        /// Nickname of this user
        /// </summary>
        public string nickname = null;
        /// <summary>
        /// Identification of user
        /// </summary>
        public string Ident = "pidgeon";
        /// <summary>
        /// Quit message
        /// </summary>
        public string Quit = null;
        /// <summary>
        /// Protocol
        /// </summary>
        public Protocol _Protocol = null;
        /// <summary>
        /// Specifies whether this network is using SSL connection
        /// </summary>
        public bool isSecure = false;
        /// <summary>
        /// If true, the channel data will be suppressed in system window
        /// </summary>
        public bool SuppressData = false;
        /// <summary>
        /// This is true when network is just parsing the list of all channels
        /// </summary>
        public bool DownloadingList = false;
        /// <summary>
        /// If the system already attempted to change the nick
        /// </summary>
        public bool usingNick2 = false;
        /// <summary>
        /// Whether user is away
        /// </summary>
        public bool IsAway = false;
        /// <summary>
        /// Whether this network is fully loaded
        /// </summary>
        public bool isLoaded = false;
        /// <summary>
        /// Version of ircd running on this network
        /// </summary>
        public string IrcdVersion = null;
        public string id = null;
        public bool Connected = false;
        /// <summary>
        /// Specifies if you are connected to network
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return Connected;
            }
        }
        private bool isDestroyed = false;
        /// <summary>
        /// This will return true in case object was requested to be disposed
        /// you should never work with objects that return true here
        /// </summary>
        public bool IsDestroyed
        {
            get
            {
                return isDestroyed;
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

        /// <summary>
        /// Create pm
        /// </summary>
        /// <param name="user"></param>
        public void Private(string user)
        {
            User u = new User(user, "", this, "");
            PrivateChat.Add(u);
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
            return _channel;
        }

        public void Destroy()
        {
            if (IsDestroyed)
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
            _Protocol = null;
            isDestroyed = true;
        }

        public bool ShowChat(string name)
        {
            return true;
        }

        public Network(string Server, Protocol sv)
        {
            ServerName = Server;
            _Protocol = sv;
            id = DateTime.Now.ToBinary ().ToString() + "~" + Server;
            Quit = "Pidgeon service - http://pidgeonclient.org";
        }

        ~Network()
        {
            Core.DebugLog("Destructor called for network " + ServerName);
        }

        public void Disconnect()
        {
            Connected = false;
        }
    }
}
