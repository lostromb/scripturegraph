using System.Text;
using System.Windows;
using System.Windows.Controls;
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

        private void ConvertDocumentToFlowDocument(StackPanel target)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TextSelection s = ReadingPane1.Selection;
            s.GetHashCode();
        }
    }
}