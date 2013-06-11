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
using System.Text;

namespace pidgeon_sv
{
    class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Core.StartTime = DateTime.Now;
                Core.Parameters = args;
                Configuration._System.UserFile = Configuration._System.DatabaseFolder + Path.DirectorySeparatorChar + "users";

                if (!Directory.Exists(Configuration._System.DatabaseFolder))
                {
                    Directory.CreateDirectory(Configuration._System.DatabaseFolder);
                }

                // Check the parameters and if we can continue, launch the core
                if (Terminal.Parameters())
                {
                    if (!Core.Init())
                    {
                        return;
                    }
                    if (Configuration.Network.UsingSSL)
                    {
                        Core.SSLListenerTh = new System.Threading.Thread(Core.ListenS);
                        Core.SSLListenerTh.Start();
                    }
                    Core.Listen();
                }
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                return;
            }
        }
    }
}
