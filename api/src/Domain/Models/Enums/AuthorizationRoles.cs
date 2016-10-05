using System;

namespace Foundatio.Skeleton.Domain.Models
{
    public static class AuthorizationRoles
    {
        public const string Client = "client";
        public const string User = "user";
        public const string Admin = "admin";
        public const string GlobalAdmin = "global";
        public static readonly string[] AllScopes = { Client, User, Admin, GlobalAdmin };

        public static readonly string[] ClientScope = { Client };
        public static readonly string[] UserScope = { Client, User };
        public static readonly string[] AdminScope = { Client, User, Admin };
        public static readonly string[] GlobalAdminScope = AllScopes;

        public static string[] GetScope(string role)
        {
            switch (role.ToLower())
            {
                case Client: return ClientScope;
                case User: return UserScope;
                case Admin: return AdminScope;
                case GlobalAdmin: return GlobalAdminScope;
                default: return new string[0];
            }
        }
    }
}