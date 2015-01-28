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
    public class ServicesListener : Listener
    {
        /// <summary>
        /// This is a thread used by listener in order to wait
        /// for incomming connections
        /// </summary>
        private Thread _Thread = null;
        private System.Net.Sockets.TcpListener Server;
        public bool IsRunning = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="pidgeon_sv.ServicesListener"/> class.
        /// </summary>
        /// <param name='port'>
        /// Port.
        /// </param>
        public ServicesListener(int port)
        {
            this.Port = port;
        }

        private void Exec()
        {
            while (this.IsRunning)
            {
                try
                {
                    System.Net.Sockets.TcpClient connection = this.Server.AcceptTcpClient();
                    Thread _client = new Thread(Session.InitialiseClient);
                    _client.Name = "Session:" + connection.Client.RemoteEndPoint.ToString();
                    ThreadPool.RegisterThread(_client);
                    _client.Start(connection);
                    System.Threading.Thread.Sleep(200);
                }
                catch (ThreadAbortException)
                {
                    SystemLog.WriteLine("Listener for services (unencrypted connection on port: " +
                           this.Port + ") is: DOWN");
                    return;
                }
                catch (Exception fail)
                {
                    Core.handleException(fail);
                }
            }
            SystemLog.WriteLine("Listener for services (unencrypted connection on port: " +
                               this.Port + ") is: DOWN");
        }

        public override bool Listen()
        {
            try
            {
                SystemLog.WriteLine("Opening listener for services (unencrypted connection on port: " +
                               this.Port + ")");

                this.Server = new System.Net.Sockets.TcpListener(IPAddress.Any, this.Port);
                this.Server.Start();

                SystemLog.WriteLine("Listener for services (unencrypted connection on port: " +
                               this.Port + ") is: UP");

                this.IsRunning = true;
                this._Thread = new Thread(this.Exec);
                this._Thread.Start();
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                SystemLog.WriteLine("Listener for services (unencrypted connection on port: " +
                               this.Port + ") is: DOWN");
                return false;
            }
            return true;
        }

        public override bool Close()
        {
            return base.Close();
        }
    }
}

