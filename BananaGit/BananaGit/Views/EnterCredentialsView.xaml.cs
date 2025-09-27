using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BananaGit.Utilities;
using BananaGit.ViewModels;

namespace BananaGit.Views
{
    /// <summary>
    /// Interaction logic for EnterCredentialsView.xaml
    /// </summary>
    public partial class EnterCredentialsView : Window
    {
        private EnterCredentialsViewModel enterCredentialsVM = new();

        public EnterCredentialsView()
        {
            InitializeComponent();

            DataContext = enterCredentialsVM;
        }
    }
}
