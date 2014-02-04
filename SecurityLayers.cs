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

namespace pidgeon_sv.Security
{
    [Serializable]
    public class Permission
    {
        public static Dictionary<string, Permission> Permissions = new Dictionary<string, Permission>();
        public static Permission CreateUser = new Permission("CreateUser");
        public static Permission DeleteUser = new Permission("DeleteUser");
        public static Permission Connect = new Permission("Connect");
        public static Permission ModifyUser = new Permission("ModifyUser");
        public static Permission KickUser = new Permission("KickUser");
        public static Permission DisplaySystemData = new Permission("DisplaySystemData");
        public static Permission ListUsers = new Permission("ListUsers");
        public static Permission LockUser = new Permission("LockUser");
        public static Permission UnlockUser = new Permission("UnlockUser");

        public string PermissionName;

        public Permission(string name)
        {
            lock (Permissions)
            {
                if (Permissions.ContainsKey(name))
                {
                    throw new Exception("You can't create multiple permissions with same name");
                }
                Permissions.Add(name, this);
            }
            this.PermissionName = name;
        }

        ~Permission()
        {
            lock (Permissions)
            {
                if (Permissions.ContainsKey(PermissionName))
                {
                    Permissions.Remove(PermissionName);
                }
            }
        }
    }

    [Serializable]
    public class SecurityRole
    {
        public static Dictionary<string, SecurityRole> Roles = new Dictionary<string, SecurityRole>();
        // Built in roles
        public static SecurityRole Root = new SecurityRole("Root");
        public static SecurityRole Administrator = new SecurityRole("Administrators");
        public static SecurityRole RegularUser = new SecurityRole("RegularUser");

        private List<Permission> Permissions = new List<Permission>();
        private string _Name;

        public static SecurityRole GetRoleFromString(string role)
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
            RegularUser.GrantPermission(Permission.DisplaySystemData);
            RegularUser.GrantPermission(Permission.Connect);
            Administrator.GrantPermission(Permission.Connect);
            Administrator.GrantPermission(Permission.CreateUser);
            Administrator.GrantPermission(Permission.DeleteUser);
            Administrator.GrantPermission(Permission.DisplaySystemData);
            Administrator.GrantPermission(Permission.KickUser);
            Administrator.GrantPermission(Permission.ListUsers);
            Administrator.GrantPermission(Permission.LockUser);
            Administrator.GrantPermission(Permission.ModifyUser);
            Administrator.GrantPermission(Permission.UnlockUser);
        }

        public string Name
        {
            get
            {
                return this._Name;
            }
        }

        public SecurityRole(string name)
        {
            lock (Roles)
            {
                if (Roles.ContainsKey(name))
                {
                    throw new Exception("You can't create multiple roles with same name");
                }
                Roles.Add(name, this);
            }
            this._Name = name;
        }

        public bool GrantPermission(Permission permission)
        {
            lock (Permissions)
            {
                if (this.Permissions.Contains(permission))
                {
                    return false;
                }
                this.Permissions.Add(permission);
            }
            return true;
        }

        public bool RevokePermission(Permission permission)
        {
            lock (Permissions)
            {
                if (!this.Permissions.Contains(permission))
                {
                    return false;
                }
                this.Permissions.Remove(permission);
            }
            return true;
        }

        public bool HasPermission(Permission permission)
        {
            lock (Permissions)
            {
                return this.Permissions.Contains(permission);
            }
        }
    }
}
