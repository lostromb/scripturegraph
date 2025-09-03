using Durandal.Common.Time;
using Durandal.Common.Utils;
using ScriptureGraph.App.Schemas;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Training;
using System.DirectoryServices;
using System.Linq;
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

        private HashSet<Guid> _activeSearchScopes = new HashSet<Guid>();
        private Dictionary<Guid, ReadingPane> _currentReadingPanes = new Dictionary<Guid, ReadingPane>();

        public MainWindow()
        {
            InitializeComponent();
            _core = ((App)Application.Current)._core;
            _searchBoxCommitter = new Committer(UpdateSearchResultsInBackground, DefaultRealTimeProvider.Singleton, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500));
            _latestSearchQuery = string.Empty;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _core.LoadSearchIndex();
            await _core.LoadDocumentLibrary();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void SearchTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchQuery = SearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                SearchFlyoutList.Children.Clear();
                SearchFlyoutList.Visibility = Visibility.Collapsed;
                return;
            }

            _latestSearchQuery = searchQuery;
            _searchBoxCommitter.Commit();
        }

        private Task UpdateSearchResultsInBackground(IRealTimeProvider realTime)
        {
            // BACKGROUND THREAD
            List<SearchQueryResult> searchResults = _core.RunSearchQuery(_latestSearchQuery).ToList();

            // UI THREAD
            Dispatcher.Invoke(() =>
            {
                SearchFlyoutList.Children.Clear();
                if (searchResults.Count > 0)
                {
                    SearchFlyoutList.Visibility = Visibility.Visible;
                }

                foreach (SearchQueryResult searchResult in searchResults)
                {
                    _core.CoreLogger.Log($"{searchResult.DisplayName} ({searchResult.EntityType.ToString()}) - {string.Join(",", searchResult.EntityIds.Length)}");
                    StackPanel horizontalPanel = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = searchResult
                    };

                    horizontalPanel.MouseEnter += SearchFlyoutItem_MouseEnter;
                    horizontalPanel.MouseLeave += SearchFlyoutItem_MouseLeave;
                    horizontalPanel.MouseDown += SearchFlyoutItem_MouseDown;

                    // Does this search result have a linked document?
                    KnowledgeGraphNodeId existingDocument = searchResult.EntityIds.FirstOrDefault((id) => _core.DoesDocumentExist(id));
                    if (existingDocument.Type != KnowledgeGraphNodeType.Unknown)
                    {
                        // A document exists! Make a link button for it
                        Button readButton = new Button()
                        {
                            Margin = new Thickness(0),
                            Padding = new Thickness(5, 10, 5, 10),
                            FontSize = 16,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        readButton.Content = "Load";
                        readButton.Tag = searchResult;
                        readButton.PreviewMouseDown += SearchFlyoutLoadButton_Click;
                        horizontalPanel.Children.Add(readButton);
                    }

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

            return Task.CompletedTask;
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

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchFlyoutList.Visibility = Visibility.Collapsed; 
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchFlyoutList.Visibility = SearchFlyoutList.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchFlyoutItem_MouseDown(object sender, System.Windows.Input.MouseEventArgs args)
        {
            SearchQueryResult selectedSearchResult = (SearchQueryResult)((FrameworkElement)sender).Tag;
            _core.CoreLogger.Log("Selected search result " + selectedSearchResult.DisplayName);
            CreateNewSearchScope(selectedSearchResult.EntityType, selectedSearchResult.DisplayName, selectedSearchResult.EntityIds);
            SearchTextBox.Text = string.Empty;
            SearchFlyoutList.Children.Clear();
            SearchFlyoutList.Visibility = Visibility.Collapsed;
        }

        private void SearchFlyoutItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs args)
        {
            ((StackPanel)sender).Background = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
        }

        private void SearchFlyoutItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs args)
        {
            ((StackPanel)sender).Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private async void SearchFlyoutLoadButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                SearchTextBox.Text = string.Empty;
                SearchFlyoutList.Children.Clear();
                SearchFlyoutList.Visibility = Visibility.Collapsed;

                await Task.Yield();
                SearchQueryResult searchResultForThisLineItem = (SearchQueryResult)((FrameworkElement)sender).Tag;
                await LoadDocumentForEntity(searchResultForThisLineItem.EntityIds.First((id) => _core.DoesDocumentExist(id)));
                CreateNewSearchScope(searchResultForThisLineItem.EntityType, searchResultForThisLineItem.DisplayName, searchResultForThisLineItem.EntityIds);
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private async void NextPrevChapterButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                await Task.Yield();
                KnowledgeGraphNodeId entityIdToLoad = (KnowledgeGraphNodeId)((FrameworkElement)sender).Tag;
                await LoadDocumentForEntity(entityIdToLoad);
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private ReadingPane CreateNewReadingPane()
        {
            //<Grid Name="ReadingPaneContainer1">
            //        <DockPanel>
            //            <Grid DockPanel.Dock="Top">
            //                <TextBlock
            //                    Background="{DynamicResource ReadingPaneHeaderColor}"
            //                    Padding="0,5,0,5"
            //                    TextAlignment="Center"
            //                    VerticalAlignment="Stretch">
            //                    Old Testament / Isaiah / Chapter 1
            //                </TextBlock>
            //                <Button Content="Close" Padding="5" HorizontalAlignment="Left"></Button>
            //            </Grid>
            //            <FlowDocumentScrollViewer
            //                Name="ReadingPane1"
            //                Width="{DynamicResource ReadingPaneWidth}">
            //            </FlowDocumentScrollViewer>
            //        </DockPanel>
            //    </Grid>

            Guid panelId = Guid.NewGuid();
            Grid readingPaneContainer = new Grid();
            DockPanel readingPaneDocker = new DockPanel();
            Grid headerGrid = new Grid();
            DockPanel.SetDock(headerGrid, Dock.Top);
            TextBlock header = new TextBlock();
            header.Background = (Brush)TryFindResource("ReadingPaneHeaderColor");
            header.Padding = new Thickness(0, 5, 0, 5);
            header.TextAlignment = TextAlignment.Center;
            header.VerticalAlignment = VerticalAlignment.Stretch;
            Button closeButton = new Button();
            closeButton.Content = "Close";
            closeButton.Padding = new Thickness(5);
            closeButton.HorizontalAlignment = HorizontalAlignment.Left;
            closeButton.Tag = panelId;
            closeButton.Click += ClosePanelButton_Click;
            FlowDocumentScrollViewer documentScrollViewer = new FlowDocumentScrollViewer();
            documentScrollViewer.Width = (double)TryFindResource("ReadingPaneWidth");

            readingPaneContainer.Children.Add(readingPaneDocker);
            readingPaneDocker.Children.Add(headerGrid);
            headerGrid.Children.Add(header);
            headerGrid.Children.Add(closeButton);
            readingPaneDocker.Children.Add(documentScrollViewer);

            return new ReadingPane()
            {
                PanelId = panelId,
                Container = readingPaneContainer,
                DocumentViewer = documentScrollViewer,
                Header = header
            };
        }

        private async Task LoadDocumentForEntity(KnowledgeGraphNodeId entityIdToLoad)
        {
            _core.CoreLogger.Log("Loading document for entity " + entityIdToLoad);
            GospelDocument document = await _core.LoadDocument(entityIdToLoad);
            FlowDocument readerDocument = ConvertDocumentToFlowDocument(document);
            string readingPaneHeader = ConvertDocumentEidToHeaderString(document);

            Dispatcher.Invoke(() =>
            {
                ReadingPane newUiPanels = CreateNewReadingPane();
                BrowseArea.Children.Add(newUiPanels.Container);
                _currentReadingPanes[newUiPanels.PanelId] = newUiPanels;
                BrowseArea.UpdateLayout();
                newUiPanels.Container.BringIntoView();
                newUiPanels.CurrentDocumentEntity = entityIdToLoad;

                newUiPanels.DocumentViewer.Document = readerDocument;
                //ScrollViewer? internalScrollViewer = typeof(FlowDocumentScrollViewer)!.GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic)!.GetValue(ReadingPane2) as ScrollViewer;
                //internalScrollViewer.ScrollToVerticalOffset();
                newUiPanels.Header.Text = readingPaneHeader;

                // this is jank and may need better logic
                newUiPanels.DocumentViewer.UpdateLayout();
                readerDocument.Blocks.LastBlock?.BringIntoView();
                newUiPanels.DocumentViewer.UpdateLayout();
                bool focusedOnParagraph = false;

                // If any blocks in the flow document have a tag equal to the thing we searched for, try to scroll to it immediately
                // This is for things like linking directly to scripture verses or talk paragraphs
                foreach (var block in readerDocument.Blocks)
                {
                    if (block.Tag != null && block.Tag is KnowledgeGraphNodeId paragraphEntity && entityIdToLoad.Equals(paragraphEntity))
                    {
                        block.Background = new SolidColorBrush(Color.FromArgb(32, 0, 255, 255));
                        block.BringIntoView();
                        focusedOnParagraph = true;
                        break;
                    }
                }

                if (!focusedOnParagraph)
                {
                    readerDocument.Blocks.FirstBlock?.BringIntoView();
                    newUiPanels.DocumentViewer.UpdateLayout();
                }
            });
        }

        private async void ClosePanelButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                Guid panelIdToRemove = (Guid)((FrameworkElement)sender).Tag;
                ReadingPane panelToRemove = _currentReadingPanes[panelIdToRemove];
                BrowseArea.Children.Remove(panelToRemove.Container);
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private async void RemoveScopeButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                UIElement elementToRemove = (UIElement)((FrameworkElement)sender).Tag;
                Guid scopeToRemove = (Guid)((FrameworkElement)elementToRemove).Tag;
                _activeSearchScopes.Remove(scopeToRemove);
                ActiveSearchScopes.Children.Remove(elementToRemove);

                // TODO remove it from the core
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void CreateNewSearchScope(SearchResultEntityType entityType, string scopeName, params KnowledgeGraphNodeId[] nodes)
        {
            Guid newScopeGuid = Guid.NewGuid();
            // Register it with the core
            // TODO

            // And make the UI for it
            DockPanel searchBubbleContainer = new DockPanel();
            searchBubbleContainer.Background = (Brush)TryFindResource("SearchBubbleBackground");
            searchBubbleContainer.Tag = newScopeGuid;

            DockPanel headerPanel = new DockPanel();
            DockPanel.SetDock(headerPanel, Dock.Top);
            headerPanel.Background = (Brush)TryFindResource("SearchBubbleHeaderBackground");

            TextBlock searchScopeTypeTextBlock = new TextBlock();
            searchScopeTypeTextBlock.Padding = new Thickness(0);
            searchScopeTypeTextBlock.Margin = new Thickness(0);
            searchScopeTypeTextBlock.HorizontalAlignment = HorizontalAlignment.Left;
            searchScopeTypeTextBlock.Text = ConvertSearchResultTypeToString(entityType);

            Button scopeRemoveButton = new Button();
            DockPanel.SetDock(scopeRemoveButton, Dock.Right);
            scopeRemoveButton.Width = 20;
            scopeRemoveButton.Height = 20;
            scopeRemoveButton.Padding = new Thickness(0);
            scopeRemoveButton.Margin = new Thickness(0);
            scopeRemoveButton.Content = "X";
            scopeRemoveButton.HorizontalAlignment = HorizontalAlignment.Right;
            scopeRemoveButton.HorizontalContentAlignment = HorizontalAlignment.Center;
            scopeRemoveButton.VerticalContentAlignment = VerticalAlignment.Center;
            scopeRemoveButton.Tag = searchBubbleContainer;
            scopeRemoveButton.Click += RemoveScopeButton_Click;

            TextBlock searchScopeNameTextBlock = new TextBlock();
            searchScopeNameTextBlock.Padding = new Thickness(5);
            DockPanel.SetDock(searchScopeNameTextBlock, Dock.Bottom);
            searchScopeNameTextBlock.Text = scopeName;

            ActiveSearchScopes.Children.Add(searchBubbleContainer);
            searchBubbleContainer.Children.Add(headerPanel);
            headerPanel.Children.Add(searchScopeTypeTextBlock);
            headerPanel.Children.Add(scopeRemoveButton);
            searchBubbleContainer.Children.Add(searchScopeNameTextBlock);

            _activeSearchScopes.Add(newScopeGuid);
        }

        private static string ConvertDocumentEidToHeaderString(GospelDocument document)
        {
            switch (document.DocumentType)
            {
                case GospelDocumentType.BibleDictionaryEntry:
                    return $"Bible Dictionary - {((BibleDictionaryDocument)document).Title}";
                case GospelDocumentType.GeneralConferenceTalk:
                    ConferenceTalkDocument conferenceDocument = (ConferenceTalkDocument)document;
                    string month = conferenceDocument.Conference.Phase == ConferencePhase.April ? "April" : "October";
                    return $"General Conference - {month} {conferenceDocument.Conference.Year} - {conferenceDocument.Speaker}";
                case GospelDocumentType.ScriptureChapter:
                    ScriptureChapterDocument scriptureDocument = (ScriptureChapterDocument)document;
                    return $"{ScriptureMetadata.GetEnglishNameForCanon(scriptureDocument.Canon)} - {ScriptureMetadata.GetEnglishNameForBook(scriptureDocument.Book)} - {scriptureDocument.Chapter}";
                default:
                    return "UNKNOWN_DOCUMENT";
            }
        }

        private FlowDocument ConvertDocumentToFlowDocument(GospelDocument inputDoc)
        {
            FlowDocument returnVal = new FlowDocument();
            ScriptureChapterDocument? scriptureChapter = inputDoc as ScriptureChapterDocument;
            BibleDictionaryDocument? bibleDictEntry = inputDoc as BibleDictionaryDocument;
            ConferenceTalkDocument? confTalkEntry = inputDoc as ConferenceTalkDocument;

            FontFamily bodyParaFont = (FontFamily)TryFindResource("SerifFontFamily");
            Thickness bodyParaMargin = (Thickness)TryFindResource("DefaultParagraphMargin");
            double titleFontSize = (double)TryFindResource("TitleFontSize");
            Thickness titleMargin = (Thickness)TryFindResource("TitleMargin");
            double subtitleFontSize = (double)TryFindResource("SubtitleFontSize");
            Thickness subtitleMargin = (Thickness)TryFindResource("SubtitleMargin");
            double bodyParaFontSize = (double)TryFindResource("VerseFontSize");
            double verseNumFontSize = (double)TryFindResource("VerseNumFontSize");
            Thickness verseNumMargin = (Thickness)TryFindResource("VerseNumMargin");
            bool displayVerses = false;

            // Determine what styles we need to use
            if (inputDoc is ScriptureChapterDocument)
            {
                bodyParaMargin = (Thickness)TryFindResource("ScriptureParagraphMargin");
                displayVerses = true;
            }


            returnVal.Background = (Brush)TryFindResource("DocumentReaderPageBackground");
            returnVal.FontFamily = bodyParaFont;
            returnVal.FontSize = bodyParaFontSize;

            // Build header
            if (scriptureChapter != null)
            {
                Section headerSection = new Section();

                Paragraph bookTitle = new Paragraph();
                bookTitle.TextAlignment = TextAlignment.Center;
                bookTitle.Margin = titleMargin;
                bookTitle.FontSize = titleFontSize;
                bookTitle.Inlines.Add(ScriptureMetadata.GetEnglishNameForBook(scriptureChapter.Book));
                headerSection.Blocks.Add(bookTitle);

                if (ScriptureMetadata.GetNumChaptersInBook(scriptureChapter.Book) > 1)
                {
                    Paragraph chapterNum = new Paragraph();
                    chapterNum.TextAlignment = TextAlignment.Center;
                    chapterNum.Margin = subtitleMargin;
                    chapterNum.FontSize = subtitleFontSize;
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
                    headerParagraph.Tag = scriptureChapter.ChapterHeader.ParagraphEntityId;
                    headerParagraph.Margin = bodyParaMargin;
                    headerParagraph.TextAlignment = TextAlignment.Justify;
                    headerParagraph.Inlines.Add(new Italic(new Run(scriptureChapter.ChapterHeader.Text)));
                    returnVal.Blocks.Add(headerParagraph);
                }
            }
            else if (bibleDictEntry != null)
            {
                Section headerSection = new Section();
                Paragraph bookTitle = new Paragraph();
                bookTitle.Margin = titleMargin;
                bookTitle.FontSize = titleFontSize;
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
                talkTitle.Margin = titleMargin;
                talkTitle.FontSize = titleFontSize;
                talkTitle.Inlines.Add(confTalkEntry.Title);
                headerSection.Blocks.Add(talkTitle);

                Paragraph talkSpeaker = new Paragraph();
                talkSpeaker.TextAlignment = TextAlignment.Center;
                talkSpeaker.Inlines.Add(confTalkEntry.Speaker);
                talkSpeaker.Margin = subtitleMargin;
                talkSpeaker.FontSize = subtitleFontSize;
                headerSection.Blocks.Add(talkSpeaker);

                returnVal.Blocks.Add(headerSection);
            }

            // Build blocks for all paragraphs
            int para = 1;
            foreach (GospelParagraph paragraph in inputDoc.Paragraphs)
            {
                Paragraph uiParagraph = new Paragraph();
                uiParagraph.Tag = paragraph.ParagraphEntityId;

                uiParagraph.Margin = bodyParaMargin;
                uiParagraph.TextAlignment = TextAlignment.Justify;

                if (displayVerses)
                {
                    Floater verseNumFloater = new Floater();
                    verseNumFloater.Padding = new Thickness(0);
                    verseNumFloater.HorizontalAlignment = HorizontalAlignment.Left;
                    verseNumFloater.FontSize = verseNumFontSize;
                    verseNumFloater.Margin = verseNumMargin;
                    verseNumFloater.Tag = paragraph.ParagraphEntityId;
                    Paragraph numPara = new Paragraph();
                    numPara.Inlines.Add(para.ToString());
                    numPara.Tag = paragraph.ParagraphEntityId;
                    verseNumFloater.Blocks.Add(numPara);
                    uiParagraph.Inlines.Add(verseNumFloater);
                    para++;
                }

                // TODO: Remove the "fancy" formatting from documents?
                // Like nbsp and fancy quotes and stuff - they might mess up copy and pasting
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

                    Button prevButton = new Button()
                    {
                        Content = "Previous",
                        IsEnabled = scriptureChapter.Prev.HasValue,
                    };

                    Button nextButton = new Button()
                    {
                        Content = "Next",
                        IsEnabled = scriptureChapter.Next.HasValue
                    };

                    if (scriptureChapter.Prev.HasValue)
                    {
                        prevButton.Tag = scriptureChapter.Prev.Value;
                        prevButton.Click += NextPrevChapterButton_Click;
                    }

                    if (scriptureChapter.Next.HasValue)
                    {
                        nextButton.Tag = scriptureChapter.Next.Value;
                        nextButton.Click += NextPrevChapterButton_Click;
                    }

                    nextButton.Click += NextPrevChapterButton_Click;

                    grid.Children.Add(prevButton);
                    grid.Children.Add(nextButton);

                    BlockUIContainer buttonContainer = new BlockUIContainer();
                    buttonContainer.Child = grid;
                    returnVal.Blocks.Add(buttonContainer);
                }
            }

            return returnVal;
        }
    }
}