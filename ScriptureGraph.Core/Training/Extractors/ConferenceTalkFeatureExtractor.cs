using Durandal.API;
using Durandal.Common.Collections;
using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    // TODO look at talks like https://www.churchofjesuschrist.org/study/general-conference/2023/10/26choi?lang=eng
    // which refer in footnotes to a lot of other talks. We should be able to parse these
    // <a class="" href="/study/general-conference/2002/10/models-to-follow?lang=eng&amp;id=p17#p17" data-scroll-id="note4">Models to Follow</a>
    public class ConferenceTalkFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/general-conference\\/(\\d+)\\/(\\d+)\\/(.+?)(?:\\?|$)");

        // <p[^>]+?(?:class=\"(.+?)\"[^>]+?)?id=\"p.+?\".*?>([\w\W]+?)<\/p>
        // group 1: paragraph role, e.g. "subtitle", "kicker", or empty for body paragraphs
        // group 2: paragraph content
        private static readonly Regex ParagraphParser = new Regex("<p[^>]+?(?:class=\\\"(.+?)\\\"[^>]+?)?id=\\\"p.+?\\\".*?>([\\w\\W]+?)<\\/p>");

        // <footer class=\"notes\">
        private static readonly Regex BeginningOfFootnotesMatcher = new Regex("<footer class=\"notes\">");

        // \s*<a[^>]+?class=\"note-ref\"[^>]+?data-scroll-id=\"(.+?)\"><sup class=\"marker\" data-value=\".+?\"><\/sup><\/a>
        private static readonly Regex FootnoteParser = new Regex("\\s*<a[^>]+?class=\"note-ref\"[^>]+?data-scroll-id=\"(.+?)\"><sup class=\"marker\" data-value=\".+?\"><\\/sup><\\/a>");

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return;
                }

                htmlPage = WebUtility.HtmlDecode(htmlPage);
                htmlPage = LdsDotOrgCommonParsers.RemoveNbsp(htmlPage);
                int year = int.Parse(urlParse.Groups[1].Value);
                ConferencePhase phase = int.Parse(urlParse.Groups[2].Value) < 7 ? ConferencePhase.April : ConferencePhase.October;
                string talkId = urlParse.Groups[3].Value;
                string talkTitle, authorFullName;
                ParseTalkAndAuthorNames(htmlPage, logger, out talkTitle, out authorFullName);

                // todo Check which talk has the author of "The First Presidency and Council of the Twelve Apostles of The Church of Jesus Christ of Latter-day Saints"

                if (!IsValidConferenceTalk(talkTitle, authorFullName))
                {
                    return;
                }

                KnowledgeGraphNodeId entireTalkNode = FeatureToNodeMapping.ConferenceTalk(year, phase, talkId);
                Dictionary<string, StructuredFootnote> footnotes = LdsDotOrgCommonParsers.ParseFootnotesFromPage(htmlPage, logger);

                // High-level features
                // Title of the talk -> Talk
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(talkTitle))
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        entireTalkNode,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                // Talk -> Speaker
                trainingFeaturesOut.Add(new TrainingFeature(
                    entireTalkNode,
                    FeatureToNodeMapping.ConferenceSpeaker(authorFullName),
                    TrainingFeatureType.EntityReference));

                Match startOfFootnotes = BeginningOfFootnotesMatcher.Match(htmlPage);
                List<ScriptureReference> scriptureReferences = new List<ScriptureReference>();
                // Break paragraphs
                int paragraph = 0;
                foreach (Match entryMatch in ParagraphParser.Matches(htmlPage))
                {
                    if (entryMatch.Groups[1].Success ||
                        (startOfFootnotes.Success && entryMatch.Index > startOfFootnotes.Index))
                    {
                        // It's some kind of special paragraph like a subtitle; don't count it
                        // Or else it's down in the footnotes region
                        continue;
                    }

                    paragraph++;
                    KnowledgeGraphNodeId thisParagraphNode = FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, paragraph);

                    // Associate this paragraph with the entire talk
                    trainingFeaturesOut.Add(new TrainingFeature(
                        thisParagraphNode,
                        entireTalkNode,
                        TrainingFeatureType.BookAssociation));

                    // And with the previous paragraph
                    if (paragraph > 1)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            thisParagraphNode,
                            FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, paragraph - 1),
                            TrainingFeatureType.ParagraphAssociation));
                    }

                    string rawParagraph = LdsDotOrgCommonParsers.RemovePageBreakTags(entryMatch.Groups[2].Value);

                    // Break sentences within the paragraph (this is mainly to control ngram propagation so we don't have associations
                    // doing 9x permutations between every single word in the paragraph)
                    List<string> sentences = EnglishWordFeatureExtractor.BreakSentence(rawParagraph).ToList();

                    // We need to do some postprocessing of each sentence.
                    // Step 1. Sentences under 50 words get prepended to the next line (or appended to the previous line if it's the last sentence).
                    for (int sentenceIdx = 0; sentenceIdx < sentences.Count; sentenceIdx++)
                    {
                        string thisSentence = sentences[sentenceIdx];
                        sentences[sentenceIdx] = thisSentence;

                        if (thisSentence.Length < 50)
                        {
                            if (sentenceIdx > 0 && sentenceIdx == sentences.Count - 1)
                            {
                                // Append to previous line
                                sentences[sentenceIdx - 1] = sentences[sentenceIdx - 1] + " " + thisSentence;
                                sentences[sentenceIdx] = string.Empty;
                            }
                            else if (sentenceIdx < sentences.Count - 1)
                            {
                                // Prepend to next line
                                sentences[sentenceIdx + 1] = thisSentence + sentences[sentenceIdx + 1];
                                sentences[sentenceIdx] = string.Empty;
                            }
                        }
                    }

                    RemoveEmptyEntries(sentences);

                    // Step 2. If a sentence begins with a citation, append that citation to the previous line (so it annotates the correct sentence)
                    for (int sentenceIdx = 0; sentenceIdx < sentences.Count; sentenceIdx++)
                    {
                        Match footnoteMatch = FootnoteParser.Match(sentences[sentenceIdx]);
                        if (sentenceIdx > 0 && footnoteMatch.Success && footnoteMatch.Index == 0)
                        {
                            sentences[sentenceIdx - 1] = sentences[sentenceIdx - 1] + footnoteMatch.Value;
                            sentences[sentenceIdx] = sentences[sentenceIdx].Substring(footnoteMatch.Length);
                        }
                    }

                    RemoveEmptyEntries(sentences);

                    foreach (string sentence in sentences)
                    {
                        string thisSentenceWordBreakerText = StringUtils.RegexRemove(FootnoteParser, sentence);
                        thisSentenceWordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, thisSentenceWordBreakerText);

                        // Common word and ngram level features associated with this paragraph entity
                        EnglishWordFeatureExtractor.ExtractTrainingFeatures(thisSentenceWordBreakerText, trainingFeaturesOut, thisParagraphNode);

                        LdsDotOrgCommonParsers.ParseAllScriptureReferences(sentence, scriptureReferences, logger);

                        // Cross-references between this verse and other verses based on inline scripture links (used on legacy pages before footnotes)
                        foreach (ScriptureReference scriptureRef in scriptureReferences)
                        {
                            KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);
                            // Entity reference between this talk paragraph and the scripture ref
                            trainingFeaturesOut.Add(new TrainingFeature(
                                thisParagraphNode,
                                refNodeId,
                                TrainingFeatureType.EntityReference));

                            // And association between all words spoken in this sentence and the scripture ref
                            foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(thisSentenceWordBreakerText))
                            {
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    ngram,
                                    refNodeId,
                                    TrainingFeatureType.WordAssociation));
                            }
                        }

                        scriptureReferences.Clear();

                        // Cross-references between this verse and other verses based on footnotes
                        foreach (Match footnoteMatch in FootnoteParser.Matches(sentence))
                        {
                            string footnoteId = footnoteMatch.Groups[1].Value;
                            StructuredFootnote? footnote;
                            if (!footnotes.TryGetValue(footnoteId, out footnote))
                            {
                                logger.Log("Couldn't resolve footnote ref " + footnoteId, LogLevel.Wrn);
                                continue;
                            }
                            
                            foreach (var scriptureRef in footnote.ScriptureReferences)
                            {
                                KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);
                                // Entity reference between this talk paragraph and the scripture ref
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    thisParagraphNode,
                                    refNodeId,
                                    TrainingFeatureType.EntityReference));

                                // And association between all words spoken in this sentence and the scripture ref
                                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(thisSentenceWordBreakerText))
                                {
                                    trainingFeaturesOut.Add(new TrainingFeature(
                                        ngram,
                                        refNodeId,
                                        TrainingFeatureType.WordAssociation));
                                }
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

        public static void ExtractSearchIndexFeatures(string htmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> trainingFeaturesOut, EntityNameIndex nameIndex)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return;
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
                    return;
                }

                KnowledgeGraphNodeId entireTalkNode = FeatureToNodeMapping.ConferenceTalk(year, phase, talkId);

                // Extract ngrams from the talk title and associate it with the talk
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(talkTitle))
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        entireTalkNode,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                KnowledgeGraphNodeId speakerNode = FeatureToNodeMapping.ConferenceSpeaker(authorFullName);
                nameIndex.Mapping[speakerNode] = authorFullName;
                nameIndex.Mapping[entireTalkNode] = talkTitle;

                // Extract ngrams from the speaker's name and associate it with the speaker
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(authorFullName))
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        speakerNode,
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
                Conference conferenceInfo = new Conference(phase, year);
                string talkId = urlParse.Groups[3].Value;
                string talkTitle, authorFullName;
                ParseTalkAndAuthorNames(htmlPage, logger, out talkTitle, out authorFullName);

                if (!IsValidConferenceTalk(talkTitle, authorFullName))
                {
                    return null;
                }

                KnowledgeGraphNodeId entireTalkNode = FeatureToNodeMapping.ConferenceTalk(year, phase, talkId);

                ConferenceTalkDocument returnVal = new ConferenceTalkDocument()
                {
                    Title = talkTitle,
                    TalkId = talkId,
                    Conference = conferenceInfo,
                    DocumentEntityId = entireTalkNode,
                    DocumentType = GospelDocumentType.GeneralConferenceTalk,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    Speaker = authorFullName,
                    SpeakerEntityId = FeatureToNodeMapping.ConferenceSpeaker(authorFullName)
                };

                string sanitizedHtml = LdsDotOrgCommonParsers.RemovePageBreakTags(htmlPage);
                sanitizedHtml = StringUtils.RegexRemove(FootnoteParser, sanitizedHtml);
                Match startOfFootnotes = BeginningOfFootnotesMatcher.Match(sanitizedHtml);

                int paragraph = 0;
                foreach (Match entryMatch in ParagraphParser.Matches(sanitizedHtml))
                {
                    if (entryMatch.Groups[1].Success ||
                         (startOfFootnotes.Success && entryMatch.Index > startOfFootnotes.Index))
                    {
                        // It's some kind of special paragraph like a subtitle; don't count it
                        // Or else it's down in the footnotes region
                        continue;
                    }

                    paragraph++;
                    KnowledgeGraphNodeId thisParagraphNode = FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, paragraph);
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, paragraph),
                        Text = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, entryMatch.Groups[2].Value)
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

        private static void RemoveEmptyEntries(List<string> list)
        {
            for (int sentenceIdx = list.Count - 1; sentenceIdx >= 0; sentenceIdx--)
            {
                if (string.IsNullOrWhiteSpace(list[sentenceIdx]))
                {
                    list.RemoveAt(sentenceIdx);
                }
            }
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
    }
}
