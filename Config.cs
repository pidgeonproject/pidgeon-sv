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
using System.Text;

namespace pidgeon_sv
{
    class Config
    {
        /// <summary>
        /// Port
        /// </summary>
        public static int server_port = 22;
        public static string userfile = "db/users";

        /// <summary>
        /// This is a minimal size of one chunk before it's written to storage, to free a memory
        /// </summary>
        public static int chunk = 200;

        /// <summary>
        /// This is a maximal size of one chunk. If it's not 0 system will freeze in case that current buffer - minbs will be more than this value.
        /// </summary>
        public static int MaxChunk = 0;

        /// <summary>
        /// Maximum buffer size before flush
        /// </summary>
        public static int maxbs
        {
            get
            {
                return minbs + chunk;
            }
        }
        /// <summary>
        /// Minimal buffer size to store, this HAVE to be lower than maximum buffer
        /// </summary>
        public static int minbs = 2000;


        public static readonly string version = "1.0.2.0";
    }
}
