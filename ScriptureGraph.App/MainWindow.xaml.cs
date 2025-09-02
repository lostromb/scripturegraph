using Durandal.Common.File;
using Durandal.Common.Instrumentation;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Tasks;
using Durandal.Common.Time;
using Durandal.Common.Utils;
using Durandal.Common.Utils.NativePlatform;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using static Durandal.Common.Audio.WebRtc.RingBuffer;

namespace ScriptureGraph.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            BuildSearchIndex().Await();
        }


        private static async Task BuildSearchIndex()
        {
            ILogger logger = new ConsoleLogger("Main", LogLevel.All);
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            IFileSystem webCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"D:\Code\scripturegraph\runtime\cache");
            IFileSystem documentCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"D:\Code\scripturegraph\runtime\documents");
            WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
            WebCrawler crawler = new WebCrawler(new PortableHttpClientFactory(), pageCache);
            KnowledgeGraph graph = new KnowledgeGraph();

            string modelFileName = @"D:\Code\scripturegraph\runtime\searchindex.graph";

            HashSet<Regex> scriptureRegexes = new HashSet<Regex>();

            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/general-conference/.+?\\?lang=eng$"));
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?\\?lang=eng$"));
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/general-conference\\?lang=eng$")); // overall conference index
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+\\?lang=eng$")); // decade index pages
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+/\\d+\\?lang=eng$")); // conference index page
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+/\\d+/.+?\\?lang=eng$")); // specific talks

            DocumentProcessorForSearchIndex processor = new DocumentProcessorForSearchIndex();

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/general-conference/2024/04/15dushku?lang=eng"),
                processor.ProcessFromWebCrawler,
                logger.Clone("WebCrawler"),
                scriptureRegexes);

            using (FileStream testGraphOut = new FileStream(modelFileName, FileMode.Create, FileAccess.Write))
            {
                graph.Save(testGraphOut);
            }
        }

        private class DocumentProcessorForSearchIndex
        {
            private static readonly Regex ScriptureChapterUrlMatcher = new Regex("\\/study\\/scriptures\\/(?:bofm|ot|nt|dc-testament|pgp)\\/.+?\\/\\d+");
            private static readonly Regex ReferenceUrlMatcher = new Regex("\\/study\\/scriptures\\/(tg|bd|gs|triple-index)\\/.+?(?:\\?|$)");
            private static readonly Regex ConferenceTalkUrlMatcher = new Regex("\\/study\\/general-conference\\/\\d+\\/\\d+\\/.+?(?:\\?|$)");

            public Task<bool> ProcessFromWebCrawler(WebCrawler.CrawledPage page, ILogger logger)
            {
                VirtualPath fileDestination = VirtualPath.Root;
                GospelDocument? parsedDoc = null;
                Match match = ScriptureChapterUrlMatcher.Match(page.Url.AbsolutePath);
                if (match.Success)
                {
                    logger.Log($"Parsing scripture page {page.Url.AbsolutePath}");
                    ScriptureChapterDocument? structuredDoc = ScripturePageFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                    parsedDoc = structuredDoc;
                    if (structuredDoc == null)
                    {
                        logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                    }
                    else
                    {
                        fileDestination = new VirtualPath($"{structuredDoc.Canon}\\{structuredDoc.Book}-{structuredDoc.Chapter}.json");
                    }
                }
                else
                {
                    match = ReferenceUrlMatcher.Match(page.Url.AbsolutePath);
                    if (string.Equals(match.Groups[1].Value, "bd", StringComparison.Ordinal))
                    {
                        logger.Log($"Parsing BD page {page.Url.AbsolutePath}");
                        BibleDictionaryDocument? structuredDoc = BibleDictionaryFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                        parsedDoc = structuredDoc;
                        if (structuredDoc == null)
                        {
                            logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                        }
                        else
                        {
                            fileDestination = new VirtualPath($"bd\\{structuredDoc.TopicId}.json");
                        }
                    }
                    else if (string.Equals(match.Groups[1].Value, "tg", StringComparison.Ordinal) ||
                        string.Equals(match.Groups[1].Value, "gs", StringComparison.Ordinal) ||
                        string.Equals(match.Groups[1].Value, "triple-index", StringComparison.Ordinal))
                    {
                    }
                    else
                    {
                        match = ConferenceTalkUrlMatcher.Match(page.Url.AbsolutePath);
                        if (match.Success)
                        {
                            logger.Log($"Parsing conference talk {page.Url.AbsolutePath}");
                            ConferenceTalkDocument? structuredDoc = ConferenceTalkFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                            parsedDoc = structuredDoc;
                            if (structuredDoc == null)
                            {
                                //logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                            }
                            else
                            {
                                fileDestination = new VirtualPath($"general-conference\\{structuredDoc.Conference}\\{structuredDoc.TalkId}.json");
                            }
                        }
                        else
                        {
                            logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                        }
                    }
                }

                return Task.FromResult<bool>(true);
            }
        }

        private FlowDocument ConvertDocumentToFlowDocument(GospelDocument inputDoc)
        {
            FlowDocument returnVal = new FlowDocument();
            returnVal.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
            bool displayVerses = false;

            ScriptureChapterDocument? scriptureChapter = inputDoc as ScriptureChapterDocument;
            BibleDictionaryDocument? bibleDictEntry = inputDoc as BibleDictionaryDocument;
            ConferenceTalkDocument? confTalkEntry = inputDoc as ConferenceTalkDocument;

            // Build header
            if (scriptureChapter != null)
            {
                Section headerSection = new Section();
                displayVerses = true;

                Paragraph bookTitle = new Paragraph();
                bookTitle.TextAlignment = TextAlignment.Center;
                bookTitle.Inlines.Add(ScriptureMetadata.GetEnglishNameForBook(scriptureChapter.Book));
                headerSection.Blocks.Add(bookTitle);

                if (ScriptureMetadata.GetNumChaptesInBook(scriptureChapter.Book) > 1)
                {
                    Paragraph chapterNum = new Paragraph();
                    chapterNum.TextAlignment = TextAlignment.Center;
                    if (string.Equals(scriptureChapter.Book, "dc", StringComparison.Ordinal))
                    {
                        chapterNum.Inlines.Add($"Section {scriptureChapter.Chapter}");
                    }
                    else
                    {
                        // Todo: More refinements for this.
                        chapterNum.Inlines.Add($"Chapter {scriptureChapter.Chapter}");
                    }

                    headerSection.Blocks.Add(chapterNum);
                }

                returnVal.Blocks.Add(headerSection);

                if (scriptureChapter.ChapterHeader != null)
                {
                    Paragraph headerParagraph = new Paragraph();
                    headerParagraph.Tag = scriptureChapter.ChapterHeader.ParagraphEntityId.Serialize();
                    headerParagraph.Inlines.Add(new Italic(new Run(scriptureChapter.ChapterHeader.Text)));
                    returnVal.Blocks.Add(headerParagraph);
                }
            }
            else if (bibleDictEntry != null)
            {
                Section headerSection = new Section();
                Paragraph bookTitle = new Paragraph();
                bookTitle.TextAlignment = TextAlignment.Center;
                bookTitle.Inlines.Add(bibleDictEntry.Title);
                headerSection.Blocks.Add(bookTitle);
                returnVal.Blocks.Add(headerSection);
            }
            else if (confTalkEntry != null)
            {
                Section headerSection = new Section();

                Paragraph talkTitle = new Paragraph();
                talkTitle.TextAlignment = TextAlignment.Center;
                talkTitle.Inlines.Add(confTalkEntry.Title);
                headerSection.Blocks.Add(talkTitle);

                Paragraph talkSpeaker = new Paragraph();
                talkSpeaker.TextAlignment = TextAlignment.Center;
                talkSpeaker.Inlines.Add(confTalkEntry.Speaker);
                headerSection.Blocks.Add(talkSpeaker);

                returnVal.Blocks.Add(headerSection);
            }

            // Build blocks for all paragraphs
            int para = 1;
            foreach (GospelParagraph paragraph in inputDoc.Paragraphs)
            {
                Paragraph uiParagraph = new Paragraph();
                uiParagraph.Tag = paragraph.ParagraphEntityId.Serialize();
                if (displayVerses)
                {
                    Floater verseNumFloater = new Floater();
                    verseNumFloater.Padding = new Thickness(0);
                    verseNumFloater.Margin = new Thickness(0);
                    verseNumFloater.HorizontalAlignment = HorizontalAlignment.Left;
                    Paragraph numPara = new Paragraph();
                    numPara.Inlines.Add(para.ToString());
                    verseNumFloater.Blocks.Add(numPara);
                    uiParagraph.Inlines.Add(verseNumFloater);
                    para++;
                }

                uiParagraph.Inlines.Add(paragraph.Text);
                returnVal.Blocks.Add(uiParagraph);
            }

            // Buttons at the bottom of the document if prev/next chapters are enabled
            if (inputDoc is ScriptureChapterDocument)
            {
                scriptureChapter.AssertNonNull(nameof(scriptureChapter));
                if (scriptureChapter.Prev.HasValue || scriptureChapter.Next.HasValue)
                {
                    UniformGrid grid = new UniformGrid();
                    grid.Children.Add(new Button()
                    {
                        Content = "Previous",
                        IsEnabled = scriptureChapter.Prev.HasValue
                    });
                    grid.Children.Add(new Button()
                    {
                        Content = "Next",
                        IsEnabled = scriptureChapter.Next.HasValue
                    });

                    BlockUIContainer buttonContainer = new BlockUIContainer();
                    buttonContainer.Child = grid;
                    returnVal.Blocks.Add(buttonContainer);
                }
            }

            return returnVal;
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            //using (FileStream fileIn = new FileStream(@"D:\Code\scripturegraph\runtime\documents\bofm\alma-32.json", FileMode.Open, FileAccess.Read))
            //using (FileStream fileIn = new FileStream(@"D:\Code\scripturegraph\runtime\documents\bd\prayer.json", FileMode.Open, FileAccess.Read))
            using (FileStream fileIn = new FileStream(@"D:\Code\scripturegraph\runtime\documents\general-conference\2024-04\15dushku.json", FileMode.Open, FileAccess.Read))
            {
                ReadingPane2.Document = ConvertDocumentToFlowDocument(GospelDocument.ParsePolymorphic(fileIn));
            }
            
            //TextSelection s = ReadingPane1.Selection;
            //s.GetHashCode();
        }
    }
}