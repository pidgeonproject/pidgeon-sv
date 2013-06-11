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
using System.IO;
using System.Net;
using System.Xml;
using System.Threading;
using System.Text;

namespace pidgeon_sv
{
    public partial class Core
    {
        /// <summary>
        /// Listen SSL
        /// </summary>
        public static void ListenS()
        {
            try
            {
                SL("Opening SSL listener");
                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Any, Configuration.Network.ServerSSL);
                server.Start();
                SL("Listener of SSL is up!!");

                while (isRunning)
                {
                    try
                    {
                        System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                        Thread _client = new Thread(Connection.InitialiseClientSSL);
                        ThreadDB.Add(_client);
                        _client.Start(connection);
                        System.Threading.Thread.Sleep(200);
                    }
                    catch (ThreadAbortException)
                    {
                        SL("Aborted SSL listener");
                        return;
                    }
                    catch (Exception fail)
                    {
                        Core.handleException(fail);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                SL("Aborted SSL listener");
                return;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                SL("Aborted SSL listener");
            }
        }

        /// <summary>
        /// Open a listener for regular connections
        /// </summary>
        public static void Listen()
        {
            try
            {
                SL("Opening listener");

                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Any, Configuration.Network.ServerPort);
                server.Start();

                SL("Listener is up!!");

                while (isRunning)
                {
                    try
                    {
                        System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                        Thread _client = new Thread(Connection.InitialiseClient);
                        ThreadDB.Add(_client);
                        _client.Start(connection);
                        System.Threading.Thread.Sleep(200);
                    }
                    catch (ThreadAbortException)
                    {
                        SL("Aborted listener");
                        return;
                    }
                    catch (Exception fail)
                    {
                        Core.handleException(fail);
                    }
                }
            }
            catch (Exception fail)
            {
                handleException(fail);
                SL("Aborted listener");
                return;
            }
        }
    }
}
