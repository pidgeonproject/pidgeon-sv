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
        /// <summary>
        /// Parameters of the application
        /// </summary>
        public static string[] Parameters = null;
        /// <summary>
        /// SSL listener thread
        /// </summary>
        public static Thread SSLListenerTh = null;
        /// <summary>
        /// Whether system is running
        /// </summary>
        private static bool isRunning = true;
        /// <summary>
        /// The running.
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                return isRunning;
            }
        }
        /// <summary>
        /// Uptime
        /// </summary>
        public static DateTime StartTime;
        /// <summary>
        /// List of all existing accounts in system
        /// </summary>
        public static List<SystemUser> UserList = new List<SystemUser>();
        /// <summary>
        /// List of all threads in core
        /// </summary>
        public static List<Thread> ThreadDB = new List<Thread>();

        /// <summary>
        /// Remove a thread from system, in case it's running it will also abort it - in case you call this function
        /// on a same thread as which you are in, it will only remove the thread from the list but it won't abort it
        /// </summary>
        /// <param name="thread"></param>
        public static void DisableThread(Thread thread)
        {
            if (thread == null)
            {
                return;
            }

            lock (ThreadDB)
            {
                if (ThreadDB.Contains(thread))
                {
                    ThreadDB.Remove(thread);
                }
            }

            if (Thread.CurrentThread == thread)
            {
                SystemLog.DebugLog("Attempt of thread to kill self: " + thread.Name);
                return;
            }

            if (thread.ThreadState == ThreadState.Running ||
                thread.ThreadState == ThreadState.WaitSleepJoin ||
                thread.ThreadState == ThreadState.Background)
            {
                thread.Abort();
            }
        }

        /// <summary>
        /// Quit
        /// </summary>
        public static void Quit()
        {
            SystemLog.Text("Killing all connections and running processes");
            foreach (Thread curr in ThreadDB)
            {
                if (curr.ThreadState == ThreadState.WaitSleepJoin || curr.ThreadState == ThreadState.Running)
                {
                    curr.Abort();
                }
            }
            SystemLog.Text("Exiting");
        }

        public static void handleException(Exception reason, bool ThreadOK = false)
        {
            if (reason.GetType() == typeof(ThreadAbortException) && ThreadOK)
            {
                return;
            }
            SystemLog.Text("Exception: " + reason.Message + " " + reason.StackTrace + " in: " + reason.Source);
        }

        /// <summary>
        /// This function load the services
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            try
            {
                SystemLog.Text("Pidgeon services " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " loading");
                SystemLog.Text("OS: " + Environment.OSVersion.ToString());

                LoadConf();

                if (!File.Exists(Configuration._System.CertificatePath) && Configuration.Network.UsingSSL)
                {
                    try
                    {
                        SystemLog.Text("There is no certificate file, creating one now");
                        GenerateCertificate(Configuration._System.CertificatePath, "pidgeonclient.org");
                    }
                    catch (Exception fail)
                    {
                        Core.handleException(fail);
                        SystemLog.Text("Unable to create cert file, ssl disabled");
                        Configuration.Network.UsingSSL = false;
                    }
                }

                SystemLog.Text("This instance of pidgeon services has following parameters:");
                SystemLog.Text("-----------------------------------------------------------");
                SystemLog.Text("Port: " + Configuration.Network.ServerPort.ToString());
                SystemLog.Text("WD: " + Directory.GetCurrentDirectory());
                if (Configuration._System.MaxFileChunkSize == 0)
                {
                    SystemLog.Text("Maximum file chunk size: unlimited");
                }
                else
                {
                    SystemLog.Text("Maximum file chunk size: " + Configuration._System.MaxFileChunkSize.ToString());
                }
                SystemLog.Text("Minimum buffer size: " + Configuration._System.MinimumBufferSize.ToString());
                SystemLog.Text("Minimum chunk size: " + Configuration._System.ChunkSize.ToString());
                SystemLog.Text("SSL is enabled: " + Configuration.Network.UsingSSL.ToString());
                if (Configuration.Network.UsingSSL)
                {
                    SystemLog.Text("SSL port: " + Configuration.Network.ServerSSL.ToString());
                }
                else
                {
                    SystemLog.Text("SSL port: none");
                }

                SystemLog.Text("-----------------------------------------------------------");

                LoadUser();

                return true;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                SystemLog.Text("Fatal error - exiting");
                return false;
            }
        }

        /// <summary>
        /// This function create a new certificate it is basically called only once
        /// </summary>
        /// <param name="name"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static bool GenerateCertificate(string name, string host)
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
    }
}
