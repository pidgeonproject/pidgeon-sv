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
    /// <summary>
    /// This is a global interface for channel modes with parameters
    /// </summary>
    public class ChannelParameterMode
    {
        /// <summary>
        /// Target of a mode
        /// </summary>
        public string Target = null;
        /// <summary>
        /// Time when it was set
        /// </summary>
        public string Time = null;
        /// <summary>
        /// User who set the ban / invite
        /// </summary>
        public string User = null;
    }

    /// <summary>
    /// Invite
    /// </summary>
    [Serializable]
    public class Invite : ChannelParameterMode
    {
        /// <summary>
        /// Creates a new instance of invite
        /// </summary>
        public Invite()
        {
            // This empty constructor is here so that we can serialize this
        }

        /// <summary>
        /// Creates a new instance of invite
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="target">Target</param>
        /// <param name="time">Time</param>
        public Invite(string user, string target, string time)
        {

        }
    }

    /// <summary>
    /// Exception
    /// </summary>
    [Serializable]
    public class ChannelBanException : ChannelParameterMode
    {
        /// <summary>
        /// Creates a new instance of channel ban exception (xml constructor only)
        /// </summary>
        public ChannelBanException()
        {
            // This empty constructor is here so that we can serialize this
        }
    }

    /// <summary>
    /// Simplest ban
    /// </summary>
    [Serializable]
    public class SimpleBan : ChannelParameterMode
    {
        /// <summary>
        /// Creates a new instance of simple ban (xml constructor only)
        /// </summary>
        public SimpleBan()
        {
            // This empty constructor is here so that we can serialize this
        }

        /// <summary>
        /// Creates a new instance of simple ban
        /// </summary>
        /// <param name="user">Person who set a ban</param>
        /// <param name="target">Who is target</param>
        /// <param name="time">Unix date when it was set</param>
        public SimpleBan(string user, string target, string time)
        {
            Target = target;
            User = user;
            Time = time;
        }
    }

    public class Channel
    {
        /// <summary>
        /// Name of a channel including the special prefix
        /// </summary>
        public string Name;
        /// <summary>
        /// Network the channel belongs to
        /// </summary>
        [NonSerialized]
        public Network _Network = null;
        /// <summary>
        /// List of all users in current channel
        /// </summary>
        [NonSerialized]
        public List<User> UserList = new List<User>();
        /// <summary>
        /// Topic
        /// </summary>
        public string Topic = null;
        /// <summary>
        /// User who set a topic
        /// </summary>
        public string TopicUser = "<Unknown user>";
        /// <summary>
        /// Date when a topic was set
        /// </summary>
        public int TopicDate = 0;
        /// <summary>
        /// Invites
        /// </summary>
        public List<Invite> Invites = null;
        /// <summary>
        /// List of bans set
        /// </summary>
        public List<SimpleBan> Bans = null;
        /// <summary>
        /// Exception list 
        /// </summary>
        public List<ChannelBanException> Exceptions = null;
        /// <summary>
        /// Channel mode
        /// </summary>
        //[NonSerialized]
        public Protocol.NetworkMode _mode = new Protocol.NetworkMode();
        /// <summary>
        /// If true the window is considered usable, in case it's false, the window is flagged as parted channel
        /// </summary>
        public bool ChannelWork = false;
        /// <summary>
        /// If this is false the channel is not being used / you aren't in it or you can't access it
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (!ChannelWork)
                {
                    return false;
                }
                if (IsDestroyed)
                {
                    return false;
                }
                if (_Network != null)
                {
                    return _Network.IsConnected;
                }
                return true;
            }
        }
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
		/// Initializes a new instance of the <see cref="pidgeon_sv.Channel"/> class.
		/// </summary>
        public Channel()
        {
            ChannelWork = true;
        }
		
        public bool containsUser(string user)
        {
            lock (UserList)
            {
                foreach (var name in UserList)
                {
                    if (name.Nick == user)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool containsBan(string host)
        {
            lock (Bans)
            {
                foreach (var name in Bans)
                {
                    if (name.Target == host)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
		
		public void Destroy()
        {
            if (IsDestroyed)
            {
                // prevent this from being called multiple times
                return;
            }

            destroyed = true;

            lock (UserList)
            {
                UserList.Clear();
            }

            ChannelWork = false;
            _Network = null;

            if (Invites != null)
            {
                lock (Invites)
                {
                    Invites.Clear();
                }
            }

            if (Exceptions != null)
            {
                lock (Exceptions)
                {
                    Exceptions.Clear();
                }
            }

            if (Bans != null)
            {
                lock (Bans)
                {
                    Bans.Clear();
                }
            }
        }
		
        public User userFromName(string name)
        {
            foreach (User item in UserList)
            {
                if (name == item.Nick)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
