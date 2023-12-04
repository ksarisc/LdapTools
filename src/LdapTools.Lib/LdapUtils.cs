using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace LdapTools.Lib
{
    public static class LdapUtils
    {
        internal const string member = "member";
        internal const string name = "samaccountname";
        internal const string sid = "objectSid";

        public static void WindowsCheck()
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                throw new InvalidOperationException("This version of " + nameof(LdapUtils) + " is only implemented on Windows platforms");
            }
        }

        public static DirectorySearcher CreateSearcher(DirectoryEntry entry, string filter, int pageSize)
        {
            WindowsCheck();
            var search = new DirectorySearcher(entry, filter) { Asynchronous = true, PageSize = pageSize, };
            search.PropertiesToLoad.Clear();
            search.PropertiesToLoad.Add(name);
            search.PropertiesToLoad.Add(sid);
            search.PropertiesToLoad.Add(member);
            return search;
        }
        public static DirectorySearcher CreateGroupSearcher(this DirectoryEntry root, string groupName, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentNullException(nameof(groupName), "Group Name REQUIRED");

            var filter = "(&(objectClass=group)(" + name + "=" + groupName.Replace(")", "") + "))";
            return CreateSearcher(root, filter, pageSize);
        } // END CreateGroupSearcher

        //public static void FindGroup(DirectoryEntry root, string groupName, Field[] fields)
        //{
        //    if (string.IsNullOrWhiteSpace(groupName))
        //        throw new ArgumentNullException(nameof(groupName), "Group Name REQUIRED");
        //    fields ??= Array.Empty<Field>();

        //    var filter = "(&(objectClass=group)(" + name + "=" + groupName.Replace(")", "") + "))";

        //    using var searcher = CreateSearcher(root, filter, defaultPageSize);
        //    var result = searcher.FindOne();
        //    if (result == null) return;

        //    // parse it out

        //} // END FindGroup

        //public static IEnumerable<Dictionary<string, object>> ListMembers(SearchResult? result, Field[] fields, int depth = 0)
        //{
        //}

        public static string GetStringProp(this DirectoryEntry self, string propName)
        {
            try
            {
                var temp = self.Properties[propName];
                if (temp != null && temp.Value != null)
                    return temp.Value.ToString() ?? string.Empty;
            }
            catch (Exception) { }
            return string.Empty;
        }

        #region Type Check
        internal static bool IsMatch(DirectoryEntry entry, string value)
        {
            if (entry == null) return false;

            try
            {
                return entry.Properties["objectClass"]?.Contains(value) == true;
            }
            catch(Exception ex)
            {
                //Console.Error.WriteLine(ex.Message);
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debug.WriteLine($"LDAP.Is `{value}` ERR: {ex.Message}");
            }
            return false;
        }

        public static bool IsGroup(this DirectoryEntry self) => IsMatch(self, "group");
        public static bool IsUser(this DirectoryEntry self) => IsMatch(self, "user");
        #endregion Type Check
    }
}
