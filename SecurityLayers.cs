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
    public class SecurityRole
    {
        public List<SystemUser.UserLevel> AuthorizedLevels
        {
            get
            {
                lock (Authorized)
                {
                    List<SystemUser.UserLevel> x = new List<SystemUser.UserLevel>();
                    x.AddRange(Authorized);
                    return x;
                }
            }
        }

        private List<SystemUser.UserLevel> Authorized = new List<SystemUser.UserLevel>();

        public SecurityRole(SystemUser.UserLevel level)
        {
            Authorized.Add(level);
        }
    }

    public class SecurityLayers
    {
        public static SecurityRole CreateUser = new SecurityRole(SystemUser.UserLevel.Admin);
        public static SecurityRole DeleteUser = new SecurityRole(SystemUser.UserLevel.Admin);
        public static SecurityRole RestartSystem = new SecurityRole(SystemUser.UserLevel.Root);
        public static SecurityRole ModifyUser = new SecurityRole(SystemUser.UserLevel.Admin);
        public static SecurityRole ReadUser = new SecurityRole(SystemUser.UserLevel.Admin);
        public static SecurityRole ShutdownSystem = new SecurityRole(SystemUser.UserLevel.Root);

        public static bool isAuthorized(SystemUser user, SecurityRole role)
        {
            if (user.Level == SystemUser.UserLevel.Root)
            {
                return true;
            }

            if (role.AuthorizedLevels.Contains(user.Level))
            {
                return true;
            }

            return false;
        }
    }
}
