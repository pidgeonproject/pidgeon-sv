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
using System.Threading;
using System.Text;

namespace pidgeon_sv
{
    /// <summary>
    /// Terminal
    /// </summary>
    public class Terminal
    {
        private static ProtocolSv protocol = null;

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
                              + "This is pidgeon services control interface, bellow is a list of available options:\n"
                              + "\n"
                              + "  -h (--help) display this help\n"
                              + "  -v increase verbosity\n"
                              + "  -p (--pid) <file> write a process id to file in parameter\n"
                              + "  -s (--daemon) will start services as a system daemon\n"
                              + "  --manage will manage local instance of services\n"
                              + "  --install will create a system databases\n"
                              + "  -t (--terminal) will log to terminal as well\n"
                              + "  --log [none|syslog|path] path to syslog\n"
                              + "\n"
                              + "for more information see http://pidgeonclient.org/wiki"
                              + "\npidgeon is open source.");
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

        private static void Management()
        {
            Configuration.Logging.Terminal = true;
            if (!File.Exists(Configuration._System.PasswordFile))
            {
                SystemLog.Error("There is no password file, can't continue (did you install services using --install option?)");
                return;
            }
            string username = File.ReadAllText(Configuration._System.PasswordFile);
            username = username.Replace("\n", "");
            string password;
            if (!username.Contains(":"))
            {
                SystemLog.Error("Password file is broken");
                return;
            }
            Configuration.Logging.ThreadWrite = true;
            SystemLog.Init();
            password = username.Substring(username.IndexOf(":") + 1);
            username = username.Substring(0, username.IndexOf(":"));
            protocol = new ProtocolSv();
            protocol.Username = username;
            protocol.Server = "localhost";
            protocol.Password = password;
            protocol.Open();
            SystemLog.WriteLine("Connecting to services on localhost, please wait");
            int retry = 10;
            while (!protocol.IsConnected && retry > 0)
            {
                retry--;
                Thread.Sleep(200);
            }
            if (!protocol.IsConnected)
            {
                SystemLog.Error("Unable to connect to services on localhost, port " + Configuration.Network.ServerPort.ToString());
                Core.Halt();
                return;
            }
            Thread.Sleep(1200);
            while (protocol.IsConnected)
            {
                Console.Write(">>");
                string command = Console.ReadLine();
                string parameters = "";
                if (command.Contains(" "))
                {
                    int ip = command.IndexOf(" ");
                    parameters = command.Substring(ip + 1);
                    command = command.Substring(0, ip);
                }
                switch (command.ToLower())
                {
                    case "":
                        break;
                    case "quit":
                    case "exit":
                        Console.WriteLine("Good bye");
                        protocol.Exit();
                        Core.Halt();
                        return;
                    case "help":
                        Console.WriteLine("You can use any of these commands:\n" +
                                          "quit        - disconnect\n" +
                                          "adduser     - insert a new user to services\n" +
                                          "deluser     - remove a user from services\n" +
                                          "listuser    - display a list of all users\n" +
                                          "sessions    - display a list of all sessions\n" +
                                          "lockuser    - lock a user\n" +
                                          "unlockuser  - unlock a user\n" +
                                          "moduser     - alter a user\n" +
                                          "kill <id>   - kill a session\n");
                        break;
                    case "adduser":
                        CreateUser();
                        break;
                    case "deluser":
                        DeleteUser();
                        break;
                    case "listuser":
                        ListUser();
                        break;
                    case "lockuser":
                        LockUser();
                        break;
                    case "unlockuser":
                        UnlockUser();
                        break;
                    case "sessions":
                        Session();
                        break;
                    case "moduser":
                        break;
                    case "kill":
                        Kill(parameters);
                        break;
                    default:
                        Console.WriteLine("Unknown command, try help if you don't know what to do");
                        break;
                }
            }
            Core.Halt();
        }

