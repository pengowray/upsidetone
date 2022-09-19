using MorseKeyer.Sound;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MorseKeyer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        Sounder Sounder;

        public MainWindow() {
            InitializeComponent();
            Sounder = new Sounder();
            Sounder.Enable();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            //todo: delete me
            //Sounder.StraightKeyDown();
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e) {
            Sounder.StraightKeyDown();
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e) {
            Sounder.StraightKeyUp();
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e) {
            // stop it getting stuck
            Sounder.StraightKeyUp();
        }
    }
}
