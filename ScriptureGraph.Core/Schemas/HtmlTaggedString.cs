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
    public class HtmlTaggedString
    {
        public required string Text { get; set; }
        public required List<HtmlTagWithIndex> Tags { get; init; }

        public static HtmlTaggedString Parse(string inputHtml)
        {
            using (PooledStringBuilder plainTextBuilderPooled = StringBuilderPool.Rent())
            {
                StringBuilder plainTextBuilder = plainTextBuilderPooled.Builder;
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(inputHtml);
                HtmlNodeNavigator? navigator = html.CreateNavigator() as HtmlNodeNavigator;
                List<HtmlTagWithIndex> tags = new List<HtmlTagWithIndex>();
                RecurseElements(navigator, plainTextBuilder, tags);

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
