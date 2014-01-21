﻿/***************************************************************************
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
    /// <summary>
    /// User
    /// </summary>
    [Serializable]
    public class User : IComparable
    {
        /// <summary>
        /// Host name
        /// </summary>
        public string Host = null;
        /// <summary>
        /// Network
        /// </summary>
        [NonSerialized]
        public Network _Network = null;
        /// <summary>
        /// Identifier
        /// </summary>
        public string Ident = null;
        /// <summary>
        /// Channel mode
        /// </summary>
        public Protocol.NetworkMode ChannelMode = new Protocol.NetworkMode();
        /// <summary>
        /// Status
        /// </summary>
        public ChannelStatus Status = ChannelStatus.Regular;
        /// <summary>
        /// Nick
        /// </summary>
        public string Nick = null;
        /// <summary>
        /// Name
        /// </summary>
        public string RealName = null;
        /// <summary>
        /// Server
        /// </summary>
        public string Server = null;
        /// <summary>
        /// Away message
        /// </summary>
        public string AwayMessage = null;
        /// <summary>
        /// User away
        /// </summary>
        public bool Away = false;
        public DateTime LastAwayCheck;
        public DateTime AwayTime;
        private bool destroyed = false;
        /// <summary>
        /// This will return true in case object was requested to be disposed
        /// you should never work with objects that return true here
        /// </summary>
        public bool IsDestroyed
        {
            get
            {
                return destroyed;
            }
        }

        /// <summary>
        /// This return true if we are looking at current user
        /// </summary>
        public bool IsPidgeon
        {
            get
            {
                return (Nick.ToLower() == _Network.nickname.ToLower());
            }
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">user!ident@hostname</param>
        /// <param name="network"></param>
        public User(string user, Network network)
        {
            if (!user.Contains("@") || !user.Contains("!"))
            {
                SystemLog.DebugLog("Unable to create user from " + user);
                return;
            }
            string name = user.Substring(0, user.IndexOf("!"));
            string ident = user.Substring(user.IndexOf("!") + 1);
            string host = ident.Substring(ident.IndexOf("@") + 1);
            ident = ident.Substring(0, ident.IndexOf("@"));
            MakeUser(name, host, network, ident);
            Server = network.ServerName;
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="nick"></param>
        /// <param name="host"></param>
        /// <param name="network"></param>
        /// <param name="ident"></param>
        public User(string nick, string host, Network network, string ident)
        {
            MakeUser(nick, host, network, ident);
            Server = network.ServerName;
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="nick"></param>
        /// <param name="host"></param>
        /// <param name="network"></param>
        /// <param name="ident"></param>
        /// <param name="server"></param>
        public User(string nick, string host, Network network, string ident, string server)
        {
            MakeUser(nick, host, network, ident);
            Server = server;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~User()
        {
            // remove reference to network from channel mode that is no longer going to be accessible so that GC can remove it
            ChannelMode.network = null;
        }

        /// <summary>
        /// Get a list of all channels this user is in
        /// </summary>
        public List<Channel> ChannelList
        {
            get
            {
                List<Channel> List = new List<Channel>();
                if (_Network == null)
                {
                    return null;
                }
                lock (_Network.Channels)
                {
                    foreach (Channel xx in _Network.Channels)
                    {
                        if (xx.containsUser(Nick))
                        {
                            List.Add(xx);
                        }
                    }
                }
                return List;
            }
        }

        /// <summary>
        /// Change a user level according to symbol
        /// </summary>
        /// <param name="symbol"></param>
        public void SymbolMode(char symbol)
        {
            if (_Network == null)
            {
                return;
            }

            if (symbol == '\0')
            {
                return;
            }

            if (_Network.UChars.Contains(symbol))
            {
                char mode = _Network.CUModes[_Network.UChars.IndexOf(symbol)];
                ChannelMode.ChangeMode("+" + mode.ToString());
            }
        }

        private void MakeUser(string nick, string host, Network network, string ident)
        {
            _Network = network;
            if (nick != "")
            {
                char prefix = nick[0];
                if (network.UChars.Contains(prefix))
                {
                    SymbolMode(prefix);
                    nick = nick.Substring(1);
                }
            }
            Nick = nick;
            Ident = ident;
            Host = host;
        }

        /// <summary>
        /// Destroy
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }
            destroyed = true;
            _Network = null;
        }

        /// <summary>
        /// Converts a user object to string
        /// </summary>
        /// <returns>[nick!ident@host]</returns>
        public override string ToString()
        {
            return Nick + "!" + Ident + "@" + Host;
        }

        /// <summary>
        /// Generate full string
        /// </summary>
        /// <returns></returns>
        public string ConvertToInfoString()
        {
            if (RealName != null)
            {
                return RealName + "\n" + ToString();
            }
            return ToString();
        }

        /// <summary>
        /// Internal function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj is User)
            {
                return this.Nick.CompareTo((obj as User).Nick);
            }
            return 0;
        }

        /// <summary>
        /// Channel status
        /// </summary>
        public enum ChannelStatus
        {
            /// <summary>
            /// Owner
            /// </summary>
            Owner,
            /// <summary>
            /// Admin
            /// </summary>
            Admin,
            /// <summary>
            /// Operator
            /// </summary>
            Op,
            /// <summary>
            /// Halfop
            /// </summary>
            Halfop,
            /// <summary>
            /// Voice
            /// </summary>
            Voice,
            /// <summary>
            /// Normal user
            /// </summary>
            Regular,
        }
    }
}
