using Org.BouncyCastle.Asn1.Cmp;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace ScriptureGraph.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal AppCore _core;

        public App()
        {
            try
            {
                _core = new AppCore();
            }
            catch (DirectoryNotFoundException e)
            {
                MessageBox.Show($"Could not initialize database. Check that the content directory exists. {e.Message}", "Error");
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Could not start program because of unknown error. {e.Message}", "Error");
                Environment.Exit(-2);
            }
        }
    }
}
