using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace XdocViewer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            // handle command-line arguments:
            var file = new System.Text.StringBuilder();
            for (int i = 0; i < e.Args.Length; i++)
            {
                if (file.Length > 0)
                    file.Append(' ');
                file.Append(e.Args[i]);
            }

            var wnd = new MainWindow();
            wnd.Show();

            if (System.IO.File.Exists(file.ToString()))
            {
                wnd.ViewModel.OnStartupOpenSupportedFile(file.ToString());
            }

        }
    }
}
