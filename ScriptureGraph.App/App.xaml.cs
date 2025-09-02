using System.Configuration;
using System.Data;
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
            _core = new AppCore();
        }
    }
}
