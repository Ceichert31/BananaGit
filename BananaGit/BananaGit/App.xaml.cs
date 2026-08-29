using System.Configuration;
using System.Data;
using System.Windows;
using Velopack;

namespace BananaGit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                VelopackApp.Build()
                    .OnFirstRun((v) =>
                    {
                        MessageBox.Show(v.Version.ToString(), "Version", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    })
                    .Run();

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unhandled exception: " + ex.ToString());
            }
        }
    }
}