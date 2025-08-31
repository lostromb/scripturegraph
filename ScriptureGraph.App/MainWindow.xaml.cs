using Durandal.Common.Utils;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Training;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }

        private FlowDocument ConvertDocumentToFlowDocument(GospelDocument inputDoc)
        {
            FlowDocument returnVal = new FlowDocument();
            returnVal.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
            bool displayVerses = false;

            ScriptureChapterDocument? scriptureChapter = inputDoc as ScriptureChapterDocument;
            BibleDictionaryDocument? bibleDictEntry = inputDoc as BibleDictionaryDocument;

            // Build header
            if (inputDoc is ScriptureChapterDocument)
            {
                scriptureChapter.AssertNonNull(nameof(scriptureChapter));
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
            else if (inputDoc is BibleDictionaryDocument)
            {
                bibleDictEntry.AssertNonNull(nameof(bibleDictEntry));
                Section headerSection = new Section();
                Paragraph bookTitle = new Paragraph();
                bookTitle.TextAlignment = TextAlignment.Center;
                bookTitle.Inlines.Add(bibleDictEntry.Title);
                headerSection.Blocks.Add(bookTitle);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //using (FileStream fileIn = new FileStream(@"D:\Code\scripturegraph\runtime\documents\bofm\alma-32.json", FileMode.Open, FileAccess.Read))
            using (FileStream fileIn = new FileStream(@"D:\Code\scripturegraph\runtime\documents\bd\prayer.json", FileMode.Open, FileAccess.Read))
            {
                ReadingPane2.Document = ConvertDocumentToFlowDocument(GospelDocument.ParsePolymorphic(fileIn));
            }
            
            //TextSelection s = ReadingPane1.Selection;
            //s.GetHashCode();
        }
    }
}