using System;

namespace Mercenary.Core.Engine
{
    public static class Extensions
    {
        public static string Capitalize(this String s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                return char.ToUpper(s[0]) + s.Substring(1).ToLower();
            }
            return s;
        }
    }
}
