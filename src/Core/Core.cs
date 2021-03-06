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
        public static List<string> Parameters = null;
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
        /// Exact point in time when the process was started
        /// </summary>
        public static DateTime StartTime;
        /// <summary>
        /// List of all existing accounts in system
        /// </summary>
        public static List<SystemUser> UserList = new List<SystemUser>();

        /// <summary>
        /// Quit
        /// </summary>
        public static void Quit()
        {
            SystemLog.WriteLine("Killing all connections and running processes");
            foreach (Thread curr in ThreadPool.Threads)
            {
                if (curr.ThreadState == ThreadState.WaitSleepJoin || curr.ThreadState == ThreadState.Running)
                {
                    curr.Abort();
                }
            }
            SystemLog.WriteLine("Exiting");
        }
        
        public static void Halt()
        {
            Core.isRunning = false;
        }

        public static void handleException(Exception reason, bool ThreadOK = false)
        {
            if (reason.GetType() == typeof(ThreadAbortException) && ThreadOK)
            {
                return;
            }
            SystemLog.Error("Exception: " + reason.Message + " " + reason.StackTrace + " in: " + reason.Source);
        }

        /// <summary>
        /// This function load the services
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            try
            {
                SystemLog.WriteLine("Pidgeon services " + Configuration.Services.PidgeonSvVersion + " loading");
                SystemLog.WriteLine("OS: " + Environment.OSVersion.ToString());
                LoadConf();
                if (!File.Exists(Configuration.Services.CertificatePath) && Configuration.Network.UsingSSL)
                {
                    try
                    {
                        SystemLog.WriteLine("There is no certificate file, creating one now");
                        GenerateCertificate(Configuration.Services.CertificatePath, "pidgeonclient.org");
                    }
                    catch (Exception fail)
                    {
                        Core.handleException(fail);
                        SystemLog.Error("Unable to create cert file, ssl disabled");
                        Configuration.Network.UsingSSL = false;
                    }
                }
                SystemLog.WriteLine("This instance of pidgeon services has following parameters:");
                SystemLog.WriteLine("-----------------------------------------------------------");
                SystemLog.WriteLine("Port: " + Configuration.Network.ServerPort.ToString());
                SystemLog.WriteLine("WD: " + Directory.GetCurrentDirectory());
                if (Configuration.Services.MaxFileChunkSize == 0)
                {
                    SystemLog.WriteLine("Maximum file chunk size: unlimited");
                }
                else
                {
                    SystemLog.WriteLine("Maximum file chunk size: " + Configuration.Services.MaxFileChunkSize.ToString());
                }
                SystemLog.WriteLine("Minimum buffer size: " + Configuration.Services.MinimumBufferSize.ToString());
                SystemLog.WriteLine("Minimum chunk size: " + Configuration.Services.ChunkSize.ToString());
                SystemLog.WriteLine("SSL is enabled: " + Configuration.Network.UsingSSL.ToString());
                if (Configuration.Network.UsingSSL)
                {
                    SystemLog.WriteLine("SSL port: " + Configuration.Network.ServerSSL.ToString());
                }
                else
                {
                    SystemLog.WriteLine("SSL port: none");
                }
                SystemLog.WriteLine("-----------------------------------------------------------");
                LoadUser();
                return true;
            }
            catch (Exception fail)
            {
                Core.handleException(fail);
                SystemLog.WriteLine("Fatal error - exiting");
                return false;
            }
        }

        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
         
            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
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
                DateTime.Parse("2090-01-01"), //not valid after
                "pidgeon"); //password to encrypt key file

            using (BinaryWriter binWriter = new BinaryWriter(File.Open(name, FileMode.Create)))
            {
                binWriter.Write(c);
            }
            return true;
        }
    }
}
