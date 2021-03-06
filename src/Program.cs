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
using System.Threading;
using System.IO;

namespace pidgeon_sv
{
    class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Core.StartTime = DateTime.Now;
                Core.Parameters = new List<string>(args);
                Configuration.Init();
                // Check the parameters and if we can continue, launch the core
                if (!Terminal.Parameters())
                    return;
                if (Configuration.Services.Daemon)
                {
                    SystemLog.DebugLog("Loading core");
                    if (!Core.Init())
                          return;
                    // load a system log writer
                    Writer.Init();
                    // create a new regular listener
                    ServicesListener listener = new ServicesListener(Configuration.Network.ServerPort);
                    listener.Listen();
                    if (Configuration.Network.UsingSSL)
                    {
                        // create a new ssl listener
                        SecuredListener listener_ssl = new SecuredListener(Configuration.Network.ServerSSL);
                        listener_ssl.Listen();
                    }
                    while (Core.IsRunning)
                        Thread.Sleep(800);
                } else
                    Console.WriteLine("Nothing to do! Run pidgeon-sv --help in order to see the options.");
            }
            catch (Exception fail)
            {
                Console.WriteLine("FATAL: " + fail.Message);
                Core.handleException(fail);
                return;
            }
        }
    }
}
