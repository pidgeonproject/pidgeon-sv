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
    public class Role
    {
        private readonly List<string> _permissions = new List<string>();
        /// <summary>
        /// Every role may contain other roles as well
        /// </summary>
        private readonly List<Role> _roles = new List<Role>();
        /// <summary>
        /// The level of role used to compare which role is higher
        /// </summary>
        public int Level;
        public Role(int level_)
        {
            this.Level = level_;
        }

        public void Revoke(string permission)
        {
            lock (this._permissions)
            {
                if (this._permissions.Contains(permission))
                {
                    this._permissions.Remove(permission);
                }
            }
        }

        public void Revoke(Role role)
        {
            lock (this._roles)
            {
                if (this._roles.Contains(role))
                {
                    this._roles.Remove(role);
                }
            }
        }

        public void Grant(Role role)
        {
            lock (this._roles)
            {
                if (!this._roles.Contains(role))
                {
                    this._roles.Add(role);
                }
            }
        }
        
        public void Grant(string permission)
        {
            lock (this._permissions)
            {
                if (!this._permissions.Contains(permission))
                    this._permissions.Add(permission);
            }
        }

        public bool IsPermitted(string permission)
        {
            if (this._permissions.Contains(permission))
                return true;
            lock (this._roles)
            {
                foreach (Role role in _roles)
                {
                    if (role.IsPermitted(permission))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
    
    [Serializable]
    public class Permission
    {
        public static readonly string CreateUser = "CreateUser";
        public static readonly string DeleteUser = "DeleteUser";
        public static readonly string Connect = "Connect";
        public static readonly string ConnectLocal = "ConnectLocal";
        public static readonly string ConnectAll = "ConnectAll";
        public static readonly string ModifyUser = "ModifyUser";
        public static readonly string KickUser = "KickUser";
        public static readonly string DisplaySystemData = "DisplaySystemData";
        public static readonly string ListUsers = "ListUsers";
        public static readonly string LockUser = "LockUser";
        public static readonly string UnlockUser = "UnlockUser";
        public static readonly string DebugCore = "DebugCore";
        public static readonly string Kill = "Kill";
    }

    [Serializable]
    public class Security
    {
        public static Dictionary<string, Role> Roles = new Dictionary<string, Role>();

        public static Role GetRoleFromString(string role)
        {
            lock (Roles)
            {
                if (Roles.ContainsKey(role))
                {
                    return Roles[role];
                }
            }
            return null;
        }

        public static void Initialize()
        {
            lock (Roles)
            {
                Roles.Add("RegularUser", new Role(2));
                Roles["RegularUser"].Grant(Permission.DisplaySystemData);
                Roles["RegularUser"].Grant(Permission.Connect);
                Roles["RegularUser"].Grant(Permission.ConnectAll);
                Roles.Add("System", new Role(10));
                Roles.Add("Sysadmin", new Role(12));
                Roles["System"].Grant(Roles["RegularUser"]);
                Roles["System"].Grant(Permission.CreateUser);
                Roles["System"].Grant(Permission.DeleteUser);
                Roles["System"].Grant(Permission.DisplaySystemData);
                Roles["System"].Grant(Permission.KickUser);
                Roles["System"].Grant(Permission.ListUsers);
                Roles["System"].Grant(Permission.LockUser);
                Roles["System"].Grant(Permission.ModifyUser);
                Roles["System"].Grant(Permission.UnlockUser);
                Roles["System"].Grant(Permission.CreateUser);
                Roles["System"].Grant(Permission.DeleteUser);
                Roles["System"].Grant(Permission.KickUser);
                Roles["System"].Grant(Permission.Kill);
                Roles["System"].Grant(Permission.ListUsers);
                Roles["System"].Grant(Permission.LockUser);
                Roles["System"].Grant(Permission.DebugCore);
                Roles["System"].Grant(Permission.ModifyUser);
                Roles["System"].Grant(Permission.UnlockUser);
                Roles["System"].Grant(Permission.DisplaySystemData);
                Roles["System"].Grant(Permission.ConnectLocal);
                Roles["Sysadmin"].Grant(Roles["System"]);
                Roles["Sysadmin"].Grant(Roles["RegularUser"]);
            }
        }
        
        public static bool HasPermission(string role, string permission)
        {
            if (role == "root")
                   return true;
            if (Roles.ContainsKey(role))
            {
                return Roles[role].IsPermitted(permission);
            }
            return false;
        }
    }
}
