using Durandal.Common.Logger;
using Durandal.Common.MathExt;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace ScriptureGraph.Core.Training.Extractors
{
    // A good omniparser test case (check that it captures the conference talk reference too):
    // https://speeches.byu.edu/talks/keith-b-mcmullin/make-god-and-his-kingdom-center-life/

    public class ByuSpeechFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/talks\\/(.+?)\\/(.+?)(?:\\/|$)");

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, Action<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return;
                }

                if (EvaluateScripturalScore(parseResult, logger) < 1.0f)
                {
                    return;
                }

                // High-level features
                // Title of the talk -> Talk
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(parseResult.TalkTitle))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                List<TrainingFeature> scratch = new List<TrainingFeature>();
                Paragraph? previousPara = null;
                foreach (Paragraph para in parseResult.Paragraphs)
                {
                    // Associate this paragraph with the entire talk
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        para.ParaEntityId,
                        TrainingFeatureType.BookAssociation));

                    // And with the previous paragraph
                    if (previousPara != null)
                    {
                        trainingFeaturesOut(new TrainingFeature(
                            para.ParaEntityId,
                            previousPara.ParaEntityId,
                            TrainingFeatureType.ParagraphAssociation));
                    }

                    previousPara = para;

                    foreach (KnowledgeGraphNodeId reference in para.References)
                    {
                        trainingFeaturesOut(new TrainingFeature(
                            para.ParaEntityId,
                            reference,
                            TrainingFeatureType.EntityReference));
                    }

                    // Break sentences within the paragraph (this is mainly to control ngram propagation so we don't have associations
                    // doing 9x permutations between every single word in the paragraph)
                    List<string> sentences = EnglishWordFeatureExtractor.BreakSentence(para.Text).ToList();

                    foreach (string sentence in sentences)
                    {
                        string thisSentenceWordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, sentence);

                        // Common word and ngram level features associated with this paragraph entity
                        scratch.Clear();
                        EnglishWordFeatureExtractor.ExtractTrainingFeatures(thisSentenceWordBreakerText, scratch, para.ParaEntityId);

                        foreach (var f in scratch)
                        {
                            trainingFeaturesOut(f);
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

                if (EvaluateScripturalScore(parseResult, logger) < 1.0f)
                {
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

        public static ByuSpeechDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return null;
                }

                // Evaluate how "scriptural" this talk is
                if (EvaluateScripturalScore(parseResult, logger) < 1.0f)
                {
                    return null;
                }

                //float score = EvaluateScripturalScore(parseResult, logger);
                //logger.Log($"Score is {score:F3} for {parseResult.TalkTitle}");

                ByuSpeechDocument returnVal = new ByuSpeechDocument()
                {
                    DocumentType = GospelDocumentType.ByuSpeech,
                    Speaker = parseResult.SpeakerName,
                    SpeakerEntityId = parseResult.SpeakerEntityId,
                    Title = parseResult.TalkTitle,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = parseResult.DocumentEntityId,
                    Kicker = parseResult.Kicker,
                    TalkId = parseResult.TalkId,
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

        private static float EvaluateScripturalScore(DocumentParseModel parseResult, ILogger logger)
        {
            float uniqueScriptureRefs = 0;
            float words = 0;

            HashSet<KnowledgeGraphNodeId> chapterNodes = new HashSet<KnowledgeGraphNodeId>();
            foreach (Paragraph para in parseResult.Paragraphs)
            {
                chapterNodes.Clear();
                words += EnglishWordFeatureExtractor.BreakWords(para.Text).Count();

                foreach (var footnote in para.References)
                {
                    // this is intended to even out the weight of very broad references to the same chapter within one paragraph
                    if (footnote.Type == KnowledgeGraphNodeType.ScriptureVerse)
                    {
                        ScriptureReference parsedRef = new ScriptureReference(footnote);
                        var chapterNode = FeatureToNodeMapping.ScriptureChapter(parsedRef.Book, parsedRef.Chapter!.Value);
                        if (!chapterNodes.Contains(chapterNode))
                        {
                            chapterNodes.Add(chapterNode);
                            uniqueScriptureRefs += 1.0f;
                        }
                    }
                }
            }

            return (uniqueScriptureRefs * 600) / words;
        }

        private static GospelParagraphClass StandardizeClass(string rawClass, string debugText)
        {
            if (string.IsNullOrEmpty(rawClass) || string.Equals("body", rawClass, StringComparison.OrdinalIgnoreCase))
            {
                return GospelParagraphClass.Default;
            }

            if (rawClass.Contains("single-speech__title")) return GospelParagraphClass.Header;
            if (rawClass.Contains("single-speech__speaker")) return GospelParagraphClass.SubHeader;
            if (rawClass.Contains("single-speech__speaker-subtext")) return GospelParagraphClass.SubHeader;
            if (rawClass.Contains("single-speech__speaker-position")) return GospelParagraphClass.SubHeader;
            if (rawClass.Contains("single-speech__callout")) return GospelParagraphClass.StudySummary;
            if (rawClass.Contains("single-speech__date")) return GospelParagraphClass.SubHeader;
            if (rawClass.Contains("subheading")) return GospelParagraphClass.SubHeader;

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
                string speakerUrlId = urlParse.Groups[1].Value;
                string talkUrlId = urlParse.Groups[2].Value;
                string talkId = $"{speakerUrlId}|{talkUrlId}";

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(htmlPage);

                string talkTitlePretty = GetSingleInnerHtml(html, "//h1[@class=\"single-speech__title\"]");
                talkTitlePretty = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, talkTitlePretty);
                talkTitlePretty = talkTitlePretty.Trim();
                string speakerNamePretty = GetSingleInnerHtml(html, "//h2[@class=\"single-speech__speaker\"]");
                speakerNamePretty = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, speakerNamePretty);
                speakerNamePretty = speakerNamePretty.Trim();
                string forum = GetSingleInnerHtml(html, "//a[@class=\"single-speech__type\"]");
                forum = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, forum);
                forum = forum.Trim();

                if (!string.Equals("Devotional", forum, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                XPathNodeIterator iter;
                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;

                DocumentParseModel returnVal = new DocumentParseModel()
                {
                    SpeakerName = speakerNamePretty,
                    DocumentEntityId = FeatureToNodeMapping.ByuSpeech(speakerUrlId, talkUrlId),
                    SpeakerEntityId = FeatureToNodeMapping.ConferenceSpeaker(speakerNamePretty),
                    TalkId = talkId,
                    TalkTitle = talkTitlePretty,
                    Forum = forum,
                };

                returnVal.Headers.Add(new Paragraph()
                {
                    ParaEntityId = FeatureToNodeMapping.ByuSpeechParagraph(speakerUrlId, talkUrlId, "title"),
                    Class = "single-speech__title",
                    Text = talkTitlePretty,
                });

                returnVal.Headers.Add(new Paragraph()
                {
                    ParaEntityId = FeatureToNodeMapping.ByuSpeechParagraph(speakerUrlId, talkUrlId, "speaker"),
                    Class = "single-speech__speaker",
                    Text = speakerNamePretty,
                });

                // Parse headers
                navigator.MoveToRoot();
                iter = navigator.Select("//div[@class=\"single-speech__subtext-wrapper\"]/p");
                int headerId = 0;
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string paraClassRaw = currentNav.GetAttribute("class", string.Empty) ?? string.Empty;
                    //Console.WriteLine($"CLASS {paraClassRaw}");
                    //Console.WriteLine(currentNav.CurrentNode.InnerHtml.Trim());

                    string content = currentNav.CurrentNode.InnerHtml;
                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();

                    Paragraph para = new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.ByuSpeechParagraph(speakerUrlId, talkUrlId, $"header{headerId++}"),
                        Class = paraClassRaw,
                        Text = content,
                    };

                    if (paraClassRaw.Contains("single-speech__speaker-position", StringComparison.OrdinalIgnoreCase))
                    {
                        para.Text = $"<i>{para.Text.Trim()}</i>";
                        para.Class = "single-speech__speaker-position";
                        returnVal.Headers.Add(para);
                    }
                    else if (paraClassRaw.Contains("single-speech__date", StringComparison.OrdinalIgnoreCase))
                    {
                        DateOnly speechDate;
                        if (DateOnly.TryParse(para.Text.Trim(), CultureInfo.GetCultureInfo(1033), out speechDate))
                        {
                            returnVal.SpeechDate = speechDate;
                        }

                        para.Text = $"<i>{para.Text.Trim()}</i>";
                        para.Class = "single-speech__date";
                        returnVal.Headers.Add(para);
                    }
                    else
                    {
                        Console.WriteLine("Unknown header class");
                        Console.WriteLine(paraClassRaw);
                        Console.WriteLine(para.Text);
                    }
                }

                string kicker = GetSingleInnerHtml(html, "//div[@class=\"single-speech__callout\"]/p");
                kicker = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, kicker);
                kicker = kicker.Trim();
                returnVal.Kicker = kicker;

                if (!string.IsNullOrEmpty(kicker))
                {
                    returnVal.Headers.Add(new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.ByuSpeechParagraph(speakerUrlId, talkUrlId, "kicker"),
                        Class = "single-speech__callout",
                        Text = kicker,
                    });
                }

                // Parse paragraphs
                int subheaderNum = 1;
                int paraNum = 1;
                navigator.MoveToRoot();
                iter = navigator.Select("//div[@class=\"single-speech__content\"]/p | //div[@class=\"single-speech__content\"]/h2");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string content = currentNav.CurrentNode.InnerHtml;

                    if (content.Contains('©'))
                    {
                        continue;
                    }

                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();
                    var parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(content, logger);

                    if (string.Equals("h2", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        //Console.WriteLine($"HEADING");
                        //Console.WriteLine(parsedHtml.TextWithInlineFormatTags);
                        Paragraph para = new Paragraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.ByuSpeechParagraph(speakerUrlId, talkUrlId, $"section{subheaderNum++}"),
                            Class = "subheading",
                            Text = parsedHtml.TextWithInlineFormatTags,
                        };

                        returnVal.Paragraphs.Add(para);
                    }
                    else
                    {
                        //Console.WriteLine($"PARAGRAPH");
                        //Console.WriteLine(parsedHtml.TextWithInlineFormatTags);
                        Paragraph para = new Paragraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.ByuSpeechParagraph(speakerUrlId, talkUrlId, paraNum++),
                            Class = "body",
                            Text = parsedHtml.TextWithInlineFormatTags,
                        };

                        foreach (ScriptureReference scriptureRef in LdsDotOrgCommonParsers.ParseAllScriptureReferences(content, logger))
                        {
                            //Console.WriteLine($"Links to {scriptureRef}");
                            para.References.Add(scriptureRef.ToNodeId());
                        }

                        returnVal.Paragraphs.Add(para);
                    }
                }

                // Was this speech one where the text is actually unavailable?
                // If so, break out
                if (returnVal.Paragraphs.Count < 3)
                {
                    return null;
                }

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        private static string GetSingleInnerHtml(HtmlDocument html, string xpath)
        {
            HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;
            XPathNodeIterator iter = navigator.Select(xpath);
            if (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
            {
                return currentNav.CurrentNode.InnerHtml;
            }

            return string.Empty;
        }

        private class DocumentParseModel
        {
            public required KnowledgeGraphNodeId DocumentEntityId;
            public required string TalkId;
            public required string SpeakerName;
            public required KnowledgeGraphNodeId SpeakerEntityId;
            public required string TalkTitle;
            public string? Kicker;
            public required string Forum;
            public readonly List<Paragraph> Headers = new List<Paragraph>();
            public readonly List<Paragraph> Paragraphs = new List<Paragraph>();
            public DateOnly? SpeechDate;
        }

        private class Paragraph
        {
            public required KnowledgeGraphNodeId ParaEntityId;
            public required string Class;
            public required string Text;
            public readonly List<KnowledgeGraphNodeId> References = new List<KnowledgeGraphNodeId>();

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
