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
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace pidgeon_sv
{
    public class Protocol
    {
        public List<ProtocolMain.Datagram> info = new List<ProtocolMain.Datagram>();
        public int type = 0;
        public Account owner = null;
        private bool Locked = false;
        private int Current_ID = 0;

        /// <summary>
        /// Character which is separating the special commands (such as CTCP part)
        /// </summary>
        public char delimiter = (char)001;
        /// <summary>
        /// Whether this server is connected or not
        /// </summary>
        protected bool Connected = false;
        /// <summary>
        /// If changes to windows should be suppressed (no color changes on new messages)
        /// </summary>
        public bool SuppressChanges = false;
        /// <summary>
        /// Password for server
        /// </summary>
        public string Password = null;
        /// <summary>
        /// Server
        /// </summary>
        public string Server = null;
        /// <summary>
        /// Port
        /// </summary>
        public int Port = 6667;
        /// <summary>
        /// Ssl
        /// </summary>
        public bool SSL = false;
        private bool destroyed = false;
        /// <summary>
        /// This is a time when this connection was open
        /// </summary>
        protected DateTime _time;

        /// <summary>
        /// Time since you connected to this protocol
        /// </summary>
        public TimeSpan ConnectionTime
        {
            get
            {
                return DateTime.Now - _time;
            }
        }

        public bool IsConnected
        {
            get
            {
                return Connected;
            }
        }

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

        public Protocol()
        {
            _time = DateTime.Now;
        }

        public int getMQID()
        {
            try
            {
                while (Locked)
                {
                    Thread.Sleep(10);
                }
                Locked = true;
                Current_ID++;
                int id = Current_ID;
                Locked = false;
                return id;
            } catch (Exception fail)
            {
                Locked = false;
                Core.handleException(fail);
                return Current_ID;
            }
        }

        public class Mode
        {
            public List<string> _Mode = new List<string>();
            public override string ToString()
            {
                string _val = "";
                int curr = 0;
                while (curr < _Mode.Count)
                {
                    _val += _Mode[curr];
                    curr++;
                }
                return "+" + _val;
            }

            public bool mode(string text)
            {
                char prefix = ' ';
                foreach (char _x in text)
                {
                    if (_x == ' ')
                    {
                        return true;
                    }
                    if (_x == '-')
                    {
                        prefix = _x;
                        continue;
                    }
                    if (_x == '+')
                    {
                        prefix = _x;
                        continue;
                    }
                    switch (prefix)
                    {
                        case '+':
                            if (!_Mode.Contains(_x.ToString()))
                            {
                                this._Mode.Add(_x.ToString());
                            }
                            continue;
                        case '-':
                            if (_Mode.Contains(_x.ToString()))
                            {
                                this._Mode.Remove(_x.ToString());
                            }
                            continue;
                    } continue;
                }
                return false;
            }
        }


        public string PRIVMSG(string user, string text)
        {
            return "";
        }

        public virtual void Transfer(string data, ProtocolIrc.Priority _priority = ProtocolIrc.Priority.Normal)
        {

        }

        public virtual int Message2(string text, string to, ProtocolIrc.Priority _priority = ProtocolIrc.Priority.Normal)
        {
            return 0;
        }

        public virtual int Message(string text, string to, ProtocolIrc.Priority _priority = ProtocolIrc.Priority.Normal)
        {
            return 0;
        }

        public virtual int requestNick(string _Nick)
        {
            return 2;
        }

        public virtual bool Command(string cm)
        {
            return false;
        }

        public virtual void WriteMode(Mode _x, string target, Network network = null)
        {
            return;
        }

        public virtual void Join(string name, Network network = null)
        {
            return;
        }

        public virtual bool ConnectTo(string server, int port)
        {
            return false;
        }

        public virtual void Part(string name, Network network = null)
        {

        }

        public virtual void Exit() { }

        public class UserMode : Mode
        {

        }

        public virtual bool Open()
        {
            return false;
        }
    }
}
