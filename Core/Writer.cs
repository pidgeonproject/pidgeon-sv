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
        public class Writer
        {
            public class Item
            {
                /// <summary>
                /// Text
                /// </summary>
                public string Text;
                /// <summary>
                /// Path
                /// </summary>
                public string FN;

                /// <summary>
                /// Creates a new instance of writer
                /// </summary>
                /// <param name="fn"></param>
                /// <param name="text"></param>
                public Item(string fn, string text)
                {
                    FN = fn;
                    Text = text;
                }
            }

            public static List<Item> DB = new List<Item>();

            public static void Insert(string text, string file)
            {
                lock (DB)
                {
                    DB.Add(new Item(file, text));
                }
            }

            private static void ex()
            {
                try
                {
                    while (Core.IsRunning)
                    {
                        List<Item> list = new List<Item>();
                        lock (DB)
                        {
                            if (DB.Count > 0)
                            {
                                list.AddRange(DB);
                                DB.Clear();
                            }
                        }
                        foreach (Item item in list)
                        {
                            File.AppendAllText(item.FN, item.Text + Environment.NewLine);
                        }
                        System.Threading.Thread.Sleep(2000);
                    }
                }
                catch (Exception fail)
                {
                    Core.handleException(fail);
                }
            }

            public static void Init()
            {
                System.Threading.Thread logger = new Thread(ex);
                logger.Name = "Writer";
                lock (Core.ThreadDB)
                {
                    Core.ThreadDB.Add(logger);
                }
                logger.Start();
            }
        }
    }
}

