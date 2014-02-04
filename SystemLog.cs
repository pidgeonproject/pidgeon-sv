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

namespace pidgeon_sv
{
    public class SystemLog
    {
		/// <summary>
		/// Log the error to system log
		/// </summary>
		/// <param name='Message'>
		/// Message.
		/// </param>
        public static void Error(string Message)
        {
            if (Configuration.Logging.Terminal)
            {
                Console.WriteLine(DateTime.Now.ToString() + " [ERROR]: " + Message);
                return;
            }
            Core.Writer.Insert(DateTime.Now.ToString() + " [ERROR]: " + Message, Configuration.Logging.Log);
        }

        public static void Warning(string Message)
        {
            if (Configuration.Logging.Terminal)
            {
                Console.WriteLine(DateTime.Now.ToString() + " [WARNING]: " + Message);
                return;
            }
            Core.Writer.Insert(DateTime.Now.ToString() + " [WARNING]: " + Message, Configuration.Logging.Log);
        }

		/// <summary>
		/// Log the text message
		/// </summary>
		/// <param name='Message'>
		/// Message.
		/// </param>
        public static void WriteLine(string Message)
        {
            if (Configuration.Logging.Terminal)
            {
                Console.WriteLine(DateTime.Now.ToString() + ": " + Message);
                return;
            }
            Core.Writer.Insert(DateTime.Now.ToString() + ": " + Message, Configuration.Logging.Log);
        }

		public static void DebugLog(string text, int verbosity = 1)
        {
            if (verbosity <= Configuration.Debugging.Verbosity)
			{
                SystemLog.WriteLine ("DEBUG {" + verbosity.ToString() + "}: " + text);
            }
        }
    }
}

