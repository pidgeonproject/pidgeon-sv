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
            SystemLog.WriteLine(" [ERROR]: " + Message, false, ConsoleColor.Red);
        }

        public static void Warning(string Message)
        {
            SystemLog.WriteLine(" [WARNING]: " + Message, false, ConsoleColor.DarkYellow);
        }

        /// <summary>
        /// Log the text message
        /// </summary>
        /// <param name='Message'>
        /// Message.
        /// </param>
        public static void WriteLine(string Message, bool Suffix = true, ConsoleColor Color = ConsoleColor.Black)
        {
            string suffix = ": ";
            if (!Suffix)
            {
                suffix = "";
            }
            if (Configuration.Logging.Terminal)
            {
                if (!Configuration.Logging.Colors)
                {
                    Console.WriteLine(DateTime.Now.ToString() + suffix + Message);
                } else
                {
                    ConsoleColor color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(DateTime.Now.ToString());
                    if (Color == ConsoleColor.Black)
                    {
                        Console.ForegroundColor = color;
                        Console.WriteLine(suffix + Message);
                    } else
                    {
                        Console.ForegroundColor = Color;
                        Console.WriteLine(suffix + Message);
                        Console.ForegroundColor = color;
                    }
                }
                return;
            }
            Core.Writer.Insert(DateTime.Now.ToString() + suffix + Message, Configuration.Logging.Log);
        }

        public static void DebugLog(string text, int verbosity = 1)
        {
            if (verbosity <= Configuration.Debugging.Verbosity)
            {
                SystemLog.WriteLine(" DEBUG {" + verbosity.ToString() + "}: " + text, false, ConsoleColor.Green);
            }
        }
    }
}
