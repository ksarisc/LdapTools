using LdapTools.Lib.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace LdapTools.Lib
{
    public sealed class GroupFinder : IDisposable
    {
        private readonly DirectoryEntry root;
        private readonly Dictionary<string, string>? fields;

        public GroupFinder(int maxSearchResults = 500, Dictionary<string, string>? additionalFields = null)
        {
            LdapUtils.WindowsCheck();
            root = new DirectoryEntry();
            fields = additionalFields;
        }

        public IEnumerable<UserResult> FindUsersOfGroup(string groupName, int maxDepth = 1)
        {
            if (maxDepth < 0) maxDepth = 0;

            using var search = root.CreateGroupSearcher(groupName);
            var result = search.FindOne();
            if (result != null)
            {
                for (int i = 0; i != result.Properties[LdapUtils.member].Count; i++)
                {
                    string? common = null;
                    try { common = result.Properties[LdapUtils.member][i].ToString(); }
                    catch (Exception ex) { }

                    if (string.IsNullOrWhiteSpace(common)) continue;

                    using var entry = new DirectoryEntry("LDAP://" + common);
                    if (entry == null) continue;

                    if (entry.IsGroup() && maxDepth != 1)
                    {
                        int depth = 1;
                        foreach (var u in GetUsers(entry, depth, maxDepth))
                        {
                            yield return u;
                        }
                    }
                    else
                    {
                        var u = ToUser(entry);
                        if (u != null) yield return u;
                    }
                }
            }
        } // END FindUsersOfGroup

        private IEnumerable<UserResult> GetUsers(DirectoryEntry group, int depth, int maxDepth)
        {
            if (depth > maxDepth) yield break;
            //if (!group.IsGroup())

            for (int i = 0; i != group.Properties[LdapUtils.member].Count; i++)
            {
                string? common = null;
                try { common = group.Properties[LdapUtils.member][i]?.ToString(); }
                catch (Exception ex) { }

                if (string.IsNullOrWhiteSpace(common)) continue;

                using var entry = new DirectoryEntry("LDAP://" + common);
                if (entry == null) continue;

                if (entry.IsGroup())
                {
                    foreach (var u in GetUsers(entry, depth + 1, maxDepth))
                    {
                        yield return u;
                    }
                }
                else
                {
                    var u = ToUser(entry);
                    if (u != null) yield return u;
                }
            }
        } // END GetUsers

        public UserResult? ToUser(DirectoryEntry entry)
        {
            if (!entry.IsUser()) return null;

            // check if is user?
            var rslt = new UserResult
            {
                UserName = entry.GetStringProp(LdapUtils.name),
                Email = entry.GetStringProp("mail"),
                FullName = $"{entry.GetStringProp("sn")}, {entry.GetStringProp("givenName")}",
                //UAC=entry.GetStringProp("userAccountControl"),
            };

            if (fields != null)
            {
                foreach (var pair in fields)
                {
                    var value = entry.GetStringProp(pair.Key);
                    rslt.AdditionalFields[pair.Value.Length > 0 ? pair.Value : pair.Key] = value;
                }
            }

            return rslt;
        } // END ToUser

        public void Dispose()
        {
            try { root?.Dispose(); }
            catch (Exception) { }
        }
    }
}