        private static void Kill(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                Console.WriteLine("Usage: kill SID (where SID is id of session that you need to end)");
                return;
            }
            protocol.Respond = false;
            ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("SYSTEM", "KILL");
            datagram.Parameters.Add("sid", data);
            protocol.Deliver(datagram);
            while (!protocol.Respond)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(20);
        }

        private static void CreateUser()
        {
            Console.Write("Enter username: ");
            string username;
            username = Console.ReadLine();
            if (username == "")
            {
                username = "http://pidgeonclient.org";
            }
            Console.Write("Enter password: ");
            string password = ReadPw();
            Console.Write("\nEnter default user role (Root | Sysadmin | RegularUser) [RegularUser]: ");
            string level = Console.ReadLine();
            if (level.Length == 0)
            {
                level = "RegularUser";
            }
            Console.Write("Enter nick name [Pidgeon]: ");
            string nickname = Console.ReadLine();
            if (nickname.Replace(" ", "") == "")
            {
                nickname = "Pidgeon";
            }
            Console.Write("Enter real name [Pidgeon]: ");
            string realname = Console.ReadLine();
            if (realname.Replace(" ", "") == "")
            {
                realname = "Pidgeon";
            }
            Console.Write("Enter ident [pidgeon]: ");
            string ident = Console.ReadLine();
            if (ident.Replace(" ", "") == "")
            {
                ident = "pidgeon";
            }
            protocol.Respond = false;
            ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("SYSTEM", "CREATEUSER");
            datagram.Parameters.Add("name", username);
            datagram.Parameters.Add("password", password);
            datagram.Parameters.Add("nickname", nickname);
            datagram.Parameters.Add("role", level);
            datagram.Parameters.Add("realname", realname);
            datagram.Parameters.Add("ident", ident);
            protocol.Deliver(datagram);
            while (!protocol.Respond)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(20);
        }

        private static void UnlockUser()
        {
            string username;
            Console.Write("Enter user to unlock: ");
            username = Console.ReadLine();
            protocol.Respond = false;
            ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("SYSTEM", "UNLOCK");
            datagram.Parameters.Add("id", username);
            protocol.Deliver(datagram);
            while (!protocol.Respond)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(20);
        }

        private static void LockUser()
        {
            string username;
            Console.Write("Enter user to lock: ");
            username = Console.ReadLine();
            protocol.Respond = false;
            ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("SYSTEM", "LOCK");
            datagram.Parameters.Add("id", username);
            protocol.Deliver(datagram);
            while (!protocol.Respond)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(20);
        }

        private static void Session()
        {
            protocol.Respond = false;
            ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("SYSTEM", "SESSION");
            protocol.Deliver(datagram);
            while (!protocol.Respond)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(20);
        }

        private static void DeleteUser()
        {
            string username;
            Console.Write("Enter user to delete: ");
            username = Console.ReadLine();
            protocol.Respond = false;
            ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("SYSTEM", "REMOVE");
            datagram.Parameters.Add("id", username);
            protocol.Deliver(datagram);
            while (!protocol.Respond)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(20);
        }

        public static string FormatToSpecSize(string st, int size)
        {
            if (st.Length > size)
            {
                st = st.Substring(0, st.Length - ((st.Length - size) + 3));
                st += "...";
            } else
            {
                while (st.Length < size)
                {
                    st += " ";
                }
            }
            return st;
        }

        private static void ListUser()
        {
            Console.WriteLine("Retrieving list of users");
            protocol.Respond = false;
            ProtocolMain.Datagram datagram = new ProtocolMain.Datagram("SYSTEM", "LIST");
            protocol.Deliver(datagram);
            while (!protocol.Respond)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(800);
        }

        private static void Install()
        {
            Configuration.Logging.Terminal = true;
            if (File.Exists(Configuration._System.UserFile))
            {
                SystemLog.Error("Not creating a user file because it already exist");
                return;
            }

            if (File.Exists(Configuration._System.PasswordFile))
            {
                SystemLog.Error("Not creating a user file because password file already exist");
                return;
            }

            Random random = new Random((int)DateTime.Now.Ticks);
            string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890$%^&()@!#abcdefghijklmnopqrstuvwxyz_+{}?><[];";
            char[] buffer = new char[20];

            for (int i = 0; i < 20; i++)
            {
                buffer[i] = _chars[random.Next(_chars.Length)];
            }
            string password = new string(buffer);

            SystemUser user = new SystemUser("system", Core.CalculateMD5Hash(password));
            user.Ident = "system";
            user.Nickname = "system";
            user.Role = "System";
            user.RealName = "Pidgeon system";
            Core.UserList.Add(user);
            Core.SaveUser();
            File.WriteAllText(Configuration._System.PasswordFile, "system:" + password.ToString());
            SystemLog.DebugLog("Finished installing of new user list");
        }

        private static void WriteLog(Parameter p)
        {
            if (p.parm == null)
            {
                throw new Exception("Parameter --log requires argument");
            }

            string file = p.parm[0];
            Configuration.Logging.Log = file;
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
                    case "management":
                        Management();
                        return true;
                    case "help":
                        ShowHelp();
                        return true;
                    case "pid":
                        WritePid(parameter);
                        break;
                    case "install":
                        Install();
                        return true;
                    case "-v":
                        Configuration.Debugging.Verbosity++;
                        break;
                    case "daemon":
                        Configuration._System.Daemon = true;
                        break;
                    case "terminal":
                        Configuration.Logging.Terminal = true;
                        break;
                    case "log":
                        WriteLog(parameter);
                        break;
                }
            }

            return false;
        }

        private static Parameter GetParameter(char name)
        {
            bool Read = false;
            string id = null;
            switch (name)
            {
                case 'h':
                    id = "help";
                    Read = true;
                    break;
                case 'v':
                    id = "-v";
                    Read = true;
                    break;
                case 'p':
                    id = "pid";
                    Read = true;
                    break;
                case 't':
                    id = "terminal";
                    Read = true;
                    break;
                case 's':
                    id = "daemon";
                    Read = true;
                    break;
            }
            if (Read)
            {
                Parameter text = new Parameter(id, new List<string>());
                return text;
            }
            return null;
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
                    if (!data.StartsWith("--") && data.StartsWith("-"))
                    {
                        // we got a single char parameter
                        string tx = data.Substring(1);
                        while (tx.Length > 0)
                        {
                            Parameter p = GetParameter(tx[0]);
                            if (p == null)
                            {
                                Console.WriteLine("Unknown parameter: -" + tx[0].ToString());
                                return false;
                            }
                            tx = tx.Substring(1);
                            ParameterList.Add(p);
                        }
                        continue;
                    }
                    bool Read = false;
                    switch (data)
                    {
                        case "--manage":
                            parsed = id;
                            id = "management";
                            Read = true;
                            break;
                        case "--help":
                            parsed = id;
                            id = "help";
                            Read = true;
                            break;
                        case "--verbose":
                            parsed = id;
                            id = "-v";
                            Read = true;
                            break;
                        case "--install":
                            parsed = id;
                            id = "install";
                            Read = true;
                            break;
                        case "--pid":
                            parsed = id;
                            id = "pid";
                            Read = true;
                            break;
                        case "--terminal":
                            parsed = id;
                            id = "terminal";
                            Read = true;
                            break;
                        case "--daemon":
                            parsed = id;
                            id = "daemon";
                            Read = true;
                            break;
                        case "--log":
                            parsed = id;
                            id = "log";
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

