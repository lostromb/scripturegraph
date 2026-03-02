using Durandal.API;
using Durandal.Common.Collections;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace ScriptureGraph.Core.Schemas
{
    public class HtmlTaggedString : IEquatable<HtmlTaggedString>
    {
        public required string Text { get; set; }
        public required List<HtmlTagWithIndex> Tags { get; init; }

        public override string ToString()
        {
            string returnVal = Text;
            for (int tagIndex = Tags.Count - 1; tagIndex >= 0; tagIndex--)
            {
                HtmlTagWithIndex tag = Tags[tagIndex];
                returnVal = returnVal.Insert(tag.Index, tag.Tag.ToString());
            }

            return returnVal;
        }

        public bool Equals(HtmlTaggedString? other)
        {
            if (other is null)
            {
                return false;
            }

            if (!string.Equals(Text, other.Text, StringComparison.Ordinal))
            {
                return false;
            }

            if (Tags.Count != other.Tags.Count)
            {
                return false;
            }

            for (int tagIdx = 0; tagIdx < Tags.Count; tagIdx++)
            {
                if (!Tags[tagIdx].Equals(other.Tags[tagIdx]))
                {
                    return false;
                }
            }

            return true;
        }

        public HtmlTaggedString Substring(int startIndex)
        {
            return Substring(startIndex, Text.Length - startIndex);
        }

        public HtmlTaggedString Substring(int startIndex, int length)
        {
            List<HtmlTagWithIndex> substringTags = new List<HtmlTagWithIndex>();
            Deque<HtmlTagWithIndex> tagStack = new Deque<HtmlTagWithIndex>();
            int endIndex = startIndex + length;

            bool pushedInitialTags = false;
            bool reachedEndIndex = false;
            int tagLevelsToClose = 0;
            for (int tagIdx = 0; tagIdx < Tags.Count; tagIdx++)
            {
                HtmlTagWithIndex thisTag = Tags[tagIdx];
                HtmlTagWithIndex thisTagNewIndex = new HtmlTagWithIndex()
                {
                    Tag = thisTag.Tag,
                    Index = Math.Min(length, Math.Max(0, thisTag.Index - startIndex))
                };

                if (thisTag.Index >= startIndex)
                {
                    if (!pushedInitialTags)
                    {
                        foreach (var bufferedTag in tagStack)
                        {
                            substringTags.Add(bufferedTag);
                        }

                        pushedInitialTags = true;
                    }

                    if (thisTag.Index < endIndex)
                    {
                        substringTags.Add(thisTagNewIndex);
                    }
                }

                if (!reachedEndIndex && thisTag.Index >= endIndex)
                {
                    tagLevelsToClose = tagStack.Count;
                    reachedEndIndex = true;
                }


                if (thisTag.Tag.IsClosing)
                {
                    if (tagStack.Count == tagLevelsToClose)
                    {
                        substringTags.Add(thisTagNewIndex);
                        tagLevelsToClose--;
                    }

                    tagStack.RemoveFromBack();
                }
                else
                {
                    tagStack.AddToBack(thisTagNewIndex);
                }
            }

            return new HtmlTaggedString()
            {
                Text = Text.Substring(startIndex, length),
                Tags = substringTags
            };
        }

        public static HtmlTaggedString Parse(string inputHtml)
        {
            using (PooledStringBuilder plainTextBuilderPooled = StringBuilderPool.Rent())
            {
                StringBuilder plainTextBuilder = plainTextBuilderPooled.Builder;
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(inputHtml);
                HtmlNodeNavigator? navigator = html.CreateNavigator() as HtmlNodeNavigator;
                List<HtmlTagWithIndex> tags = new List<HtmlTagWithIndex>();
                RecurseElements(navigator!, plainTextBuilder, tags);

                return new HtmlTaggedString()
                {
                    Tags = tags,
                    Text = plainTextBuilder.ToString(),
                };
            }
        }

        private static void RecurseElements(
            HtmlNodeNavigator current,
            StringBuilder plainTextBuilder,
            List<HtmlTagWithIndex> tags)
        {
            foreach (var descendant in current.SelectChildren(XPathNodeType.All))
            {
                if (!(descendant is HtmlNodeNavigator descNavi))
                {
                    continue;
                }

                if (descNavi.NodeType == XPathNodeType.Text)
                {
                    //Console.WriteLine(descNavi.Value);
                    plainTextBuilder.Append(descNavi.Value);
                }
                else if (descNavi.NodeType == XPathNodeType.Whitespace)
                {
                    //Console.WriteLine("(Whitespace)");
                    plainTextBuilder.Append(descNavi.Value);
                }
                else if (descNavi.NodeType == XPathNodeType.Element)
                {
                    int textIndex = plainTextBuilder.Length;
                    string tagName = descNavi.Name;
                    IDictionary<string, string> tagAttrs = new SmallDictionary<string, string>();
                    if (descNavi.HasAttributes)
                    {
                        XPathNavigator attrNavigator = descNavi.Clone();
                        while (attrNavigator.MoveToNextAttribute())
                        {
                            tagAttrs[attrNavigator.LocalName] = attrNavigator.Value;
                        }
                    }

                    //Console.WriteLine($"<{tagName}>");
                    tags.Add(new HtmlTagWithIndex()
                    {
                        Index = textIndex,
                        Tag = new HtmlTag()
                        {
                            TagName = tagName,
                            IsClosing = false,
                            Attributes = tagAttrs,
                        }
                    });

                    RecurseElements(descNavi, plainTextBuilder, tags);

                    textIndex = plainTextBuilder.Length;
                    //Console.WriteLine($"</{tagName}>");
                    tags.Add(new HtmlTagWithIndex()
                    {
                        Index = textIndex,
                        Tag = new HtmlTag()
                        {
                            TagName = tagName,
                            IsClosing = true,
                            Attributes = new SmallDictionary<string, string>(0),
                        }
                    });
                }
                else
                {
                    //Console.WriteLine("ELEMENT " + descNavi.NodeType);
                }
            }
        }
    }
}
