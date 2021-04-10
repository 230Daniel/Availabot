using System;
using System.Linq;

namespace Availabot.Extensions
{
    public static class StringExtensions
    {
        public static string Title(this string input) =>
            input switch
            {
                null => null,
                "" => "",
                _ => input.First().ToString().ToUpper() + input.Substring(1)
            };
    }
}
