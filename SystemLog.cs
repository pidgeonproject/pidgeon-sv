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
        public static void Error(string Message)
        {
            if (!Configuration._System.Daemon)
            {
                Console.WriteLine(DateTime.Now.ToString() + " [ERROR]: " + Message);
                return;
            }
            Core.Writer.Insert(DateTime.Now.ToString() + " [ERROR]: " + Message, Configuration._System.Log);
        }

        public static void Text (string Message)
        {
            if (!Configuration._System.Daemon)
            {
                Console.WriteLine(DateTime.Now.ToString() + ": " + Message);
                return;
            }
            Core.Writer.Insert(DateTime.Now.ToString() + ": " + Message, Configuration._System.Log);
        }
    }
}

