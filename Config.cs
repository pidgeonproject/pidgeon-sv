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

using System.IO;
using System;
using System.Collections.Generic;

namespace pidgeon_sv
{
    /// <summary>
    /// Configuration of application
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Network
        /// </summary>
        public class Network
        {
            /// <summary>
            /// Port
            /// </summary>
            public static int ServerPort = 64530;
            /// <summary>
            /// Port SSL
            /// </summary>
            public static int ServerSSL = 22432;
            /// <summary>
            /// Port for internal data transfers between the components of services
            /// </summary>
            public static int IPC = 8428;
            /// <summary>
            /// Using SSL
            /// </summary>
            public static bool UsingSSL = true;
        }

        /// <summary>
        /// Debugging
        /// </summary>
        public class Debugging
        {
            /// <summary>
            /// Verbosity
            /// </summary>
            public static int Verbosity = 0;
        }

        public class Logging
        {
            /// <summary>
            /// Log
            /// </summary>
            public static string Log = "/var/log/pidgeonsv.log";
            public static bool Colors = true;
            public static bool Terminal = false;
        }

        /// <summary>
        /// _System
        /// </summary>
        public class _System
        {
            /// <summary>
            /// Name of file where user information is stored
            /// </summary>
            public static string UserFile = "users.xml";
            /// <summary>
            /// File that contains the password of system user, every user who is able to read it
            /// will be able to manage this instance
            /// </summary>
            public static string PasswordFile = "password";

            /// <summary>
            /// Configuration file
            /// </summary>
            public static readonly string ConfigurationFile = "pidgeon.xml";

            /// <summary>
            /// This is a minimal size of one chunk before it's written to storage, to free a memory
            /// </summary>
            public static int ChunkSize = 200;

            /// <summary>
            /// This is a maximal size of one chunk. If it's not 0 system will freeze in case that current buffer - minbs will be more than this value.
            /// </summary>
            public static int MaxFileChunkSize = 0;

            /// <summary>
            /// If this is true all users with unknown user level will be considered as root
            /// </summary>
            public static bool Rooted = false;

            /// <summary>
            /// Maximum buffer size before flush
            /// </summary>
            public static int MaximumBufferSize
            {
                get
                {
                    return MinimumBufferSize + ChunkSize;
                }
            }

            /// <summary>
            /// Database folder (where the user database is located)
            /// </summary>
            public static string DatabaseFolder = "data";

            /// <summary>
            /// Default folder where the user temporary data are stored
            /// </summary>
            public static string FileDBDefaultFolder = "buffers";

            /// <summary>
            /// Certificate path
            /// </summary>
            public static string CertificatePath = "server.pfx";

            /// <summary>
            /// Running as a daemon
            /// </summary>
            public static bool Daemon = false;

            /// <summary>
            /// Minimal buffer size to store
            /// </summary>
            public static int MinimumBufferSize = 800;

            /// <summary>
            /// Version of application
            /// </summary>
            public static string PidgeonSvVersion
            {
                get
                {
                    return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
            }
        }

        public static void Init()
        {
            Configuration._System.UserFile = Configuration._System.DatabaseFolder +
                                                 Path.DirectorySeparatorChar +
                                                 Configuration._System.UserFile;

            Configuration._System.PasswordFile = Configuration._System.DatabaseFolder +
                                                 Path.DirectorySeparatorChar +
                                                 Configuration._System.PasswordFile;
        }
    }
}
