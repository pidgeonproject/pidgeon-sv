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
    class Config
    {
        /// <summary>
        /// Port
        /// </summary>
        public static int server_port = 64530;
        public static string UserFile = "users";

        public static readonly string File = "pidgeon.conf";

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
        public static bool Rooted = true;

        public static bool UsingSSL = true;

        public static int MaxSM = 100;

        /// <summary>
        /// Maximum buffer size before flush
        /// </summary>
        public static int maxbs
        {
            get
            {
                return minbs + ChunkSize;
            }
        }

        public static Mode mode = Mode.Bouncer;

        public static string DatabaseFolder = "db";

        public static string FileDBDefaultFolder = "data";

        public static string CertificatePath = "server.pfx";

        /// <summary>
        /// Minimal buffer size to store, this HAVE to be lower than maximum buffer
        /// </summary>
        public static int minbs = 800;

        public enum Mode
        { 
            Core,
            Bouncer,
        }

        public static string version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
    }
}
