using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class ConferenceTalkFeatureExtractorNew
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/general-conference\\/(\\d+)\\/(\\d+)\\/(.+?)(?:\\?|$)");

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return;
                }

                // High-level features
                // Title of the talk -> Talk
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(parseResult.TalkTitle))
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                // Talk -> Speaker
                trainingFeaturesOut.Add(new TrainingFeature(
                    parseResult.DocumentEntityId,
                    parseResult.SpeakerEntityId,
                    TrainingFeatureType.EntityReference));

                // Talk -> Conference
                trainingFeaturesOut.Add(new TrainingFeature(
                    parseResult.DocumentEntityId,
                    FeatureToNodeMapping.Conference(parseResult.Conference),
                    TrainingFeatureType.EntityReference));

                // Conference -> Year
                trainingFeaturesOut.Add(new TrainingFeature(
                    FeatureToNodeMapping.Year(parseResult.Conference.Year),
                    FeatureToNodeMapping.Conference(parseResult.Conference),
                    TrainingFeatureType.EntityReference));

                // Year -> Year whatever
                trainingFeaturesOut.Add(new TrainingFeature(
                   FeatureToNodeMapping.Year(parseResult.Conference.Year - 1),
                   FeatureToNodeMapping.Year(parseResult.Conference.Year),
                   TrainingFeatureType.EntityReference));

                Paragraph? previousPara = null;
                foreach (Paragraph para in parseResult.Paragraphs)
                {
                    GospelParagraphClass paraClass = StandardizeClass(para.Class, para.Text);

                    // Associate this paragraph with the entire talk
                    trainingFeaturesOut.Add(new TrainingFeature(
                        para.ParaEntityId,
                        parseResult.DocumentEntityId,
                        TrainingFeatureType.BookAssociation));

                    // And with the previous paragraph
                    if (previousPara != null)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            para.ParaEntityId,
                            previousPara.ParaEntityId,
                            TrainingFeatureType.ParagraphAssociation));
                    }

                    if (paraClass == GospelParagraphClass.Header &&
                        paraClass == GospelParagraphClass.SubHeader &&
                        paraClass == GospelParagraphClass.StudySummary)
                    {
                        continue;
                    }

                    previousPara = para;

                    // Break sentences within the paragraph (this is mainly to control ngram propagation so we don't have associations
                    // doing 9x permutations between every single word in the paragraph)
                    foreach (string sentence in EnglishWordFeatureExtractor.BreakSentence(para.Text))
                    {
                        foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(sentence))
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                                para.ParaEntityId,
                                ngram,
                                ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                        }
                    }

                    foreach (var footnote in para.References)
                    {
                        TrainingFeatureType featureType = TrainingFeatureType.ScriptureReference;
                        if (footnote.ScriptureRef != null && footnote.ScriptureRef.LowEmphasis)
                        {
                            featureType = TrainingFeatureType.ScriptureReferenceWithoutEmphasis;
                        }

                        trainingFeaturesOut.Add(new TrainingFeature(
                            para.ParaEntityId,
                            footnote.TargetNodeId,
                            featureType));

                        // Also parse the words actually tagged by the footnote (only applies for links directly embedded in text, which is rare)
                        if (footnote.ReferenceSpan.HasValue && footnote.ReferenceSpan.Value.Length > 0)
                        {
                            string footnoteText = para.Text.Substring(footnote.ReferenceSpan.Value.Start, footnote.ReferenceSpan.Value.Length);
                            foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(footnoteText))
                            {
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    ngram,
                                    footnote.TargetNodeId,
                                    TrainingFeatureType.WordDesignation));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static void ExtractSearchIndexFeatures(string htmlPage, Uri pageUrl, ILogger logger, Action<TrainingFeature> trainingFeaturesOut, EntityNameIndex nameIndex)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return;
                }

                // Extract ngrams from the talk title and associate it with the talk
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(parseResult.TalkTitle))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                nameIndex.Mapping[parseResult.SpeakerEntityId] = parseResult.SpeakerName;
                nameIndex.Mapping[parseResult.DocumentEntityId] = parseResult.TalkTitle;

                // Extract ngrams from the speaker's name and associate it with the speaker
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(parseResult.SpeakerName))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.SpeakerEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static ConferenceTalkDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return null;
                }

                ConferenceTalkDocument returnVal = new ConferenceTalkDocument()
                {
                    DocumentType = GospelDocumentType.GeneralConferenceTalk,
                    Conference = parseResult.Conference,
                    Speaker = parseResult.SpeakerName,
                    SpeakerEntityId = parseResult.SpeakerEntityId,
                    TalkId = parseResult.TalkId,
                    Title = parseResult.TalkTitle,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = parseResult.DocumentEntityId,
                    Kicker = parseResult.Kicker
                };

                foreach (var paragraph in parseResult.Headers)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = paragraph.ParaEntityId,
                        Class = StandardizeClass(paragraph.Class, paragraph.Text),
                        Text = paragraph.Text.Trim()
                    });
                }

                foreach (var paragraph in parseResult.Paragraphs)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = paragraph.ParaEntityId,
                        Class = StandardizeClass(paragraph.Class, paragraph.Text),
                        Text = paragraph.Text.Trim()
                    });
                }

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        private static readonly IReadOnlyDictionary<string, GospelParagraphClass> CLASS_MAPPING = new Dictionary<string, GospelParagraphClass>(StringComparer.OrdinalIgnoreCase)
        {
            {"title", GospelParagraphClass.Header },
            {"author-name", GospelParagraphClass.SubHeader },
            {"author-role", GospelParagraphClass.SubHeader },
            {"kicker", GospelParagraphClass.StudySummary },
            {"line", GospelParagraphClass.Poem },
            {"subheading", GospelParagraphClass.SubHeader },
        };

        private static GospelParagraphClass StandardizeClass(string rawClass, string debugText)
        {
            if (string.IsNullOrEmpty(rawClass))
            {
                return GospelParagraphClass.Default;
            }

            GospelParagraphClass returnVal;
            if (CLASS_MAPPING.TryGetValue(rawClass, out returnVal))
            {
                return returnVal;
            }

            Console.WriteLine("Found a new class " + rawClass + " " + debugText);
            return GospelParagraphClass.Default;
        }

        private static DocumentParseModel? ParseInternal(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return null;
                }

                htmlPage = WebUtility.HtmlDecode(htmlPage);
                htmlPage = LdsDotOrgCommonParsers.RemoveNbsp(htmlPage);
                int year = int.Parse(urlParse.Groups[1].Value);
                ConferencePhase phase = int.Parse(urlParse.Groups[2].Value) < 7 ? ConferencePhase.April : ConferencePhase.October;
                string talkId = urlParse.Groups[3].Value;
                string talkTitle, authorFullName;
                ParseTalkAndAuthorNames(htmlPage, logger, out talkTitle, out authorFullName);

                if (!IsValidConferenceTalk(talkTitle, authorFullName))
                {
                    return null;
                }

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(htmlPage);

                DocumentParseModel returnVal = new DocumentParseModel()
                {
                    Conference = new Conference(phase, year),
                    SpeakerName = authorFullName,
                    TalkId = talkId,
                    DocumentEntityId = FeatureToNodeMapping.ConferenceTalk(year, phase, talkId),
                    SpeakerEntityId = FeatureToNodeMapping.ConferenceSpeaker(authorFullName),
                    TalkTitle = talkTitle
                };

                XPathNodeIterator iter;
                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;

                // Parse footnotes
                Dictionary<string, ISet<KnowledgeGraphNodeId>> footnoteReferences = new Dictionary<string, ISet<KnowledgeGraphNodeId>>();

                HashSet<ScriptureReference> scriptureRefs = new HashSet<ScriptureReference>();
                navigator.MoveToRoot();
                iter = navigator.Select("//footer[@class=\"notes\"]//li[@id]");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string footnotePId = currentNav.GetAttribute("id", string.Empty);
                    //Console.WriteLine($"ID {footnotePId}");

                    string content = currentNav.CurrentNode.InnerHtml;
                    content = WebUtility.HtmlDecode(content);

                    // Extract all scripture references from links and text
                    scriptureRefs.Clear();
                    LdsDotOrgCommonParsers.ParseAllScriptureReferences(content, scriptureRefs, logger);
                    HashSet<KnowledgeGraphNodeId> refNodeIds = new HashSet<KnowledgeGraphNodeId>();
                    foreach (ScriptureReference scriptureRef in scriptureRefs)
                    {
                        KnowledgeGraphNodeId refId = scriptureRef.ToNodeId();
                        if (!refNodeIds.Contains(refId))
                        {
                            refNodeIds.Add(refId);
                        }
                    }

                    // Conference talks can also reference other talks
                    foreach (KnowledgeGraphNodeId confTalkReference in LdsDotOrgCommonParsers.ParseAllConferenceTalkLinks(content, logger))
                    {
                        if (!refNodeIds.Contains(confTalkReference))
                        {
                            refNodeIds.Add(confTalkReference);
                        }
                    }

                    footnoteReferences[footnotePId] = refNodeIds;
                    //foreach (var node in refNodeIds)
                    //{
                    //    Console.WriteLine(node);
                    //}
                }

                // Parse headers
                navigator.MoveToRoot();
                iter = navigator.Select("//*[@id=\"main\"]/div[@class=\"body\"]/header//p | //*[@id=\"main\"]/div[@class=\"body\"]/header//h1");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string paraIdRaw = currentNav.GetAttribute("id", string.Empty) ?? string.Empty;
                    string paraClassRaw = currentNav.GetAttribute("class", string.Empty) ?? string.Empty;
                    //Console.WriteLine($"ID {paraIdRaw} CLASS {paraClassRaw}");
                    //Console.WriteLine(currentNav.CurrentNode.InnerHtml.Trim());

                    string content = currentNav.CurrentNode.InnerHtml;
                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();

                    var parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(content, logger);
                    List<FootnoteReference> footnoteRefs = new List<FootnoteReference>();
                    foreach (var inlineRef in parsedHtml.Links)
                    {
                        foreach (ScriptureReference scriptureRef in LdsDotOrgCommonParsers.ParseAllScriptureReferences(inlineRef.Item2, logger))
                        {
                            //Console.WriteLine($"Links to {inlineRef.Item2}");
                            footnoteRefs.Add(new FootnoteReference()
                            {
                                TargetNodeId = scriptureRef.ToNodeId(),
                                ScriptureRef = scriptureRef,
                                ReferenceSpan = inlineRef.Item1
                            });
                        }
                    }

                    Paragraph para = new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, paraIdRaw),
                        Class = paraClassRaw,
                        Id = paraIdRaw,
                        Text = parsedHtml.TextWithInlineFormatTags,
                        References = footnoteRefs,
                    };

                    if (string.Equals("h1", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        para.Text = para.Text.Trim();
                        para.Class = "title";
                        returnVal.Headers.Add(para);
                    }
                    else if (string.Equals(paraClassRaw, "author-name", StringComparison.OrdinalIgnoreCase))
                    {
                        para.Text = para.Text.Trim();
                        returnVal.Headers.Add(para);
                    }
                    else if (string.Equals(paraClassRaw, "author-role", StringComparison.OrdinalIgnoreCase))
                    {
                        para.Text = $"<i>{para.Text.Trim()}</i>";
                        returnVal.Headers.Add(para);
                    }
                    else if (string.Equals(paraClassRaw, "kicker", StringComparison.OrdinalIgnoreCase))
                    {
                        returnVal.Kicker = para.Text.Trim();
                        returnVal.Headers.Add(para);
                    }
                    else
                    {
                        //Console.WriteLine(paraClassRaw);
                        //Console.WriteLine(para.Text);
                    }
                }

                // Parse paragraphs
                int subheaderNum = 0;
                navigator.MoveToRoot();
                iter = navigator.Select("//*[@id=\"main\"]/div[@class=\"body\"]/div[@class=\"body-block\"]//p[@id] | //*[@id=\"main\"]/div[@class=\"body\"]/div[@class=\"body-block\"]//h2");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string paraIdRaw = currentNav.GetAttribute("id", string.Empty) ?? string.Empty;
                    string paraClassRaw = currentNav.GetAttribute("class", string.Empty) ?? string.Empty;
                    //Console.WriteLine($"ID {paraIdRaw} CLASS {paraClassRaw}");
                    //Console.WriteLine(currentNav.CurrentNode.InnerHtml.Trim());

                    string content = currentNav.CurrentNode.InnerHtml;
                    if (content.Contains("<cite>"))
                    {
                        continue;
                    }

                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();
                    var parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(content, logger);
                    List<FootnoteReference> footnoteRefs = new List<FootnoteReference>();
                    foreach (var inlineRef in parsedHtml.Links)
                    {
                        Uri uri = new Uri("scripture://" + inlineRef.Item2, UriKind.Absolute);
                        string footnote = uri.Fragment.TrimStart('#');
                        ISet<KnowledgeGraphNodeId>? footnoteTargets;
                        if (footnote.StartsWith("note"))
                        {
                            if (footnoteReferences.TryGetValue(footnote, out footnoteTargets))
                            {
                                //Console.WriteLine($"Has footnote {footnote}");
                                foreach (KnowledgeGraphNodeId nodeId in footnoteTargets)
                                {
                                    footnoteRefs.Add(new FootnoteReference()
                                    {
                                        TargetNodeId = nodeId,
                                        ScriptureRef = null,
                                        ReferenceSpan = inlineRef.Item1
                                    });
                                }
                            }
                            else
                            {
                                logger.Log($"Verse {paraIdRaw} has reference to unknown footnote {footnote}, ignoring...", LogLevel.Wrn);
                            }
                        }
                        else
                        {
                            // This could be an href that links directly to another scripture verse
                            // example D&C 76:15
                            foreach (ScriptureReference scriptureRef in LdsDotOrgCommonParsers.ParseAllScriptureReferences(inlineRef.Item2, logger))
                            {
                                //Console.WriteLine($"Links to {scriptureRef}");
                                footnoteRefs.Add(new FootnoteReference()
                                {
                                    TargetNodeId = scriptureRef.ToNodeId(),
                                    ScriptureRef = scriptureRef,
                                    ReferenceSpan = inlineRef.Item1
                                });
                            }
                        }
                    }

                    if (string.Equals("h2", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        Paragraph para = new Paragraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, $"section{subheaderNum++}"),
                            Class = "subheading",
                            Id = paraIdRaw,
                            Text = parsedHtml.TextWithInlineFormatTags,
                            References = footnoteRefs,
                        };

                        returnVal.Paragraphs.Add(para);
                    }
                    else
                    {
                        //Console.WriteLine(parsedHtml.TextWithInlineFormatTags);
                        Paragraph para = new Paragraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, paraIdRaw),
                            Class = paraClassRaw,
                            Id = paraIdRaw,
                            Text = parsedHtml.TextWithInlineFormatTags,
                            References = footnoteRefs,
                        };

                        returnVal.Paragraphs.Add(para);
                    }
                }

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        private static void ExtractFeaturesFromSingleParagraph(
            Paragraph currentParagraph,
            string book,
            int chapter,
            int? verse,
            List<TrainingFeature> trainingFeaturesOut)
        {
            // Common word and ngram level features associated with this verse entity
            EnglishWordFeatureExtractor.ExtractTrainingFeatures(currentParagraph.Text.Trim(), trainingFeaturesOut, currentParagraph.ParaEntityId);

            // Is this paragraph an actual numerical verse?
            if (verse.HasValue)
            {
                // Relationship between this verse and the previous one (if present)
                if (verse.Value > 1)
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        currentParagraph.ParaEntityId,
                        FeatureToNodeMapping.ScriptureVerse(
                            book,
                            chapter,
                            verse.Value - 1),
                        TrainingFeatureType.ParagraphAssociation));
                }
            }

            // Relationship between this verse and the book it's in
            trainingFeaturesOut.Add(new TrainingFeature(
                currentParagraph.ParaEntityId,
                FeatureToNodeMapping.ScriptureChapter(
                    book,
                    chapter),
                TrainingFeatureType.BookAssociation));

            // Cross-references between this verse and other verses based on footnotes
            foreach (var footnote in currentParagraph.References)
            {
                TrainingFeatureType featureType = TrainingFeatureType.ScriptureReference;
                if (footnote.ScriptureRef != null && footnote.ScriptureRef.LowEmphasis)
                {
                    featureType = TrainingFeatureType.ScriptureReferenceWithoutEmphasis;
                }

                trainingFeaturesOut.Add(new TrainingFeature(
                    currentParagraph.ParaEntityId,
                    footnote.TargetNodeId,
                    featureType));

                // Also parse the words actually tagged by the footnote - this is why we had to do very careful
                // start-end index calculations earlier
                if (footnote.ReferenceSpan.HasValue)
                {
                    string footnoteText = currentParagraph.Text.Substring(footnote.ReferenceSpan.Value.Start, footnote.ReferenceSpan.Value.Length);
                    foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(footnoteText))
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            ngram,
                            footnote.TargetNodeId,
                            TrainingFeatureType.WordDesignation));
                    }
                }
            }
        }

        private static readonly Regex PrintableTitleParser = new Regex("<h1.*?>(.+?)<\\/h1>");

        // <p[^>]+class=\"author-name\"[^>]+?>\s*(?:<span[\w\W]+?<\/span>)?(?:By |From )?(?:Elder |Sister |President |Bishop |Brother )?(.+?)<\/p>
        private static readonly Regex AuthorNameParser = new Regex("<p[^>]+class=\\\"author-name\\\"[^>]+?>\\s*(.+?)<\\/p>");

        private static readonly Regex AuthorTitleRemover = new Regex("(?:By |From )?(?:Elder |Sister |President |Bishop |Brother )?");

        private static readonly Regex ParenthesesRemover = new Regex("\\s+\\(.+?\\)");

        private static void ParseTalkAndAuthorNames(string htmlPage, ILogger logger, out string talkTitle, out string authorFullName)
        {
            talkTitle = StringUtils.RegexRip(PrintableTitleParser, htmlPage, 1, logger);
            talkTitle = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, talkTitle);
            // Some rare talks have a scripture citation in the title, to handle that remove anything within parentheses
            // https://www.churchofjesuschrist.org/study/general-conference/1999/04/repent-of-our-selfishness-d-c-56-8?lang=eng
            talkTitle = StringUtils.RegexRemove(ParenthesesRemover, talkTitle);
            talkTitle = talkTitle.Trim();
            authorFullName = StringUtils.RegexRip(AuthorNameParser, htmlPage, 1, logger);
            authorFullName = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, authorFullName);
            authorFullName = StringUtils.RegexRemove(AuthorTitleRemover, authorFullName);
            authorFullName = authorFullName.Trim();
        }

        private static bool IsValidConferenceTalk(string talkTitle, string authorFullName)
        {
            if ((talkTitle.Contains("Audit") ||
                    talkTitle.Contains("Finance")) &&
                    talkTitle.Contains("Report"))
            {
                // Ignore auditing department reports
                return false;
            }

            if (talkTitle.Contains("Statistic"))
            {
                // Ignore church statistical reports
                return false;
            }

            if (talkTitle.Contains("Sustain") && talkTitle.Contains("Officers"))
            {
                // Ignore sustainings of church officers
                return false;
            }

            // This catches general assemblies, solemn assemblies, and other special occasionas
            if (authorFullName.Contains("Presented by"))
            {
                return false;
            }

            // Ignore entire sessions, meetings, firesides, video presentations, and proclamations
            if (string.IsNullOrWhiteSpace(authorFullName))
            {
                return false;
            }

            return true;
        }

        private class DocumentParseModel
        {
            public required KnowledgeGraphNodeId DocumentEntityId;
            public required string TalkId;
            public required string SpeakerName;
            public required KnowledgeGraphNodeId SpeakerEntityId;
            public required string TalkTitle;
            public required Conference Conference;
            public string? Kicker;
            public readonly List<Paragraph> Headers = new List<Paragraph>();
            public readonly List<Paragraph> Paragraphs = new List<Paragraph>();
        }

        private class Paragraph
        {
            public required KnowledgeGraphNodeId ParaEntityId;
            public required string Id;
            public required string Class;
            public required string Text;
            public required List<FootnoteReference> References;

            public override string ToString()
            {
                return Text;
            }
        }

        private class FootnoteReference
        {
            public required KnowledgeGraphNodeId TargetNodeId;
            public ScriptureReference? ScriptureRef;
            public IntRange? ReferenceSpan;
        }
    }
}
