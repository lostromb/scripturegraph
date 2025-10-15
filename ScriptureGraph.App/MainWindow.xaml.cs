using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Time;
using Durandal.Common.Utils;
using ScriptureGraph.App.Schemas;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
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

        private Dictionary<Guid, KnowledgeGraphNodeId[]> _activeSearchScopes = new Dictionary<Guid, KnowledgeGraphNodeId[]>();
        private Dictionary<Guid, ReadingPane> _currentReadingPanes = new Dictionary<Guid, ReadingPane>();
        private UIElement? _currentSearchResultsPane;
        private KnowledgeGraphNodeId? _lastRightClickedParagraph;

        public MainWindow()
        {
            InitializeComponent();
            _core = ((App)Application.Current)._core;
            _searchBoxCommitter = new Committer(UpdateSearchResultsInBackground, DefaultRealTimeProvider.Singleton, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500));
            _latestSearchQuery = string.Empty;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _core.LoadSearchIndexes();
            await _core.LoadDocumentLibrary();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                await LoadDocumentForEntity(FeatureToNodeMapping.BookChapter("messiah2", "1"));
                return;

                if (_activeSearchScopes.Count == 0)
                {
                    CloseSearchResultsIfPresent();
                    return;
                }

                SearchButton.IsEnabled = false;

                SlowSearchQuery query = new SlowSearchQuery()
                {
                    SearchScopes = new List<KnowledgeGraphNodeId[]>(),
                    IgnoredDocumentScopes = new HashSet<KnowledgeGraphNodeId>(),
                    MaxResults = 50,
                    MinConfidence = 0.1f,
                    CategoryFilters = new ResultFilterSet()
                    {
                        Include_OldTestament = FilterCheckBox_OT.IsChecked.GetValueOrDefault(false),
                        Include_NewTestament = FilterCheckBox_NT.IsChecked.GetValueOrDefault(false),
                        Include_BookOfMormon = FilterCheckBox_BOFM.IsChecked.GetValueOrDefault(false),
                        Include_DC = FilterCheckBox_DC.IsChecked.GetValueOrDefault(false),
                        Include_PearlGP = FilterCheckBox_PGP.IsChecked.GetValueOrDefault(false),
                        Include_BibleDict = FilterCheckBox_BD.IsChecked.GetValueOrDefault(false),
                        Include_GenConference = FilterCheckBox_GC.IsChecked.GetValueOrDefault(false),
                        Include_Books = FilterCheckBox_Books.IsChecked.GetValueOrDefault(false),
                        Include_Hymns = FilterCheckBox_Hymns.IsChecked.GetValueOrDefault(false),
                        Include_Speeches = FilterCheckBox_Speeches.IsChecked.GetValueOrDefault(false),
                    }
                };

                foreach (var searchScope in _activeSearchScopes)
                {
                    query.SearchScopes.Add(searchScope.Value);
                }

                // Tell the search to ignore panels we already have open
                foreach (var readingPane in _currentReadingPanes)
                {
                    if (readingPane.Value.CurrentDocumentEntity.HasValue &&
                        !query.IgnoredDocumentScopes.Contains(readingPane.Value.CurrentDocumentEntity.Value))
                    {
                        query.IgnoredDocumentScopes.Add(readingPane.Value.CurrentDocumentEntity.Value);
                    }
                }

                SlowSearchQueryResult searchResults = await Task.Run(() => _core.RunSlowSearchQuery(query)).ConfigureAwait(true);

                // We're still on the UI thread so no need to dispatch
                Grid searchResultsPane = await CreateNewSearchResultsPane(searchResults).ConfigureAwait(true);
                if (_currentSearchResultsPane != null)
                {
                    BrowseArea.Children.Remove(_currentSearchResultsPane);
                }

                _currentSearchResultsPane = searchResultsPane;
                BrowseArea.Children.Add(searchResultsPane);
                BrowseArea.UpdateLayout();
                searchResultsPane.BringIntoView();
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
            finally
            {
                SearchButton.IsEnabled = true;
            }
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs args)
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
            List<FastSearchQueryResult> searchResults = _core.RunFastSearchQuery(_latestSearchQuery).ToList();

            // UI THREAD
            Dispatcher.Invoke(() =>
            {
                SearchFlyoutList.Children.Clear();
                if (searchResults.Count > 0)
                {
                    SearchFlyoutList.Visibility = Visibility.Visible;
                }

                foreach (FastSearchQueryResult searchResult in searchResults)
                {
                    _core.CoreLogger.Log($"{searchResult.DisplayName} ({searchResult.EntityType.ToString()}) - {string.Join(",", searchResult.EntityIds.Length)}");
                    StackPanel horizontalPanel = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = searchResult,
                        ToolTip = "Click this result to add it to the search terms",
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
                        readButton.ToolTip = "Begin reading this document immediately without searching";
                        readButton.PreviewMouseDown += SearchFlyoutLoadButton_Click;
                        horizontalPanel.Children.Add(readButton);
                    }

                    StackPanel verticalPanel = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                    };

                    TextBlock nameBlock = new TextBlock()
                    {
                        Margin = new Thickness(10, 0, 0, 0),
                        FontSize = 16,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = searchResult.DisplayName,
                    };

                    TextBlock typeBlock = new TextBlock()
                    {
                        Margin = new Thickness(10, 0, 0, 0),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = ConvertSearchResultTypeToString(searchResult.EntityType),
                    };

                    verticalPanel.Children.Add(nameBlock);
                    verticalPanel.Children.Add(typeBlock);
                    horizontalPanel.Children.Add(verticalPanel);

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
                case SearchResultEntityType.BibleDictionary:
                    return "Bible Dictionary";
                case SearchResultEntityType.Book_ATGQ:
                    return "Answers to Gospel Questions";
                case SearchResultEntityType.Book_MD:
                    return "Mormon Doctrine";
                case SearchResultEntityType.Book_Messiah1:
                    return "Promised Messiah";
                case SearchResultEntityType.Book_Messiah2:
                    return "Mortal Messiah";
                case SearchResultEntityType.Book_Messiah3:
                    return "Millennial Messiah";
                case SearchResultEntityType.ByuSpeech:
                    return "BYU Speeches";
                case SearchResultEntityType.Hymn:
                    return "Hymns";
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

        private void SearchFlyoutItem_MouseDown(object sender, MouseEventArgs args)
        {
            try
            {
                FastSearchQueryResult selectedSearchResult = (FastSearchQueryResult)((FrameworkElement)sender).Tag;
                _core.CoreLogger.Log("Selected search result " + selectedSearchResult.DisplayName);
                //CloseSearchResultsIfPresent();
                CreateNewSearchScope(selectedSearchResult.EntityType, selectedSearchResult.DisplayName, selectedSearchResult.EntityIds);
                SearchTextBox.Text = string.Empty;
                SearchFlyoutList.Children.Clear();
                SearchFlyoutList.Visibility = Visibility.Collapsed;
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void SearchFlyoutItem_MouseEnter(object sender, MouseEventArgs args)
        {
            ((StackPanel)sender).Background = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
        }

        private void SearchFlyoutItem_MouseLeave(object sender, MouseEventArgs args)
        {
            ((StackPanel)sender).Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void CloseSearchResultsIfPresent()
        {
            if (_currentSearchResultsPane != null)
            {
                BrowseArea.Children.Remove(_currentSearchResultsPane);
                _currentSearchResultsPane = null;
            }
        }

        private async void SearchFlyoutLoadButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                SearchTextBox.Text = string.Empty;
                SearchFlyoutList.Children.Clear();
                SearchFlyoutList.Visibility = Visibility.Collapsed;
                //CloseSearchResultsIfPresent();

                await Task.Yield();
                FastSearchQueryResult searchResultForThisLineItem = (FastSearchQueryResult)((FrameworkElement)sender).Tag;
                await LoadDocumentForEntity(searchResultForThisLineItem.EntityIds.First((id) => _core.DoesDocumentExist(id)));
                //CreateNewSearchScope(searchResultForThisLineItem.EntityType, searchResultForThisLineItem.DisplayName, searchResultForThisLineItem.EntityIds);
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
                Tuple<KnowledgeGraphNodeId, Guid> entityIdAndPaneToLoad = (Tuple<KnowledgeGraphNodeId, Guid>)((FrameworkElement)sender).Tag;
                await LoadDocumentIntoExistingPane(entityIdAndPaneToLoad.Item1, entityIdAndPaneToLoad.Item2);
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
            closeButton.ToolTip = "Close this reading pane";
            closeButton.Padding = new Thickness(5);
            closeButton.HorizontalAlignment = HorizontalAlignment.Left;
            closeButton.Tag = panelId;
            closeButton.Click += ClosePanelButton_Click;

            //Button addToSearchButton = new Button();
            //addToSearchButton.Content = "Search";
            //addToSearchButton.Padding = new Thickness(5);
            //addToSearchButton.HorizontalAlignment = HorizontalAlignment.Right;
            //addToSearchButton.Tag = panelId;
            //addToSearchButton.Click += SearchFromPanelButton_Click;

            FlowDocumentScrollViewer documentScrollViewer = new FlowDocumentScrollViewer();
            documentScrollViewer.Width = (double)TryFindResource("ReadingPaneWidth");
            documentScrollViewer.Tag = panelId;

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.Tag = panelId;
            MenuItem quickFootnotesItem = new MenuItem()
            {
                Header = "Quick Footnotes",
                Tag = panelId
            };
            MenuItem addToSearchItem = new MenuItem()
            {
                Header = "Add this document to search",
                Tag = panelId
            };
            MenuItem addParaToSearchItem = new MenuItem()
            {
                Header = "Add this paragraph to search",
                Tag = panelId
            };
            MenuItem searchTextItem = new MenuItem()
            {
                Header = "Add selected text to search",
                Tag = panelId
            };

            quickFootnotesItem.Click += QuickFootnotesMenuItem_Click;
            addToSearchItem.Click += AddEntireDocumentToSearchMenuItem_Click;
            addParaToSearchItem.Click += AddParagraphToSearchMenuItem_Click;
            searchTextItem.Click += AddSelectionToSearchMenuItem_Click;

            documentScrollViewer.ContextMenuOpening += ReadingPane_ContextMenuOpening;


            readingPaneContainer.Children.Add(readingPaneDocker);
            readingPaneDocker.Children.Add(headerGrid);
            headerGrid.Children.Add(header);
            headerGrid.Children.Add(closeButton);
            //headerGrid.Children.Add(addToSearchButton);
            readingPaneDocker.Children.Add(documentScrollViewer);
            documentScrollViewer.ContextMenu = contextMenu;
            contextMenu.Items.Add(quickFootnotesItem);
            contextMenu.Items.Add(addToSearchItem);
            contextMenu.Items.Add(addParaToSearchItem);
            contextMenu.Items.Add(searchTextItem);

            return new ReadingPane()
            {
                PanelId = panelId,
                Container = readingPaneContainer,
                DocumentViewer = documentScrollViewer,
                Header = header,
                HeaderText = "UNKNOWN",
            };
        }

        private async void ReadingPane_ContextMenuOpening(object sender, ContextMenuEventArgs args)
        {
            try
            {
                // See if the current reading pane has any currently selected text, and enable / disable menu items based on that
                FlowDocumentScrollViewer scrollViewer = (FlowDocumentScrollViewer)sender;
                //Guid sourcePanelId = (Guid)scrollViewer.Tag;
                // Access the "add selection to search" menu item based on hardcoded index
                // wish there was a better way to access this, oh well (tag is already taken by the panel guid)
                MenuItem menuItem_AddSelectionToSearch = (MenuItem)scrollViewer.ContextMenu.Items.GetItemAt(2);
                menuItem_AddSelectionToSearch.IsEnabled =
                    scrollViewer.IsSelectionActive &&
                    scrollViewer.Selection != null &&
                    !scrollViewer.Selection.IsEmpty;

                // Also do a hit bounds check to allow right click -> quick footnotes search (need to figure out which paragraph was clicked though)
                _lastRightClickedParagraph = null;
                foreach (var block in scrollViewer.Document.Blocks)
                {
                    if (block.IsMouseOver && block.Tag != null && block.Tag is KnowledgeGraphNodeId selectedParaNodeId)
                    {
                        _lastRightClickedParagraph = selectedParaNodeId;
                        _core.CoreLogger.Log("Detected context menu selection on entity: " + selectedParaNodeId);
                        break;
                    }
                }

                MenuItem menuItem_QuickFootnotes = (MenuItem)scrollViewer.ContextMenu.Items.GetItemAt(0);
                menuItem_QuickFootnotes.IsEnabled = _lastRightClickedParagraph.HasValue;
                MenuItem menuItem_AddPara = (MenuItem)scrollViewer.ContextMenu.Items.GetItemAt(2);
                menuItem_AddPara.IsEnabled = _lastRightClickedParagraph.HasValue;

                while (scrollViewer.ContextMenu.Items.Count > 4)
                {
                    scrollViewer.ContextMenu.Items.RemoveAt(4);
                }

                if (!_lastRightClickedParagraph.HasValue ||
                    _lastRightClickedParagraph.Value.Type == KnowledgeGraphNodeType.ScriptureVerse)
                {
                    return;
                }

                // Add direct scripture references by doing a quick search
                SlowSearchQuery query = new SlowSearchQuery()
                {
                    SearchScopes = new List<KnowledgeGraphNodeId[]>(),
                    IgnoredDocumentScopes = new HashSet<KnowledgeGraphNodeId>(),
                    MaxResults = 10,
                    MinConfidence = 2.0f,
                    CategoryFilters = new ResultFilterSet()
                    {
                        Include_OldTestament = true,
                        Include_NewTestament = true,
                        Include_BookOfMormon = true,
                        Include_DC = true,
                        Include_PearlGP = true,
                    }
                };

                query.SearchScopes.Add(new KnowledgeGraphNodeId[] { _lastRightClickedParagraph.Value });

                SlowSearchQueryResult searchResults = await Task.Run(() => _core.RunSlowSearchQuery(query)).ConfigureAwait(true);

                var verses = searchResults.EntityIds
                    .Where(s => s.Type == KnowledgeGraphNodeType.ScriptureVerse || s.Type == KnowledgeGraphNodeType.ScriptureBook)
                    .Select(s => new ScriptureReference(s))
                    .ToList();
                if (verses.Count > 0)
                {
                    scrollViewer.ContextMenu.Items.Add(new Separator());
                    foreach (ScriptureReference verse in verses)
                    {
                        string menuItemText;
                        if (verse.Chapter.HasValue && verse.Verse.HasValue)
                        {
                            menuItemText = $"Open {ScriptureMetadataEnglish.GetNameForBook(verse.Book)} {verse.Chapter.Value}:{verse.Verse.Value}";
                        }
                        else if (verse.Chapter.HasValue && string.IsNullOrEmpty(verse.Paragraph))
                        {
                            menuItemText = $"Open {ScriptureMetadataEnglish.GetNameForBook(verse.Book)} {verse.Chapter.Value}";
                        }
                        else
                        {
                            continue;
                        }

                        MenuItem verseLinkItem = new MenuItem()
                        {
                            Header = menuItemText,
                            Tag = verse
                        };
                        verseLinkItem.Click += DirectScriptureLinkMenuItem_Click;
                        scrollViewer.ContextMenu.Items.Add(verseLinkItem);
                        
                    }
                }
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private async Task<Grid> CreateNewSearchResultsPane(SlowSearchQueryResult searchResults, string? title = null)
        {
            //<Grid Name="ReadingPaneContainer1">
            //        <DockPanel>
            //            <Grid DockPanel.Dock="Top">
            //                <TextBlock
            //                    Background="{DynamicResource ReadingPaneHeaderColor}"
            //                    Padding="0,5,0,5"
            //                    TextAlignment="Center"
            //                    VerticalAlignment="Stretch">
            //                    Search Results
            //                </TextBlock>
            //                <Button Content="Close" Padding="5" HorizontalAlignment="Left"></Button>
            //            </Grid>
            //<TextBlock
            //    Background="{DynamicResource SearchResultLabelBackground}"
            //    Text="Book of Mormon - Alma 36:20"
            //    IsManipulationEnabled="False"></TextBlock>
            //<TextBlock 
            //    Background="{DynamicResource DocumentReaderPageBackground}"
            //    FontFamily="{DynamicResource SerifFontFamily}"
            //    FontSize="{DynamicResource VerseFontSize}"
            //    TextWrapping="Wrap"
            //    TextAlignment="Justify"
            //    Padding="5"
            //    IsManipulationEnabled="False"
            //    Text="And oh, what joy, and what marvelous light I did behold; yea, my soul was filled with joy as exceeding as was my pain!"></TextBlock>
            //        </DockPanel>
            //    </Grid>

            List<KnowledgeGraphNodeId> searchResultEntities = searchResults.EntityIds;

            Guid panelId = Guid.NewGuid();
            Grid resultsPaneContainer = new Grid();
            DockPanel resultsPaneDocker = new DockPanel();
            Grid headerGrid = new Grid();
            DockPanel.SetDock(headerGrid, Dock.Top);
            TextBlock header = new TextBlock();
            header.Background = (Brush)TryFindResource("ReadingPaneHeaderColor");
            header.Padding = new Thickness(0, 5, 0, 5);
            header.TextAlignment = TextAlignment.Center;
            header.VerticalAlignment = VerticalAlignment.Stretch;
            header.Text = title ?? "Search Results";
            Button closeButton = new Button();
            closeButton.Content = "Close";
            closeButton.ToolTip = "Close this reading pane";
            closeButton.Padding = new Thickness(5);
            closeButton.HorizontalAlignment = HorizontalAlignment.Left;
            closeButton.Tag = panelId;
            closeButton.Click += CloseSearchPanelButton_Click;

            ScrollViewer searchResultsScroller = new ScrollViewer();
            searchResultsScroller.Width = (double)TryFindResource("ReadingPaneWidth");

            StackPanel searchResultsStacker = new StackPanel();
            searchResultsStacker.Orientation = Orientation.Vertical;

            // Stack the search results in here...
            foreach (var resultEntity in searchResultEntities)
            {
                await CreateUiElementsForSearchResult(resultEntity, searchResultsStacker.Children, searchResults.ActivatedWords);
            }

            resultsPaneContainer.Children.Add(resultsPaneDocker);
            resultsPaneDocker.Children.Add(headerGrid);
            headerGrid.Children.Add(header);
            headerGrid.Children.Add(closeButton);
            resultsPaneDocker.Children.Add(searchResultsScroller);
            searchResultsScroller.Content = searchResultsStacker;

            return resultsPaneContainer;
        }

        private async Task LoadDocumentIntoExistingPane(KnowledgeGraphNodeId entityIdToLoad, Guid panelGuid)
        {
            _core.CoreLogger.Log("Loading document for entity " + entityIdToLoad + " into pane " + panelGuid);
            GospelDocument document = await _core.LoadDocument(entityIdToLoad); // FIXME no validation of existing panel guids
            FlowDocument readerDocument = ConvertDocumentToFlowDocument(document, panelGuid);
            string readingPaneHeader = ConvertDocumentEidToHeaderString(document);
            ReadingPane existingReadingPanel = _currentReadingPanes[panelGuid];
            existingReadingPanel.HeaderText = readingPaneHeader;
            existingReadingPanel.CurrentDocument = document;

            Dispatcher.Invoke(() =>
            {
                // FIXME this shares a lot of code with LoadDocumentForEntity(), should probably consolidate
                existingReadingPanel.CurrentDocumentEntity = entityIdToLoad;

                existingReadingPanel.DocumentViewer.Document = readerDocument;
                //ScrollViewer? internalScrollViewer = typeof(FlowDocumentScrollViewer)!.GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic)!.GetValue(ReadingPane2) as ScrollViewer;
                //internalScrollViewer.ScrollToVerticalOffset();
                existingReadingPanel.Header.Text = readingPaneHeader;

                // this is jank and may need better logic
                existingReadingPanel.DocumentViewer.UpdateLayout();
                readerDocument.Blocks.LastBlock?.BringIntoView();
                existingReadingPanel.DocumentViewer.UpdateLayout();
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
                    existingReadingPanel.DocumentViewer.UpdateLayout();
                }
            });
        }

        private async Task LoadDocumentForEntity(KnowledgeGraphNodeId entityIdToLoad)
        {
            _core.CoreLogger.Log("Loading document for entity " + entityIdToLoad);
            GospelDocument document = await _core.LoadDocument(entityIdToLoad);
            ReadingPane newUiPanels = CreateNewReadingPane();
            FlowDocument readerDocument = ConvertDocumentToFlowDocument(document, newUiPanels.PanelId);
            string readingPaneHeader = ConvertDocumentEidToHeaderString(document);
            newUiPanels.HeaderText = readingPaneHeader;
            newUiPanels.CurrentDocument = document;

            Dispatcher.Invoke(() =>
            {
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

        private void ClosePanelButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                Guid panelIdToRemove = (Guid)((FrameworkElement)sender).Tag;
                ReadingPane panelToRemove = _currentReadingPanes[panelIdToRemove];
                BrowseArea.Children.Remove(panelToRemove.Container);
                _currentReadingPanes.Remove(panelIdToRemove);
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void AddEntireDocumentToSearchMenuItem_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                Guid panelToAddToSearch = (Guid)((FrameworkElement)sender).Tag;
                if (!_currentReadingPanes.ContainsKey(panelToAddToSearch))
                {
                    return;
                }

                ReadingPane panel = _currentReadingPanes[panelToAddToSearch];

                if (panel.CurrentDocument == null || !panel.CurrentDocumentEntity.HasValue)
                {
                    return;
                }

                CreateNewSearchScope(
                    AppCore.ConvertEntityTypeToSearchResponseType(panel.CurrentDocumentEntity.Value),
                    _core.GetPrettyNameForEntity(panel.CurrentDocumentEntity.Value),
                    panel.CurrentDocumentEntity.Value);
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void AddParagraphToSearchMenuItem_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                if (!_lastRightClickedParagraph.HasValue)
                {
                    return;
                }

                string documentPrettyName = _core.GetPrettyNameForEntity(_lastRightClickedParagraph.Value);

                CreateNewSearchScope(
                    AppCore.ConvertEntityTypeToSearchResponseType(_lastRightClickedParagraph.Value),
                    _core.GetPrettyNameForEntity(_lastRightClickedParagraph.Value),
                    _lastRightClickedParagraph.Value);
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void AddSelectionToSearchMenuItem_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                Guid panelToAddToSearch = (Guid)((FrameworkElement)sender).Tag;
                if (!_currentReadingPanes.ContainsKey(panelToAddToSearch))
                {
                    return;
                }

                ReadingPane panel = _currentReadingPanes[panelToAddToSearch];

                if (panel.CurrentDocument == null || !panel.CurrentDocumentEntity.HasValue)
                {
                    return;
                }

                // Get the selected text from the reading pane and make sure it is non-null
                TextSelection selection = panel.DocumentViewer.Selection;
                string selectionRawText = selection.Text;
                if (selection == null || selection.IsEmpty)
                {
                    _core.CoreLogger.Log("Text selection is empty; not adding to search");
                    return;
                }

                // Extract features from text
                List<KnowledgeGraphNodeId> entities = new List<KnowledgeGraphNodeId>(256);
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(selectionRawText))
                {
                    entities.Add(ngram);
                }

                // And get the entities for the paragraphs / verses spanned by the selection
                TextPointer currentPointer = selection.Start;
                KnowledgeGraphNodeId? firstParagraphEntityAdded = null;
                KnowledgeGraphNodeId lastParagraphEntityAdded = default;
                if (currentPointer.Paragraph != null &&
                    currentPointer.Paragraph.Tag != null &&
                    currentPointer.Paragraph.Tag is KnowledgeGraphNodeId paragraphTag)
                {
                    lastParagraphEntityAdded = paragraphTag;
                    firstParagraphEntityAdded = paragraphTag;
                    entities.Add(paragraphTag);
                }

                while (currentPointer.GetOffsetToPosition(selection.End) > 0)
                {
                    currentPointer = currentPointer.GetNextContextPosition(LogicalDirection.Forward);
                    if (currentPointer.Paragraph != null &&
                        currentPointer.Paragraph.Tag != null &&
                        currentPointer.Paragraph.Tag is KnowledgeGraphNodeId paragraphTag2 &&
                        !paragraphTag2.Equals(lastParagraphEntityAdded))
                    {
                        lastParagraphEntityAdded = paragraphTag2;
                        entities.Add(paragraphTag2);
                    }
                }

                // Build a friendly name for this search scope
                KnowledgeGraphNodeId entityToUseForHeaders = firstParagraphEntityAdded.GetValueOrDefault(panel.CurrentDocumentEntity.Value);

                string documentPrettyName = _core.GetPrettyNameForEntity(entityToUseForHeaders);
                StringBuilder friendlyNameBuilder = new StringBuilder();
                friendlyNameBuilder.Append("\"");
                int lastAppendedIndex = 0;
                int nextSpaceIndex = 0;
                while (nextSpaceIndex >= 0 && friendlyNameBuilder.Length < 40)
                {
                    nextSpaceIndex = selectionRawText.IndexOf(' ', nextSpaceIndex + 1);
                    if (nextSpaceIndex < 0)
                    {
                        friendlyNameBuilder.Append(selectionRawText.AsSpan(lastAppendedIndex));
                    }
                    else
                    {
                        friendlyNameBuilder.Append(selectionRawText.AsSpan(lastAppendedIndex, nextSpaceIndex - lastAppendedIndex));
                    }

                    lastAppendedIndex = nextSpaceIndex;
                }

                if (nextSpaceIndex >= 0)
                {
                    friendlyNameBuilder.Append("...");
                }

                friendlyNameBuilder.Append("\" (");
                friendlyNameBuilder.Append(documentPrettyName);
                friendlyNameBuilder.Append(")");

                CreateNewSearchScope(
                    AppCore.ConvertEntityTypeToSearchResponseType(entityToUseForHeaders),
                    friendlyNameBuilder.ToString(),
                    entities.ToArray());
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private async void QuickFootnotesMenuItem_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                if (!_lastRightClickedParagraph.HasValue)
                {
                    return;
                }

                Guid panelSource = (Guid)((FrameworkElement)sender).Tag;
                if (!_currentReadingPanes.ContainsKey(panelSource))
                {
                    return;
                }

                ReadingPane panel = _currentReadingPanes[panelSource];

                SlowSearchQuery query = new SlowSearchQuery()
                {
                    SearchScopes = new List<KnowledgeGraphNodeId[]>(),
                    IgnoredDocumentScopes = new HashSet<KnowledgeGraphNodeId>(),
                    MaxResults = 50,
                    MinConfidence = 0.1f,
                    CategoryFilters = new ResultFilterSet()
                    {
                        Include_OldTestament = FilterCheckBox_OT.IsChecked.GetValueOrDefault(false),
                        Include_NewTestament = FilterCheckBox_NT.IsChecked.GetValueOrDefault(false),
                        Include_BookOfMormon = FilterCheckBox_BOFM.IsChecked.GetValueOrDefault(false),
                        Include_DC = FilterCheckBox_DC.IsChecked.GetValueOrDefault(false),
                        Include_PearlGP = FilterCheckBox_PGP.IsChecked.GetValueOrDefault(false),
                        Include_BibleDict = FilterCheckBox_BD.IsChecked.GetValueOrDefault(false),
                        Include_GenConference = FilterCheckBox_GC.IsChecked.GetValueOrDefault(false),
                        Include_Books = FilterCheckBox_Books.IsChecked.GetValueOrDefault(false),
                        Include_Hymns = FilterCheckBox_Hymns.IsChecked.GetValueOrDefault(false),
                        Include_Speeches = FilterCheckBox_Speeches.IsChecked.GetValueOrDefault(false),
                    }
                };

                query.SearchScopes.Add(new KnowledgeGraphNodeId[] { _lastRightClickedParagraph.Value });

                // Tell the search to ignore panels we already have open
                foreach (var readingPane in _currentReadingPanes)
                {
                    if (readingPane.Value.CurrentDocumentEntity.HasValue &&
                        !query.IgnoredDocumentScopes.Contains(readingPane.Value.CurrentDocumentEntity.Value))
                    {
                        query.IgnoredDocumentScopes.Add(readingPane.Value.CurrentDocumentEntity.Value);
                    }
                }

                SlowSearchQueryResult searchResults = await Task.Run(() => _core.RunSlowSearchQuery(query)).ConfigureAwait(true);

                string title = _lastRightClickedParagraph.Value.ToString() ?? "NULL";
                if (_lastRightClickedParagraph.Value.Type == KnowledgeGraphNodeType.ScriptureVerse)
                {
                    title = new ScriptureReference(_lastRightClickedParagraph.Value).ToString() ?? "NULL";
                }

                // todo: better formatting of conference talks, etc.

                // We're still on the UI thread so no need to dispatch
                Grid searchResultsPane = await CreateNewSearchResultsPane(searchResults, $"Footnotes: {title}").ConfigureAwait(true);
                if (_currentSearchResultsPane != null)
                {
                    BrowseArea.Children.Remove(_currentSearchResultsPane);
                }

                _currentSearchResultsPane = searchResultsPane;
                BrowseArea.Children.Add(searchResultsPane);
                BrowseArea.UpdateLayout();
                searchResultsPane.BringIntoView();
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private async void DirectScriptureLinkMenuItem_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                ScriptureReference panelSource = (ScriptureReference)((FrameworkElement)sender).Tag;
                await LoadDocumentForEntity(panelSource.ToNodeId());
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void CloseSearchPanelButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                CloseSearchResultsIfPresent();
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void RemoveScopeButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                UIElement elementToRemove = (UIElement)((FrameworkElement)sender).Tag;
                Guid scopeToRemove = (Guid)((FrameworkElement)elementToRemove).Tag;
                _activeSearchScopes.Remove(scopeToRemove);
                ActiveSearchScopes.Children.Remove(elementToRemove);
                //CloseSearchResultsIfPresent();
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void CreateNewSearchScope(SearchResultEntityType entityType, string scopeName, params KnowledgeGraphNodeId[] nodes)
        {
            // Sanity checking
            if (string.IsNullOrEmpty(scopeName))
            {
                _core.CoreLogger.Log("Attempted to create search scope with empty name", LogLevel.Err);
                return;
            }

            if (nodes.Length == 0)
            {
                _core.CoreLogger.Log("Attempted to create search scope with no entities", LogLevel.Err);
                return;
            }

            // Deduplicate node ids
            HashSet<KnowledgeGraphNodeId> nodeIdSet = new HashSet<KnowledgeGraphNodeId>();
            foreach (var node in nodes)
            {
                if (nodeIdSet.Contains(node))
                {
                    _core.CoreLogger.Log($"Duplicate entity {node} in search set; ignoring...", LogLevel.Wrn);
                }
                else
                {
                    nodeIdSet.Add(node);
                }
            }

            // Ensure there's not already an existing scope with the same items
            foreach (var existingScope in _activeSearchScopes.Values)
            {
                if (existingScope.Length == nodeIdSet.Count &&
                    existingScope.All(s => nodeIdSet.Contains(s)))
                {
                    _core.CoreLogger.Log("Attempted to create a search scope that already exists", LogLevel.Wrn);
                    return;
                }
            }

            Guid newScopeGuid = Guid.NewGuid();

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

            _activeSearchScopes.Add(newScopeGuid, nodeIdSet.ToArray());
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
                    return $"{ScriptureMetadata.GetNameForCanon(scriptureDocument.Canon, LanguageCode.ENGLISH)} - {ScriptureMetadata.GetNameForBook(scriptureDocument.Book, LanguageCode.ENGLISH)} - {scriptureDocument.Chapter}";
                case GospelDocumentType.ByuSpeech:
                    ByuSpeechDocument speechDocument = (ByuSpeechDocument)document;
                    return $"BYU Speeches - {speechDocument.Title} - {speechDocument.Speaker}";
                case GospelDocumentType.Hymn:
                    HymnDocument hymnDocument = (HymnDocument)document;
                    return $"Hymns {hymnDocument.SongNum} - {hymnDocument.Title}";
                case GospelDocumentType.GospelBookChapter:
                    BookChapterDocument chapterDocument = (BookChapterDocument)document;
                    string normalBookName = "UNKNOWN_BOOK";
                    if (chapterDocument.BookId.Equals("atgq", StringComparison.OrdinalIgnoreCase))
                        normalBookName = "Answers to Gospel Questions";
                    else if (chapterDocument.BookId.Equals("md", StringComparison.OrdinalIgnoreCase))
                        normalBookName = "Mormon Doctrine";
                    return $"{normalBookName} - {chapterDocument.ChapterName}";
                default:
                    return "UNKNOWN_DOCUMENT";
            }
        }

        private static readonly Regex FORMATTING_HTML_PARSER = new Regex("<(\\/)?([bi])\\s*>");

        private static void AddFormattedInlines(string text, InlineCollection collection)
        {
            // TODO: Remove the "fancy" formatting from documents?
            // Like nbsp and fancy quotes and stuff - they might mess up copy and pasting
            bool italic = false;
            bool bold = false;
            MatchCollection tagMatches = FORMATTING_HTML_PARSER.Matches(text);
            int currentIndex = 0;
            foreach (Match match in tagMatches)
            {
                string substring = text.Substring(currentIndex, match.Index - currentIndex);
                Inline inline = new Run(substring);
                if (bold)
                    inline = new Bold(inline);
                if (italic)
                    inline = new Italic(inline);

                collection.Add(inline);
                currentIndex = match.Index + match.Length;

                if (string.Equals("b", match.Groups[2].Value))
                {
                    bold = !match.Groups[1].Success;
                }
                else if (string.Equals("i", match.Groups[2].Value))
                {
                    italic = !match.Groups[1].Success;
                }
            }

            if (currentIndex < text.Length)
            {
                string substring = text.Substring(currentIndex);
                Inline inline = new Run(substring);
                if (bold)
                    inline = new Bold(inline);
                if (italic)
                    inline = new Italic(inline);

                collection.Add(inline);
            }
        }

        private FlowDocument ConvertDocumentToFlowDocument(GospelDocument inputDoc, Guid targetPane)
        {
            FlowDocument returnVal = new FlowDocument();
            returnVal.Background = (Brush)TryFindResource("DocumentReaderPageBackground");

            KnowledgeGraphNodeId? prevChapter = null;
            KnowledgeGraphNodeId? nextChapter = null;
            if (inputDoc is ScriptureChapterDocument scriptureChapter)
            {
                prevChapter = scriptureChapter.Prev;
                nextChapter = scriptureChapter.Next;
            }
            else if (inputDoc is BookChapterDocument bookChapter)
            {
                prevChapter = bookChapter.Prev;
                nextChapter = bookChapter.Next;
            }

            // Buttons at the top of the document if prev/next chapters are enabled
            if (prevChapter.HasValue || nextChapter.HasValue)
            {
                UniformGrid grid = new UniformGrid();

                Button prevButton = new Button()
                {
                    Content = "Previous",
                    IsEnabled = prevChapter.HasValue,
                };

                Button nextButton = new Button()
                {
                    Content = "Next",
                    IsEnabled = nextChapter.HasValue
                };

                if (prevChapter.HasValue)
                {
                    prevButton.Tag = new Tuple<KnowledgeGraphNodeId, Guid>(prevChapter.Value, targetPane);
                    prevButton.Click += NextPrevChapterButton_Click;
                }

                if (nextChapter.HasValue)
                {
                    nextButton.Tag = new Tuple<KnowledgeGraphNodeId, Guid>(nextChapter.Value, targetPane);
                    nextButton.Click += NextPrevChapterButton_Click;
                }

                grid.Children.Add(prevButton);
                grid.Children.Add(nextButton);

                BlockUIContainer buttonContainer = new BlockUIContainer();
                buttonContainer.Child = grid;
                returnVal.Blocks.Add(buttonContainer);
            }

            // Build blocks for all paragraphs
            bool firstNonTitle = true;
            bool firstPara = true;
            int para = 1;
            foreach (GospelParagraph paragraph in inputDoc.Paragraphs)
            {
                string styleKey = paragraph.Class.ToString();
                Paragraph uiParagraph = new Paragraph();
                uiParagraph.Tag = paragraph.ParagraphEntityId;

                uiParagraph.TextAlignment = (TextAlignment)TryFindResource($"Para_Align_{styleKey}");
                uiParagraph.FontFamily = (FontFamily)TryFindResource($"Para_FontFamily_{styleKey}");
                uiParagraph.Margin = (Thickness)TryFindResource($"Para_Margin_{styleKey}");
                uiParagraph.FontSize = (double)TryFindResource($"Para_FontSize_{styleKey}");

                if (!firstPara &&
                    firstNonTitle &&
                    paragraph.Class != GospelParagraphClass.Header &&
                    paragraph.Class != GospelParagraphClass.SubHeader)
                {
                    // Add a special margin space above the first non-title block
                    // (things like N.T. epistles have titles and subtitles mixed together so those would look weird if we
                    // had a flat large margin space below all titles in general)
                    uiParagraph.Margin = new Thickness(uiParagraph.Margin.Left, uiParagraph.Margin.Top + 20, uiParagraph.Margin.Right, uiParagraph.Margin.Bottom);
                    firstNonTitle = false;
                }

                firstPara = false;

                if (paragraph.Class == GospelParagraphClass.StudySummary)
                {
                    paragraph.Text = $"<i>{paragraph.Text}</i>";
                }

                if (inputDoc is ScriptureChapterDocument &&
                    paragraph.Class == GospelParagraphClass.Verse)
                {
                    Floater verseNumFloater = new Floater();
                    verseNumFloater.Padding = new Thickness(0);
                    verseNumFloater.HorizontalAlignment = HorizontalAlignment.Left;
                    verseNumFloater.FontSize = (double)TryFindResource("VerseNumFontSize");
                    verseNumFloater.Margin = (Thickness)TryFindResource("VerseNumMargin");
                    verseNumFloater.Tag = paragraph.ParagraphEntityId;
                    Paragraph numPara = new Paragraph();
                    numPara.Inlines.Add(para.ToString());
                    numPara.Tag = paragraph.ParagraphEntityId;
                    verseNumFloater.Blocks.Add(numPara);
                    uiParagraph.Inlines.Add(verseNumFloater);
                    para++;
                }

                // TODO small caps for scripture chapter title and subtitles
                AddFormattedInlines(paragraph.Text, uiParagraph.Inlines);
                returnVal.Blocks.Add(uiParagraph);
            }

            if (prevChapter.HasValue || nextChapter.HasValue)
            {
                UniformGrid grid = new UniformGrid();

                Button prevButton = new Button()
                {
                    Content = "Previous",
                    IsEnabled = prevChapter.HasValue,
                };

                Button nextButton = new Button()
                {
                    Content = "Next",
                    IsEnabled = nextChapter.HasValue
                };

                if (prevChapter.HasValue)
                {
                    prevButton.Tag = new Tuple<KnowledgeGraphNodeId, Guid>(prevChapter.Value, targetPane);
                    prevButton.Click += NextPrevChapterButton_Click;
                }

                if (nextChapter.HasValue)
                {
                    nextButton.Tag = new Tuple<KnowledgeGraphNodeId, Guid>(nextChapter.Value, targetPane);
                    nextButton.Click += NextPrevChapterButton_Click;
                }

                grid.Children.Add(prevButton);
                grid.Children.Add(nextButton);

                BlockUIContainer buttonContainer = new BlockUIContainer();
                buttonContainer.Child = grid;
                returnVal.Blocks.Add(buttonContainer);
            }

            return returnVal;
        }

        private async Task CreateUiElementsForSearchResult(
            KnowledgeGraphNodeId searchResult,
            UIElementCollection target,
            Dictionary<KnowledgeGraphNodeId, float> activatedWords)
        {
            try
            {
                if (searchResult.Type == KnowledgeGraphNodeType.ScriptureVerse)
                {
                    GospelDocument scriptureChapter = await _core.LoadDocument(searchResult);
                    if (scriptureChapter is ScriptureChapterDocument scriptureDoc)
                    {
                        CreateUiElementsForScriptureVerseResult(searchResult, scriptureDoc, target);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected ScriptureChapterDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.ConferenceTalkParagraph)
                {
                    GospelDocument scriptureChapter = await _core.LoadDocument(searchResult);
                    if (scriptureChapter is ConferenceTalkDocument conferenceDoc)
                    {
                        CreateUiElementsForConferenceParagraphResult(searchResult, conferenceDoc, target);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected ConferenceTalkDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.ConferenceTalk)
                {
                    GospelDocument scriptureChapter = await _core.LoadDocument(searchResult);
                    if (scriptureChapter is ConferenceTalkDocument conferenceDoc)
                    {
                        TextBlock searchResultLabel = new TextBlock()
                        {
                            Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                            FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                            FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Justify,
                            Padding = new Thickness(5),
                            IsManipulationEnabled = false,
                            Tag = new FastSearchQueryResult()
                            {
                                DisplayName = $"{conferenceDoc.Title}",
                                EntityType = SearchResultEntityType.ConferenceTalk,
                                EntityIds = new KnowledgeGraphNodeId[] { searchResult }
                            },
                            Text = $"{conferenceDoc.Speaker} - {conferenceDoc.Title}"
                        };

                        // Use the kicker as the search preview
                        searchResultLabel.Text = conferenceDoc.Kicker;

                        if (string.IsNullOrEmpty(searchResultLabel.Text))
                        {
                            // If no kicker, see if there's a best match paragraph within the document based on the search query
                            GospelParagraph? bestPara = AppCore.GetBestMatchParagraph(conferenceDoc, activatedWords);
                            if (bestPara != null)
                            {
                                searchResultLabel.Text = bestPara.Text;
                            }
                        }

                        searchResultLabel.Text = searchResultLabel.Text == null ? string.Empty : AppCore.StripHtml(searchResultLabel.Text);
                        searchResultLabel.MouseEnter += SearchResultPreviewDocument_MouseEnter;
                        searchResultLabel.MouseLeave += SearchResultPreviewDocument_MouseLeave;
                        searchResultLabel.MouseDown += SearchResultPreviewDocument_Click;

                        string month = conferenceDoc.Conference.Phase == ConferencePhase.April ? "April" : "October";
                        TextBlock searchResultHeader = CreateSearchResultHeader($"{month} {conferenceDoc.Conference.Year} General Conference - {conferenceDoc.Title} - {conferenceDoc.Speaker}");

                        target.Add(searchResultHeader);
                        target.Add(searchResultLabel);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected ConferenceTalkDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.BibleDictionaryParagraph)
                {
                    GospelDocument dictionaryEntry = await _core.LoadDocument(searchResult);
                    if (dictionaryEntry is BibleDictionaryDocument dictionaryDoc)
                    {
                        CreateUiElementsForBDParagraphResult(searchResult, dictionaryDoc, target);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected BibleDictionaryDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.BibleDictionaryTopic)
                {
                    GospelDocument dictionaryEntry = await _core.LoadDocument(searchResult);
                    if (dictionaryEntry is BibleDictionaryDocument dictionaryDoc)
                    {
                        TextBlock searchResultLabel = new TextBlock()
                        {
                            Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                            FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                            FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Justify,
                            Padding = new Thickness(5),
                            IsManipulationEnabled = false,
                            Tag = new FastSearchQueryResult()
                            {
                                DisplayName = dictionaryDoc.Title,
                                EntityType = SearchResultEntityType.BibleDictionary,
                                EntityIds = new KnowledgeGraphNodeId[] { searchResult }
                            },
                            Text = $"Bible Dictionary - {dictionaryDoc.Title}"
                        };

                        // See if there's a best match paragraph within the document based on the search query
                        GospelParagraph? bestPara = AppCore.GetBestMatchParagraph(dictionaryEntry, activatedWords);
                        if (bestPara != null)
                        {
                            searchResultLabel.Text = bestPara.Text;
                        }

                        searchResultLabel.Text = searchResultLabel.Text == null ? string.Empty : AppCore.StripHtml(searchResultLabel.Text);
                        searchResultLabel.MouseEnter += SearchResultPreviewDocument_MouseEnter;
                        searchResultLabel.MouseLeave += SearchResultPreviewDocument_MouseLeave;
                        searchResultLabel.MouseDown += SearchResultPreviewDocument_Click;

                        TextBlock searchResultHeader = CreateSearchResultHeader($"Bible Dictionary - {dictionaryDoc.Title}");

                        target.Add(searchResultHeader);
                        target.Add(searchResultLabel);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected BibleDictionaryDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.BookChapter)
                {
                    GospelDocument dictionaryEntry = await _core.LoadDocument(searchResult);
                    if (dictionaryEntry is BookChapterDocument chapterDoc)
                    {
                        TextBlock searchResultLabel = new TextBlock()
                        {
                            Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                            FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                            FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Justify,
                            Padding = new Thickness(5),
                            IsManipulationEnabled = false,
                            Tag = new FastSearchQueryResult()
                            {
                                DisplayName = chapterDoc.ChapterName,
                                EntityType = SearchResultEntityType.Topic, // FIXME wrong
                                EntityIds = new KnowledgeGraphNodeId[] { searchResult }
                            },
                            Text = $"{chapterDoc.BookId} - {chapterDoc.ChapterName}"
                        };

                        // See if there's a best match paragraph within the document based on the search query
                        GospelParagraph? bestPara = AppCore.GetBestMatchParagraph(dictionaryEntry, activatedWords);
                        if (bestPara != null)
                        {
                            searchResultLabel.Text = bestPara.Text;
                        }

                        searchResultLabel.Text = searchResultLabel.Text == null ? string.Empty : AppCore.StripHtml(searchResultLabel.Text);
                        searchResultLabel.MouseEnter += SearchResultPreviewDocument_MouseEnter;
                        searchResultLabel.MouseLeave += SearchResultPreviewDocument_MouseLeave;
                        searchResultLabel.MouseDown += SearchResultPreviewDocument_Click;

                        TextBlock searchResultHeader = CreateSearchResultHeader($"{chapterDoc.BookId} - {chapterDoc.ChapterName}");

                        target.Add(searchResultHeader);
                        target.Add(searchResultLabel);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected BookChapterDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.BookParagraph)
                {
                    GospelDocument dictionaryEntry = await _core.LoadDocument(searchResult);
                    if (dictionaryEntry is BookChapterDocument chapterDoc)
                    {
                        CreateUiElementsForBookChapterResult(searchResult, chapterDoc, target);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected BookChapterDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.ByuSpeech)
                {
                    GospelDocument dictionaryEntry = await _core.LoadDocument(searchResult);
                    if (dictionaryEntry is ByuSpeechDocument speechDoc)
                    {
                        TextBlock searchResultLabel = new TextBlock()
                        {
                            Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                            FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                            FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Justify,
                            Padding = new Thickness(5),
                            IsManipulationEnabled = false,
                            Tag = new FastSearchQueryResult()
                            {
                                DisplayName = speechDoc.Title,
                                EntityType = SearchResultEntityType.ByuSpeech,
                                EntityIds = new KnowledgeGraphNodeId[] { searchResult }
                            },
                            Text = $"{speechDoc.Speaker} - {speechDoc.Title}"
                        };

                        // See if there's a best match paragraph within the document based on the search query
                        GospelParagraph? bestPara = AppCore.GetBestMatchParagraph(dictionaryEntry, activatedWords);
                        if (bestPara != null)
                        {
                            searchResultLabel.Text = bestPara.Text;
                        }

                        searchResultLabel.Text = searchResultLabel.Text == null ? string.Empty : AppCore.StripHtml(searchResultLabel.Text);
                        searchResultLabel.MouseEnter += SearchResultPreviewDocument_MouseEnter;
                        searchResultLabel.MouseLeave += SearchResultPreviewDocument_MouseLeave;
                        searchResultLabel.MouseDown += SearchResultPreviewDocument_Click;

                        TextBlock searchResultHeader = CreateSearchResultHeader($"{speechDoc.Speaker} - {speechDoc.Title}");

                        target.Add(searchResultHeader);
                        target.Add(searchResultLabel);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected BookChapterDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.ByuSpeechParagraph)
                {
                    GospelDocument document = await _core.LoadDocument(searchResult);
                    if (document is ByuSpeechDocument speechDoc)
                    {
                        CreateUiElementsForByuSpeechParaResult(searchResult, speechDoc, target);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected BookChapterDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.Hymn)
                {
                    GospelDocument dictionaryEntry = await _core.LoadDocument(searchResult);
                    if (dictionaryEntry is HymnDocument hymnDoc)
                    {
                        TextBlock searchResultLabel = new TextBlock()
                        {
                            Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                            FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                            FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Justify,
                            Padding = new Thickness(5),
                            IsManipulationEnabled = false,
                            Tag = new FastSearchQueryResult()
                            {
                                DisplayName = hymnDoc.Title,
                                EntityType = SearchResultEntityType.Hymn,
                                EntityIds = new KnowledgeGraphNodeId[] { searchResult }
                            },
                            Text = $"Hymns {hymnDoc.SongNum} - {hymnDoc.Title}"
                        };

                        // See if there's a best match paragraph within the document based on the search query
                        GospelParagraph? bestPara = AppCore.GetBestMatchParagraph(dictionaryEntry, activatedWords);
                        if (bestPara != null)
                        {
                            searchResultLabel.Text = bestPara.Text;
                        }

                        searchResultLabel.Text = searchResultLabel.Text == null ? string.Empty : AppCore.StripHtml(searchResultLabel.Text);
                        searchResultLabel.MouseEnter += SearchResultPreviewDocument_MouseEnter;
                        searchResultLabel.MouseLeave += SearchResultPreviewDocument_MouseLeave;
                        searchResultLabel.MouseDown += SearchResultPreviewDocument_Click;

                        TextBlock searchResultHeader = CreateSearchResultHeader($"Hymns {hymnDoc.SongNum} - {hymnDoc.Title}");

                        target.Add(searchResultHeader);
                        target.Add(searchResultLabel);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected HymnDocument");
                    }
                }
                else if (searchResult.Type == KnowledgeGraphNodeType.HymnVerse)
                {
                    GospelDocument document = await _core.LoadDocument(searchResult);
                    if (document is HymnDocument hymnDoc)
                    {
                        CreateUiElementsForHymnVerseResult(searchResult, hymnDoc, target);
                    }
                    else
                    {
                        throw new Exception("Invalid loaded document type: expected HymnDocument");
                    }
                }
                else
                {
                    TextBlock placeholderSearchResult = new TextBlock()
                    {
                        Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                        FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                        FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Justify,
                        Padding = new Thickness(5),
                        IsManipulationEnabled = false,
                        Tag = new FastSearchQueryResult()
                        {
                            DisplayName = "UNKNOWN",
                            EntityType = SearchResultEntityType.Unknown,
                            EntityIds = new KnowledgeGraphNodeId[] { searchResult }
                        },
                        Text = $"Entity type {searchResult.Type} not yet handled"
                    };

                    placeholderSearchResult.MouseEnter += SearchResultPreviewDocument_MouseEnter;
                    placeholderSearchResult.MouseLeave += SearchResultPreviewDocument_MouseLeave;
                    placeholderSearchResult.MouseDown += SearchResultPreviewDocument_Click;
                    TextBlock searchResultHeader = CreateSearchResultHeader(searchResult.ToString() ?? "ERROR");
                    target.Add(searchResultHeader);
                    target.Add(placeholderSearchResult);
                }
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }

        private void CreateUiElementsForScriptureVerseResult(
            KnowledgeGraphNodeId entityId,
            ScriptureChapterDocument chapter,
            UIElementCollection target)
        {
            ScriptureReference parsedRef = new ScriptureReference(entityId);
            if (!parsedRef.Verse.HasValue)
            {
                throw new Exception("Scripture reference has no verse data " + entityId.ToString());
            }

            string? verseLabel = parsedRef.Paragraph;
            if (verseLabel == null && parsedRef.Verse.HasValue)
            {
                verseLabel = parsedRef.Verse.Value.ToString();
            }

            if (verseLabel == null)
            {
                throw new Exception("No verse or paragraph ref present");
            }

            if (!chapter.Paragraphs.Any(s => s.ParagraphEntityId.Equals(entityId)))
            {
                throw new Exception("Verse reference to invalid verse " + entityId.ToString());
            }

            GospelParagraph para = chapter.Paragraphs.First(s => s.ParagraphEntityId.Equals(entityId));
            string text = $"[{parsedRef.Verse.Value}] {AppCore.StripHtml(para.Text)}";

            TextBlock scriptureSearchResult = new TextBlock()
            {
                Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Padding = new Thickness(5),
                IsManipulationEnabled = false,
                Tag = new FastSearchQueryResult()
                {
                    DisplayName = $"{ScriptureMetadata.GetNameForBook(parsedRef.Book, LanguageCode.ENGLISH)} {parsedRef.Chapter.Value}:{parsedRef.Verse.Value}",
                    EntityType = SearchResultEntityType.ScriptureVerse,
                    EntityIds = new KnowledgeGraphNodeId[] { entityId }
                },
                Text = text
            };

            scriptureSearchResult.Text = scriptureSearchResult.Text == null ? string.Empty : AppCore.StripHtml(scriptureSearchResult.Text);
            scriptureSearchResult.MouseEnter += SearchResultPreviewDocument_MouseEnter;
            scriptureSearchResult.MouseLeave += SearchResultPreviewDocument_MouseLeave;
            scriptureSearchResult.MouseDown += SearchResultPreviewDocument_Click;

            TextBlock searchResultHeader = CreateSearchResultHeader($"{ScriptureMetadata.GetNameForBook(chapter.Book, LanguageCode.ENGLISH)} {chapter.Chapter}");
            target.Add(searchResultHeader);
            target.Add(scriptureSearchResult);
        }

        private void CreateUiElementsForConferenceParagraphResult(
            KnowledgeGraphNodeId entityId,
            ConferenceTalkDocument document,
            UIElementCollection target)
        {
            GospelParagraph? targetPara = document.Paragraphs.FirstOrDefault(s => s.ParagraphEntityId.Equals(entityId));
            if (targetPara == null)
            {
                throw new Exception("Verse reference to invalid paragraph " + entityId.ToString());
            }

            int paraNumber = document.Paragraphs.IndexOf(targetPara) + 1;

            TextBlock conferenceTalkResult = new TextBlock()
            {
                Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Default"),
                FontSize = (double)TryFindResource("Para_FontSize_Default"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Padding = new Thickness(5),
                IsManipulationEnabled = false,
                Tag = new FastSearchQueryResult()
                {
                    DisplayName = $"{document.Title} ¶{paraNumber}",
                    EntityType = SearchResultEntityType.ConferenceTalk,
                    EntityIds = new KnowledgeGraphNodeId[] { entityId }
                },
                Text = AppCore.StripHtml(targetPara.Text)
            };

            conferenceTalkResult.MouseEnter += SearchResultPreviewDocument_MouseEnter;
            conferenceTalkResult.MouseLeave += SearchResultPreviewDocument_MouseLeave;
            conferenceTalkResult.MouseDown += SearchResultPreviewDocument_Click;

            string month = document.Conference.Phase == ConferencePhase.April ? "April" : "October";
            TextBlock searchResultHeader = CreateSearchResultHeader($"{month} {document.Conference.Year} General Conference - {document.Title} - {document.Speaker}");

            target.Add(searchResultHeader);
            target.Add(conferenceTalkResult);
        }

        private TextBlock CreateSearchResultHeader(string text)
        {
            return new TextBlock()
            {
                Background = (Brush)TryFindResource("SearchResultLabelBackground"),
                IsManipulationEnabled = false,
                Margin = new Thickness(2),
                Text = text
            };
        }

        private void CreateUiElementsForBDParagraphResult(
            KnowledgeGraphNodeId entityId,
            BibleDictionaryDocument document,
            UIElementCollection target)
        {
            GospelParagraph? targetPara = document.Paragraphs.FirstOrDefault(s => s.ParagraphEntityId.Equals(entityId));
            if (targetPara == null)
            {
                throw new Exception("Verse reference to invalid paragraph " + entityId.ToString());
            }

            TextBlock conferenceTalkResult = new TextBlock()
            {
                Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Padding = new Thickness(5),
                IsManipulationEnabled = false,
                Tag = new FastSearchQueryResult()
                {
                    DisplayName = document.Title,
                    EntityType = SearchResultEntityType.BibleDictionary,
                    EntityIds = new KnowledgeGraphNodeId[] { entityId }
                },
                Text = AppCore.StripHtml(targetPara.Text)
            };

            conferenceTalkResult.MouseEnter += SearchResultPreviewDocument_MouseEnter;
            conferenceTalkResult.MouseLeave += SearchResultPreviewDocument_MouseLeave;
            conferenceTalkResult.MouseDown += SearchResultPreviewDocument_Click;

            TextBlock searchResultHeader = CreateSearchResultHeader($"Bible Dictionary - {document.Title}");

            target.Add(searchResultHeader);
            target.Add(conferenceTalkResult);
        }

        private void CreateUiElementsForBookChapterResult(
            KnowledgeGraphNodeId entityId,
            BookChapterDocument document,
            UIElementCollection target)
        {
            GospelParagraph? targetPara = document.Paragraphs.FirstOrDefault(s => s.ParagraphEntityId.Equals(entityId));
            if (targetPara == null)
            {
                throw new Exception("Verse reference to invalid paragraph " + entityId.ToString());
            }

            TextBlock conferenceTalkResult = new TextBlock()
            {
                Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Padding = new Thickness(5),
                IsManipulationEnabled = false,
                Tag = new FastSearchQueryResult()
                {
                    DisplayName = document.ChapterName,
                    EntityType = SearchResultEntityType.Topic, // FIXME wrong
                    EntityIds = new KnowledgeGraphNodeId[] { entityId }
                },
                Text = AppCore.StripHtml(targetPara.Text)
            };

            conferenceTalkResult.MouseEnter += SearchResultPreviewDocument_MouseEnter;
            conferenceTalkResult.MouseLeave += SearchResultPreviewDocument_MouseLeave;
            conferenceTalkResult.MouseDown += SearchResultPreviewDocument_Click;

            TextBlock searchResultHeader = CreateSearchResultHeader($"{document.BookId} - {document.ChapterName}");

            target.Add(searchResultHeader);
            target.Add(conferenceTalkResult);
        }

        private void CreateUiElementsForByuSpeechParaResult(
            KnowledgeGraphNodeId entityId,
            ByuSpeechDocument document,
            UIElementCollection target)
        {
            GospelParagraph? targetPara = document.Paragraphs.FirstOrDefault(s => s.ParagraphEntityId.Equals(entityId));
            if (targetPara == null)
            {
                throw new Exception("Verse reference to invalid paragraph " + entityId.ToString());
            }

            TextBlock conferenceTalkResult = new TextBlock()
            {
                Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Padding = new Thickness(5),
                IsManipulationEnabled = false,
                Tag = new FastSearchQueryResult()
                {
                    DisplayName = document.Title,
                    EntityType = SearchResultEntityType.ByuSpeech,
                    EntityIds = new KnowledgeGraphNodeId[] { entityId }
                },
                Text = AppCore.StripHtml(targetPara.Text)
            };

            conferenceTalkResult.MouseEnter += SearchResultPreviewDocument_MouseEnter;
            conferenceTalkResult.MouseLeave += SearchResultPreviewDocument_MouseLeave;
            conferenceTalkResult.MouseDown += SearchResultPreviewDocument_Click;

            TextBlock searchResultHeader = CreateSearchResultHeader($"{document.Speaker} - {document.Title}");

            target.Add(searchResultHeader);
            target.Add(conferenceTalkResult);
        }

        private void CreateUiElementsForHymnVerseResult(
            KnowledgeGraphNodeId entityId,
            HymnDocument document,
            UIElementCollection target)
        {
            GospelParagraph? targetPara = document.Paragraphs.FirstOrDefault(s => s.ParagraphEntityId.Equals(entityId));
            if (targetPara == null)
            {
                throw new Exception("Reference to invalid verse " + entityId.ToString());
            }

            TextBlock conferenceTalkResult = new TextBlock()
            {
                Background = (Brush)TryFindResource("DocumentReaderPageBackground"),
                FontFamily = (FontFamily)TryFindResource("Para_FontFamily_Verse"),
                FontSize = (double)TryFindResource("Para_FontSize_Verse"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Padding = new Thickness(5),
                IsManipulationEnabled = false,
                Tag = new FastSearchQueryResult()
                {
                    DisplayName = document.Title,
                    EntityType = SearchResultEntityType.Hymn,
                    EntityIds = new KnowledgeGraphNodeId[] { entityId }
                },
                Text = AppCore.StripHtml(targetPara.Text)
            };

            conferenceTalkResult.MouseEnter += SearchResultPreviewDocument_MouseEnter;
            conferenceTalkResult.MouseLeave += SearchResultPreviewDocument_MouseLeave;
            conferenceTalkResult.MouseDown += SearchResultPreviewDocument_Click;

            TextBlock searchResultHeader = CreateSearchResultHeader($"Hymns {document.SongNum} - {document.Title}");

            target.Add(searchResultHeader);
            target.Add(conferenceTalkResult);
        }

        private void SearchResultPreviewDocument_MouseEnter(object sender, MouseEventArgs args)
        {
            ((TextBlock)sender).Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
        }

        private void SearchResultPreviewDocument_MouseLeave(object sender, MouseEventArgs args)
        {
            ((TextBlock)sender).Background = (Brush)TryFindResource("DocumentReaderPageBackground");
        }

        private async void SearchResultPreviewDocument_Click(object sender, MouseButtonEventArgs args)
        {
            try
            {
                FastSearchQueryResult searchResult = (FastSearchQueryResult)((TextBlock)sender).Tag;
                // When we click on a search result, load the document in a tab
                KnowledgeGraphNodeId entityIdToLoad = searchResult.EntityIds[0];
                await LoadDocumentForEntity(entityIdToLoad);
                // And add it to the search bar
                //CreateNewSearchScope(searchResult.EntityType, searchResult.DisplayName, searchResult.EntityIds);
            }
            catch (Exception e)
            {
                _core.CoreLogger.Log(e);
            }
        }
    }
}