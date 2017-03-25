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

namespace VirtualButtonBoxSettings {
    /// <summary>
    /// Interaction logic for ProfileSettings.xaml
    /// </summary>
    public partial class ProfileSettings : Window {
        private Profile profile;

        public ProfileSettings(Profile profile) {
            InitializeComponent();
            this.profile = profile;

            this.HidePointer.IsChecked = profile.HidePointer;
            this.PointerAlphaBox.Text = profile.PointerAlpha.ToString("0.##");
            this.ControllerAlphaBox.Text = profile.ControllerAlpha.ToString("0.##");
        }

        private void OKClicked(object sender, RoutedEventArgs e) {
            this.profile.HidePointer = (bool)this.HidePointer.IsChecked;

            try {
                this.profile.PointerAlpha = double.Parse(this.PointerAlphaBox.Text);
            }
            catch(FormatException ex) { }

            try {
                this.profile.ControllerAlpha = double.Parse(this.ControllerAlphaBox.Text);
            }
            catch (FormatException ex) { }

            this.Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
