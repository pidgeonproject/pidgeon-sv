using System;
using System.Collections.Generic;
using System.Text;

namespace pidgeon_sv
{
    public class SecurityRole
    {
        public List<Account.UserLevel> AuthorizedLevels
        {
            get
            {
                lock (Authorized)
                {
                    List<Account.UserLevel> x = new List<Account.UserLevel>();
                    x.AddRange(Authorized);
                    return x;
                }
            }
        }

        private List<Account.UserLevel> Authorized = new List<Account.UserLevel>();

        public SecurityRole(Account.UserLevel level)
        {
            Authorized.Add(level);
        }
    }

    public class SecurityLayers
    {
        public static SecurityRole CreateUser = new SecurityRole(Account.UserLevel.Admin);
        public static SecurityRole DeleteUser = new SecurityRole(Account.UserLevel.Admin);
        public static SecurityRole RestartSystem = new SecurityRole(Account.UserLevel.Root);
        public static SecurityRole ModifyUser = new SecurityRole(Account.UserLevel.Admin);
        public static SecurityRole ReadUser = new SecurityRole(Account.UserLevel.Admin);

        public static bool isAuthorized(Account user, SecurityRole role)
        {
            if (user.Level == Account.UserLevel.Root)
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
