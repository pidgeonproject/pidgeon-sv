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

namespace pidgeon_sv
{
    /// <summary>
    /// Terminal
    /// </summary>
    public class Terminal
    {
        private class Parameter
        {
            /// <summary>
            /// The parameter.
            /// </summary>
            public string parameter;
            /// <summary>
            /// Parm
            /// </summary>
            public List<string> parm;

            /// <summary>
            /// Initializes a new instance of the <see cref="Client.Terminal.Parameter"/> class.
            /// </summary>
            /// <param name='_Parameter'>
            /// Name
            /// </param>
            /// <param name='Params'>
            /// Parameters.
            /// </param>
            public Parameter(string _Parameter, List<string> Params)
            {
                parameter = _Parameter;
                parm = Params;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: pidgeon-sv [options]\n"
                              + "********************************\n"
                              + "This is pidgeon services daemon, bellow is a list of available options:\n"
                              + "if this program is started with no parameters, it will start in interactive service mode\n"
                              + "\n"
                              + "\n"
                              + "  -h (--help) display this help\n"
                              + "  -a (--add) insert user\n"
                              + "  -l (--list) list user\n"
                              + "  -v increase verbosity\n"
                              + "  -d (--delete) remove user\n"
                              + "  -p (--pid) <file> write a process id to file in parameter\n"
                              + "  -s (--daemon) will start as a system daemon"
                              + "\n"
                              + "for more information see http://pidgeonclient.org/wiki pidgeon is open source.");
        }

        private static string ReadPw()
        {
            string password = "";
            ConsoleKeyInfo key;
            key = Console.ReadKey(true);
            while (key.Key != ConsoleKey.Enter)
            {
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
                key = Console.ReadKey(true);
            }
            return password;
        }

        private static void CreateUser(Parameter parameter)
        {
            Core.LoadUser(true);
            Console.Write("Enter username: ");
            string username;
            username = Console.ReadLine();
            if (username == "")
            {
                username = "http://pidgeonclient.org";
            }
            Console.Write("Enter password: ");
            string password;
            password = ReadPw();
            Console.Write("\nEnter user level (root | admin | user) [user]: ");
            string level;
            level = Console.ReadLine();
            SystemUser.UserLevel ul = SystemUser.UserLevel.User;

            switch (level)
            {
                case "root":
                    ul = SystemUser.UserLevel.Root;
                    break;
                case "admin":
                    ul = SystemUser.UserLevel.Admin;
                    break;
                case "":
                case "user":
                    ul = SystemUser.UserLevel.User;
                    break;
                default:
                    Console.WriteLine("Invalid level, using user");
                    break;
            }

            Console.Write("Enter real name: ");
            string realname = Console.ReadLine();
            Console.Write("Enter ident [pidgeon]: ");
            string ident = Console.ReadLine();
            if (ident.Replace(" ", "") == "")
            {
                ident = "pidgeon";
            }
            SystemUser user = SystemUser.getUser(username);
            if (user != null)
            {
                Console.WriteLine("This user already exist");
                return;
            }
            user = new SystemUser(username, password);
            user.ident = ident;
            user.Level = ul;
            user.RealName = realname;
            Core.UserList.Add(user);
            Core.SaveUser();
            Console.WriteLine("\n User created");
        }

        private static void DeleteUser(Parameter parameter)
        {
            Core.LoadUser(true);
            string username;
            Console.Write("WARNING: if you delete a user, the running process will not recognize it, if you want to delete the user on-line, you must lock it and remove it before restarting the daemon\n\n");
            Console.Write("Enter user to delete: ");
            username = Console.ReadLine();
            SystemUser user = SystemUser.getUser(username);
            if (user != null)
            {
                Core.UserList.Remove(user);
                Console.WriteLine("User deleted");
                Core.SaveUser();
                return;
            }
            Console.WriteLine("User was not found");
        }

        private static void ListUser()
        {
            Core.LoadUser(true);
            if (Core.UserList.Count == 0)
            {
                return;
            }

            Console.WriteLine("List of all users:\n=========================================\n");
            foreach (SystemUser user in Core.UserList)
            {
                Console.WriteLine(user.UserName + " locked: " + user.IsLocked.ToString() + " name: " + user.RealName);
            }
        }

        private static void WritePid(Parameter p)
        {
            if (p.parm == null)
            {
                throw new Exception("Parameter --pid (-p) requires argument");
            }

            string file = p.parm[0];

            System.IO.File.WriteAllText(file, System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
        }

        private static bool Process(List<Parameter> ls)
        {
            foreach (Parameter parameter in ls)
            {
                switch (parameter.parameter)
                {
                    case "help":
                        ShowHelp();
                        return true;
                    case "add":
                        CreateUser(parameter);
                        return true;
                    case "list":
                        ListUser();
                        return true;
                    case "delete":
                        DeleteUser(parameter);
                        return true;
                    case "pid":
                        WritePid(parameter);
                        break;
                    case "-v":
                        Configuration.Debugging.Verbosity++;
                        break;
                    case "daemon":
                        Configuration._System.Daemon = true;
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Check the parameters of program, return true if we can continue
        /// </summary>
        public static bool Parameters()
        {
            List<string> args = new List<string>();
            foreach (string xx in Core.Parameters)
            {
                args.Add(xx);
            }

            List<Parameter> ParameterList = new List<Parameter>();

            if (args.Count > 0)
            {
                List<string> values = null;
                string id = null;
                string parsed = null;
                foreach (string data in args)
                {
                    bool Read = false;
                    switch (data)
                    {
                        case "--help":
                        case "-h":
                            parsed = id;
                            id = "help";
                            Read = true;
                            break;
                        case "--add":
                        case "-a":
                            parsed = id;
                            id = "add";
                            Read = true;
                            break;
                        case "-l":
                        case "--list":
                            parsed = id;
                            id = "list";
                            Read = true;
                            break;
                        case "-d":
                        case "--delete":
                            parsed = id;
                            id = "delete";
                            Read = true;
                            break;
                        case "-v":
                        case "--verbose":
                            parsed = id;
                            id = "-v";
                            Read = true;
                            break;
                        case "-p":
                        case "--pid":
                            parsed = id;
                            id = "pid";
                            Read = true;
                            break;
                        case "-s":
                        case "--daemon":
                            parsed = id;
                            id = "daemon";
                            Read = true;
                            break;
                    }

                    if (parsed != null)
                    {
                        Parameter text = new Parameter(parsed, values);
                        ParameterList.Add(text);
                        parsed = null;
                        values = null;
                    }

                    if (Read)
                    {
                        continue;
                    }

                    if (values == null)
                    {
                        values = new List<string>();
                    }

                    values.Add(data);
                }

                if (id != null)
                {
                    ParameterList.Add(new Parameter(id, values));
                }

                if (Process(ParameterList))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

