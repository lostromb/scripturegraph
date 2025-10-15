using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    internal static class MoreStringUtils
    {
        public static string RegexGroupReplace(Regex expression, string input, Func<GroupCollection, string> replacementDelegate, int maxReplacements = -1)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            MatchCollection matchCollection = expression.Matches(input);
            string text = string.Empty;
            int num = 0;
            int num2 = 0;
            foreach (Match item in matchCollection)
            {
                text += input.Substring(num, item.Index - num);
                num = item.Index + item.Length;
                text += replacementDelegate(item.Groups);
                num2++;
                if (maxReplacements > 0 && num2 >= maxReplacements)
                {
                    break;
                }
            }

            return text + input.Substring(num);
        }
    }
}
