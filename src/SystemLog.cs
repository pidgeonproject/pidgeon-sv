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
using System.Diagnostics;
using System.Threading;

namespace pidgeon_sv
{
    public class MessageLine
    {
        public string text;
        public DateTime date;
        public Type type;
        
        public MessageLine(string Text)
        {
            this.text = Text;
            this.date = DateTime.Now;
            this.type = Type.Text;
        }
        
        public enum Type
        {
            Warning,
            Error,
            Text,
            Debug
        }
        
        public MessageLine(string Text, DateTime Date, Type Type)
        {
            this.type = Type;
            this.date = Date;
            this.text = Text;
        }
    }
    
    public class SystemLog
    {
        private static Thread thread = null;
        public static List<MessageLine> data = new List<MessageLine>();
        
        private static void flush()
        {
            List<MessageLine> lines = new List<MessageLine>();
            lock (data)
            {
                lines.AddRange(data);
                data.Clear();
            }
            foreach (MessageLine line in lines)
            {
                ConsoleColor color = Console.ForegroundColor;
                bool suffix = false;
                switch (line.type)
                {
                    case MessageLine.Type.Debug:
                        color = ConsoleColor.Green;
                        break;
                    case MessageLine.Type.Error:
                        color = ConsoleColor.Red;
                        break;
                    case MessageLine.Type.Warning:
                        color = ConsoleColor.DarkYellow;
                        break;
                    case MessageLine.Type.Text:
                        suffix = true;
                        break;
                }
                SystemLog.WriteLineWithDate(line.text, line.date, suffix, color);
            }
        }
        
        private static void writer()
        {
            do
            {
                flush();
                Thread.Sleep(200);
            } while (Core.IsRunning);
            Configuration.Logging.ThreadWrite = false;
            flush();
            if (Configuration.Debugging.Verbosity > 0)
            {
                SystemLog.DebugLog("Writer thread is down");
            }
            ThreadPool.UnregisterThis();
        }
        
        public static void Init()
        {
            thread = new Thread(writer);
            thread.Name = "writer";
            thread.Start();
            ThreadPool.RegisterThread(thread);
        }
        
        /// <summary>
        /// Log the error to system log
        /// </summary>
        /// <param name='Message'>
        /// Message.
        /// </param>
        public static void Error(string Message)
        {
            if (Configuration.Logging.ThreadWrite)
            {
                lock (data)
                {
                    data.Add(new MessageLine(" [ERROR]: " + Message, DateTime.Now, MessageLine.Type.Error));
                }
                return;
            }
            SystemLog.WriteLine(" [ERROR]: " + Message, false, ConsoleColor.Red);
        }

        public static void Warning(string Message)
        {
            if (Configuration.Logging.ThreadWrite)
            {
                lock (data)
                {
                    data.Add(new MessageLine(" [WARNING]: " + Message, DateTime.Now, MessageLine.Type.Warning));
                }
                return;
            }
            SystemLog.WriteLine(" [WARNING]: " + Message, false, ConsoleColor.DarkYellow);
        }

        public static void WriteNow(string Message, bool Suffix = true, ConsoleColor Color = ConsoleColor.Black)
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
            }
            // TODO: fix this
            if (Configuration.Logging.Log != "none")
            {
                Writer.Insert(DateTime.Now.ToString() + suffix + Message, Configuration.Logging.Log);
            }
        }

        /// <summary>
        /// Log the text message
        /// </summary>
        /// <param name='Message'>
        /// Message.
        /// </param>
        public static void WriteLine(string Message, bool Suffix = true, ConsoleColor Color = ConsoleColor.Black)
        {
            if (Configuration.Logging.ThreadWrite)
            {
                lock (data)
                {
                    data.Add(new MessageLine(Message));
                }
                return;
            }
            WriteNow(Message, Suffix, Color);
        }
        
        /// <summary>
        /// Log the text message
        /// </summary>
        /// <param name='Message'>
        /// Message.
        /// </param>
        public static void WriteLineWithDate(string Message, DateTime Date, bool Suffix = true, ConsoleColor Color = ConsoleColor.Black)
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
                    Console.Write(Date.ToString());
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
            }
            if (Configuration.Logging.Log != "none")
            {
                Writer.Insert(Date.ToString() + suffix + Message, Configuration.Logging.Log);
            }
        }

        public static void DebugLog(string text, int verbosity = 1)
        {
            if (verbosity <= Configuration.Debugging.Verbosity)
            {
                if (Configuration.Logging.ThreadWrite)
                {
                    lock (data)
                    {
                        data.Add(new MessageLine(" DEBUG {" + verbosity.ToString() + "}: " + text,
                                                 DateTime.Now, MessageLine.Type.Debug));
                    }
                    return;
                }
                SystemLog.WriteLine(" DEBUG {" + verbosity.ToString() + "}: " + text, false, ConsoleColor.Green);
            }
        }
    }
}
