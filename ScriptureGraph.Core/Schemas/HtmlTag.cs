using Durandal.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public class HtmlTag : IEquatable<HtmlTag>
    {
        public required string TagName { get; init; }
        public required bool IsClosing { get; init; }
        public required IDictionary<string, string> Attributes { get; init; }

        public bool Equals(HtmlTag? other)
        {
            if (other is null)
            {
                return false;
            }

            if (IsClosing != other.IsClosing)
            {
                return false;
            }

            if (!string.Equals(TagName, other.TagName, StringComparison.Ordinal))
            {
                return false;
            }

            if (Attributes.Count != other.Attributes.Count)
            {
                return false;
            }

            foreach (var attr in Attributes)
            {
                string? otherAttr;
                if (!other.Attributes.TryGetValue(attr.Key, out otherAttr))
                {
                    return false;
                }

                if (!string.Equals(attr.Value, otherAttr, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            if (IsClosing)
            {
                return $"</{TagName}>";
            }
            else
            {
                if (Attributes.Count > 0)
                {
                    return $"<{TagName} {string.Join(" ", Attributes.Select(s => s.Key + "=\"" + s.Value + "\""))}>";
                }
                else
                {
                    return $"<{TagName}>";
                }
            }
        }


    }
}
