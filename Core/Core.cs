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
    public partial class Core
    {
		private static bool running = true;
		/// <summary>
		/// The running.
		/// </summary>
        public static bool IsRunning
		{
			get
			{
				return true;
			}
		}
		/// <summary>
		/// Uptime
		/// </summary>
        public static DateTime StartedTime;
		/// <summary>
		/// List of all existing accounts in system
		/// </summary>
        public static List<Account> _accounts = new List<Account>();
        public static List<Thread> threads = new List<Thread>();

        public static void Quit()
        {
            SL("Killing all connections and running processes");
            foreach (Thread curr in threads)
            {
				if (curr.ThreadState == ThreadState.WaitSleepJoin || curr.ThreadState == ThreadState.Running)
				{
                	curr.Abort();
				}
            }
            SL("Exiting");
        }
		
        public static void handleException(Exception reason, bool ThreadOK = false)
        {
            if (reason.GetType() == typeof(ThreadAbortException) && ThreadOK)
            {
                return;
            }
            SL("Exception: " + reason.Message + " " + reason.StackTrace + " in: " + reason.Source);
        }

        public static void DebugLog(string text)
        {
            SL("DEBUG: " + text);
        }
		
		/// <summary>
		/// System log
		/// </summary>
		/// <param name='text'>
		/// Text
		/// </param>
        public static void SL(string text)
        {
            Console.WriteLine(DateTime.Now.ToString() + ": " + text);
        }
		
        public static bool Init()
        {
            try
            {
                SL("Pidgeon services " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " loading");
                SL("OS: " + Environment.OSVersion.ToString());

                LoadConf();

                if (!File.Exists(Config.CertificatePath) && Config.UsingSSL)
                {
                    SL("There is no certificate file, creating one now");
                    certificate(Config.CertificatePath, "pidgeonclient.org");
                }

                Config.UserFile = Config.DatabaseFolder + Path.DirectorySeparatorChar + "users";

                if (!Directory.Exists("db"))
                {
                    Directory.CreateDirectory("db");
                }

                SL("This instance of pidgeon services has following parameters:");
                SL("-----------------------------------------------------------");
                SL("Port: " + Config.server_port.ToString());
                if (Config.MaxFileChunkSize == 0)
                {
                    SL("Maximum file chunk size: unlimited");
                }
                else
                {
                    SL("Maximum file chunk size: " + Config.MaxFileChunkSize.ToString());
                }
                SL("Mode: " + Config.mode.ToString());
                SL("Minimum buffer size: " + Config.minbs.ToString());
                SL("Minimum chunk size: " + Config.ChunkSize.ToString());
                SL("SSL is enabled: " + Config.UsingSSL.ToString());

                SL("-----------------------------------------------------------");

                LoadUser();

                return true;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                SL("Fatal error - exiting");
                return false;
            }
        }

        public static bool certificate(string name, string host)
        {
            byte[] c = Certificate.CreateSelfSignCertificatePfx(
                "CN=" + host, //host name
                DateTime.Parse("2000-01-01"), //not valid before
                DateTime.Parse("2020-01-01"), //not valid after
                "pidgeon"); //password to encrypt key file

            using (BinaryWriter binWriter = new BinaryWriter(File.Open(name, FileMode.Create)))
            {
                binWriter.Write(c);
            }
            return true;
        }

        public static void Listen()
        {
            try
            {
                SL("Waiting for clients");

                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Any, Config.server_port);
                server.Start();

                while (running)
                {
                    try
                    {
                        System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                        Thread _client = new Thread(Connection.InitialiseClient);
                        threads.Add(_client);
                        _client.Start(connection);
                        System.Threading.Thread.Sleep(200);
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
                SL("Terminating");
                return;
            }
        }
    }
}
