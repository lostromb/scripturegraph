using Durandal.Common.File;
using Durandal.Common.Instrumentation;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Tasks;
using Durandal.Common.Time;
using Durandal.Common.Utils;
using Durandal.Common.Utils.NativePlatform;
using Org.BouncyCastle.Tls;
using ScriptureGraph.App.Schemas;
using ScriptureGraph.Core;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace ScriptureGraph.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppCore _core;

        private readonly Committer _searchBoxCommitter;

        private string _latestSearchQuery;

        public MainWindow()
        {
            InitializeComponent();
            _core = ((App)Application.Current)._core;
            _searchBoxCommitter = new Committer(UpdateSearchResultsInBackground, DefaultRealTimeProvider.Singleton, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500));
            _latestSearchQuery = string.Empty;
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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string graphFileName = @"D:\Code\scripturegraph\runtime\searchindex.graph";
            string indexFileName = @"D:\Code\scripturegraph\runtime\entitynames_eng.map";

            if (!File.Exists(graphFileName))
            {
                throw new Exception("Can't find search index file");
            }

            if (!File.Exists(indexFileName))
            {
                throw new Exception("Can't find name index file");
            }

            using (FileStream searchGraphIn = new FileStream(graphFileName, FileMode.Open, FileAccess.Read))
            using (FileStream searchIndexIn = new FileStream(indexFileName, FileMode.Open, FileAccess.Read))
            {
                await _core.LoadSearchIndex(searchGraphIn, searchIndexIn);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void SearchTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchQuery = SearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return;
            }

            _latestSearchQuery = searchQuery;
            _searchBoxCommitter.Commit();
        }

        private async Task UpdateSearchResultsInBackground(IRealTimeProvider realTime)
        {
            // BACKGROUND THREAD
            List<SearchQueryResult> searchResults = _core.RunSearchQuery(_latestSearchQuery).ToList();

            // UI THREAD
            Dispatcher.Invoke(() =>
            {
                SearchFlyoutList.Children.Clear();
                foreach (SearchQueryResult searchResult in searchResults)
                {
                    _core.CoreLogger.Log($"{searchResult.DisplayName} ({searchResult.EntityType.ToString()}) - {string.Join(",", searchResult.EntityIds.Length)}");
                    StackPanel horizontalPanel = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    TextBlock nameBlock = new TextBlock()
                    {
                        Margin = new Thickness(10),
                        FontSize = 16,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = searchResult.DisplayName
                    };

                    TextBlock typeBlock = new TextBlock()
                    {
                        Margin = new Thickness(10),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = ConvertSearchResultTypeToString(searchResult.EntityType)
                    };

                    horizontalPanel.Children.Add(nameBlock);
                    horizontalPanel.Children.Add(typeBlock);

                    SearchFlyoutList.Children.Add(horizontalPanel);
                }
            });
        }

        private static string ConvertSearchResultTypeToString(SearchResultEntityType entityType)
        {
            switch (entityType)
            {
                case SearchResultEntityType.ScriptureBook:
                    return "Scripture Book";
                case SearchResultEntityType.ScriptureChapter:
                    return "Scripture Chapter";
                case SearchResultEntityType.ScriptureVerse:
                    return "Scripture Verse";
                case SearchResultEntityType.Person:
                    return "Person";
                case SearchResultEntityType.KeywordPhrase:
                    return "Keyword or Phrase";
                case SearchResultEntityType.ConferenceTalk:
                    return "General Conference Address";
                case SearchResultEntityType.Topic:
                    return "Topic";
                default:
                    return "UNKNOWN_TYPE";
            }
        }
    }
}