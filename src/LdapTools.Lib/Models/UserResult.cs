using System;
using System.Collections.Generic;

namespace LdapTools.Lib.Models
{
    public class UserResult
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }

        public Dictionary<string, string> AdditionalFields { get; } = new Dictionary<string, string>();
    }
}
