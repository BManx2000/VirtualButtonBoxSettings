using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using GDI = System.Drawing;
using System.Threading;
using System.Windows.Controls;
using System.IO;
using System.Runtime.Serialization.Json;
using System.ComponentModel;

namespace VirtualButtonBoxSettings {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private Profile Profile;
        private ButtonGrid ButtonGrid;
        private GridButton CurrentButton = null;
        private int SelectedX = -1;
        private int SelectedY = -1;

        private BackgroundWorker worker;
        private BackgroundWorker profileWorker;

        public MainWindow() {
            InitializeComponent();

            CreateWorker();
            CreateProfileWorker();

            UpdateDisplay();
            UpdateProfileImage();
            Settings.Instance.save();
        }

        private void CreateWorker() {
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += this.GenerateImage;
            worker.RunWorkerCompleted += this.ShowImage;
        }

        private void CreateProfileWorker() {
            profileWorker = new BackgroundWorker();
            profileWorker.WorkerSupportsCancellation = true;
            profileWorker.DoWork += this.GenerateProfileImage;
        }

        private void UpdateImage() {
            if (this.ButtonGrid == null) {
                this.TextureImage.Source = null;
                return;
            }

            double width = this.ButtonGrid.Width*Settings.PixelDensity;
            double height = this.ButtonGrid.Height*Settings.PixelDensity;
            double scaleFactor = 1024 / width;
            if (768 / height < scaleFactor) {
                scaleFactor = 768 / height;
            }
            
            this.TextureImage.Width = (int)(width*scaleFactor);
            this.TextureImage.Height = (int)(height*scaleFactor);
            this.SelectionCanvas.Width = this.TextureImage.Width;
            this.SelectionCanvas.Height = this.TextureImage.Height;
            this.ReferenceImage.Width = this.TextureImage.Width;
            this.ReferenceImage.Height = this.TextureImage.Height;
            this.GridCanvas.Width = this.TextureImage.Width;
            this.GridCanvas.Height = this.TextureImage.Height;

            this.UpdateBackgroundGridDisplay();

            if (worker.IsBusy) {
                worker.CancelAsync();
                CreateWorker();
            }
            worker.RunWorkerAsync();
        }

        private void UpdateProfileImage() {
            if(profileWorker.IsBusy) {
                profileWorker.CancelAsync();
                CreateProfileWorker();
            }
            profileWorker.RunWorkerAsync();
        }

        private void GenerateImage(object sender, DoWorkEventArgs e) {
            BitmapSource bitmap = this.ButtonGrid.CreateAndSaveImage(this.worker);
            if(bitmap == null) {
                return;
            }
            bitmap.Freeze();
            e.Result = bitmap;
        }

        private void GenerateProfileImage(object sender, DoWorkEventArgs e) {
            Profiles.SaveProfilesTexture(this.profileWorker);
        }

        private void ShowImage(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Result != null) {
                this.TextureImage.Source = (BitmapSource)e.Result;
            }
        }

        private void ImageClicked(object sender, MouseButtonEventArgs e) {
            if(this.ButtonGrid == null) {
                return;
            }
            Point position = e.GetPosition(TextureImage);

            int x = ((int)position.X) * this.ButtonGrid.GridWidth / (int)TextureImage.Width;
            int y = ((int)position.Y) * this.ButtonGrid.GridHeight / (int)TextureImage.Height;
            this.SelectedX = x;
            this.SelectedY = y;
            this.CurrentButton = this.ButtonGrid.ButtonAtPosition(x, y);

            this.UpdateDisplay();
        }

        private void UpdateDisplayAndImage() {
            this.UpdateDisplay();
            this.UpdateImage();
        }

        private void UpdateDisplay() {
            if (this.ButtonGrid == null || this.SelectedX >= this.ButtonGrid.GridWidth || this.SelectedY >= this.ButtonGrid.GridHeight) {
                this.CurrentButton = null;
                this.SelectedX = -1;
                this.SelectedY = -1;
            }

            this.UpdateGridDisplay();
            this.UpdateButtonDisplay();
            this.UpdateSelectionDisplay();
        }

        private void UpdateBackgroundGridDisplay() {
            this.GridCanvas.Children.Clear();
            if(this.ButtonGrid != null) {
                var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 255));
                
                for(int i=1; i<this.ButtonGrid.GridWidth; i++) {
                    var line = new System.Windows.Shapes.Line();
                    line.Stroke = brush;
                    //line.Width = 1;
                    double x = (this.GridCanvas.Width * i) / this.ButtonGrid.GridWidth;
                    line.X1 = x;
                    line.Y1 = 0;
                    line.X2 = x;
                    line.Y2 = this.GridCanvas.Height;
                    this.GridCanvas.Children.Add(line);
                }
                for (int i = 1; i < this.ButtonGrid.GridHeight; i++) {
                    var line = new System.Windows.Shapes.Line();
                    line.Stroke = brush;
                    //line.Width = 1;
                    double y = (this.GridCanvas.Height * i) / this.ButtonGrid.GridHeight;
                    line.X1 = 0;
                    line.Y1 = y;
                    line.X2 = this.GridCanvas.Width;
                    line.Y2 = y;
                    this.GridCanvas.Children.Add(line);
                }
            }
        }

        private void UpdateGridDisplay() {
            this.ProfileNameBox.Items.Clear();
            foreach(Profile profile in Profiles.profiles) {
                this.ProfileNameBox.Items.Add(profile);
            }
            this.ProfileNameBox.SelectedItem = this.Profile;

            this.GridNameBox.Items.Clear();
            if (this.Profile != null) {
                foreach (ButtonGrid grid in this.Profile.grids) {
                    this.GridNameBox.Items.Add(grid);
                }
                this.GridNameBox.SelectedItem = this.ButtonGrid;

                this.ProfileHideControls.IsEnabled = true;
                this.HidePointerCheckbox.IsChecked = this.Profile.HidePointer;
            }
            else {
                this.ProfileHideControls.IsEnabled = false;
            }

            this.ProfileControls.IsEnabled = this.Profile != null;
            this.ProfileUpButton.IsEnabled = this.Profile != null;
            this.ProfileDownButton.IsEnabled = this.Profile != null;
            this.DeleteProfileButton.IsEnabled = this.Profile != null;
            this.GridControls.IsEnabled = this.ButtonGrid != null;
            this.DeleteGridButton.IsEnabled = this.ButtonGrid != null;
            this.ButtonControls.IsEnabled = this.CurrentButton != null;
            this.DeleteButtonButton.IsEnabled = this.CurrentButton != null;
            this.NewButtonButton.IsEnabled = this.CurrentButton == null && this.SelectedX >= 0 && this.SelectedY >= 0;

            if (this.ButtonGrid == null) {
                this.WidthBox.Text = null;
                this.HeightBox.Text = null;
                this.GridWidthBox.Text = null;
                this.GridHeightBox.Text = null;
                this.XBox.Text = null;
                this.YBox.Text = null;
                this.PitchBox.Text = null;
                this.YawBox.Text = null;
                this.RollBox.Text = null;
                this.AlphaBox.Text = null;
                this.OutlineBox.IsChecked = false;
                this.BorderBox.IsChecked = false;
                this.LockPosition.IsChecked = false;
            }
            else {
                this.WidthBox.Text = this.ButtonGrid.Width.ToString("0.####");
                this.HeightBox.Text = this.ButtonGrid.Height.ToString("0.####");

                this.GridWidthBox.Text = this.ButtonGrid.GridWidth.ToString();
                this.GridHeightBox.Text = this.ButtonGrid.GridHeight.ToString();

                this.XBox.Text = this.ButtonGrid.X.ToString("0.####");
                this.YBox.Text = this.ButtonGrid.Y.ToString("0.####");
                this.ZBox.Text = this.ButtonGrid.Z.ToString("0.####");

                this.PitchBox.Text = this.ButtonGrid.Pitch.ToString("0.##");
                this.YawBox.Text = this.ButtonGrid.Yaw.ToString("0.##");
                this.RollBox.Text = this.ButtonGrid.Roll.ToString("0.##");

                this.AlphaBox.Text = this.ButtonGrid.Alpha.ToString("0.##");
                this.OutlineBox.IsChecked = this.ButtonGrid.Outline;
                this.BorderBox.IsChecked = this.ButtonGrid.Border;
                this.LockPosition.IsChecked = this.ButtonGrid.Locked;
            }
        }

        private void UpdateButtonDisplay() {
            if(this.CurrentButton == null) {
                this.ButtonXBox.Text = null;
                this.ButtonYBox.Text = null;
                this.ButtonWidthBox.Text = null;
                this.ButtonHeightBox.Text = null;
                this.FontSizeBox.Text = null;
                this.ButtonTextBox.Text = null;
                this.ButtonTypeBox.SelectedIndex = -1;
                this.KeypressLabel.Text = "No Keypress";
                this.DegreesBox.Text = null;
                this.DefaultPositionBox.Text = null;

                this.NormalButtonControls.Visibility = Visibility.Visible;
                this.RotaryControls.Visibility = Visibility.Collapsed;
                this.TwoRotaryControls.Visibility = Visibility.Collapsed;
                this.MultiRotaryControls.Visibility = Visibility.Collapsed;
                this.ThreeSwitchControls.Visibility = Visibility.Collapsed;
            }
            else {
                this.ButtonXBox.Text = this.CurrentButton.X.ToString();
                this.ButtonYBox.Text = this.CurrentButton.Y.ToString(); ;
                this.ButtonWidthBox.Text = this.CurrentButton.Width.ToString();
                this.ButtonHeightBox.Text = this.CurrentButton.Height.ToString();
                this.FontSizeBox.Text = (this.CurrentButton.FontSize*100).ToString("0.##");
                this.ButtonTextBox.Text = this.CurrentButton.Text;
                this.ButtonTypeBox.SelectedIndex = (int)this.CurrentButton.ButtonType;
                this.DegreesBox.Text = this.CurrentButton.RotaryAngle.ToString("0.##");
                this.DefaultPositionBox.Text = (this.CurrentButton.DefaultKeypress + 1).ToString();

                if(this.CurrentButton.Keypress == null) {
                    this.KeypressLabel.Text = "No Keypress";
                    this.KeypressButton.Content = "Set Key";
                    this.MidKeypressLabel.Text = "No Keypress";
                    this.MidKeypressButton.Content = "Set Mid Key";
                }
                else {
                    this.KeypressLabel.Text = this.CurrentButton.Keypress.ToString();
                    this.KeypressButton.Content = "Clear Key";
                    this.MidKeypressLabel.Text = this.CurrentButton.Keypress.ToString();;
                    this.MidKeypressButton.Content = "Clear Mid Key";
                }

                if (this.CurrentButton.CWKeypress == null) {
                    this.CWKeypressLabel.Text = "No Keypress";
                    this.CWKeypressButton.Content = "Set CW Key";
                    this.UpKeypressLabel.Text = "No Keypress";
                    this.UpKeypressButton.Content = "Set Up Key";
                }
                else {
                    this.CWKeypressLabel.Text = this.CurrentButton.CWKeypress.ToString();
                    this.CWKeypressButton.Content = "Clear CW Key";
                    this.UpKeypressLabel.Text = this.CurrentButton.CWKeypress.ToString(); ;
                    this.UpKeypressButton.Content = "Clear Up Key";
                }

                if (this.CurrentButton.CCWKeypress == null) {
                    this.CCWKeypressLabel.Text = "No Keypress";
                    this.CCWKeypressButton.Content = "Set CCW Key";
                    this.DownKeypressLabel.Text = "No Keypress";
                    this.DownKeypressButton.Content = "Set Down Key";
                }
                else {
                    this.CCWKeypressLabel.Text = this.CurrentButton.CCWKeypress.ToString();
                    this.CCWKeypressButton.Content = "Clear CCW Key";
                    this.DownKeypressLabel.Text = this.CurrentButton.CCWKeypress.ToString(); ;
                    this.DownKeypressButton.Content = "Clear Down Key";
                }

                this.NormalButtonControls.Visibility = this.CurrentButton.ButtonType == ButtonType.Normal ? Visibility.Visible : Visibility.Collapsed;
                this.RotaryControls.Visibility = (this.CurrentButton.ButtonType == ButtonType.TwoDirectionRotary || this.CurrentButton.ButtonType == ButtonType.MultiPositionRotary) ? Visibility.Visible : Visibility.Collapsed;
                this.TwoRotaryControls.Visibility = this.CurrentButton.ButtonType == ButtonType.TwoDirectionRotary ? Visibility.Visible : Visibility.Collapsed;
                this.MultiRotaryControls.Visibility = this.CurrentButton.ButtonType == ButtonType.MultiPositionRotary ? Visibility.Visible : Visibility.Collapsed;
                this.ThreeSwitchControls.Visibility = this.CurrentButton.ButtonType == ButtonType.ThreeWaySwitch ? Visibility.Visible : Visibility.Collapsed;
                this.RotaryMinusButton.IsEnabled = this.CurrentButton.MultiKeypresses.Count > 2;

                this.MultiRotaryContainer.Children.Clear();
                this.MultiRotaryContainer.RowDefinitions.Clear();
                for(int i=0; i<this.CurrentButton.MultiKeypresses.Count; i++) {
                    KeyCombo keypress = this.CurrentButton.MultiKeypresses[i];

                    this.MultiRotaryContainer.RowDefinitions.Add(new RowDefinition());
                    TextBlock label = new TextBlock();
                    label.Text = keypress == null ? "No Keypress" : keypress.ToString();
                    label.HorizontalAlignment = HorizontalAlignment.Stretch;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.TextAlignment = TextAlignment.Right;
                    label.TextWrapping = TextWrapping.Wrap;
                    label.Margin = new Thickness(0, 5, 5, 5);
                    Grid.SetRow(label, i);
                    Grid.SetColumn(label, 0);
                    this.MultiRotaryContainer.Children.Add(label);

                    Button button = new Button();
                    button.Tag = i;
                    button.Margin = new Thickness(5);
                    button.Content = keypress == null ? "Set Key " + (i + 1) : "Clear Key " + (i + 1);
                    button.HorizontalAlignment = HorizontalAlignment.Stretch;
                    button.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetRow(button, i);
                    Grid.SetColumn(button, 1);
                    button.AddHandler(Button.ClickEvent, new RoutedEventHandler(MultiKeypressSet));
                    this.MultiRotaryContainer.Children.Add(button);
                }
            }
        }

        private void UpdateSelectionDisplay() {
            if(this.SelectedX >= 0 && this.SelectedY >= 0) {
                this.SelectionRectangle.Visibility = Visibility.Visible;
                int x = 0;
                int y = 0;
                double width = 1;
                double height = 1;
                if(this.CurrentButton == null) {
                    x = ((int)this.TextureImage.Width) * this.SelectedX / this.ButtonGrid.GridWidth;
                    y = ((int)this.TextureImage.Height) * this.SelectedY / this.ButtonGrid.GridHeight;
                    width = Math.Ceiling((this.TextureImage.Width) / this.ButtonGrid.GridWidth);
                    height = Math.Ceiling((this.TextureImage.Height) / this.ButtonGrid.GridHeight);
                }
                else {
                    x = ((int)this.TextureImage.Width) * this.CurrentButton.X / this.ButtonGrid.GridWidth;
                    y = ((int)this.TextureImage.Height) * this.CurrentButton.Y / this.ButtonGrid.GridHeight;
                    width = Math.Ceiling((this.TextureImage.Width) * this.CurrentButton.Width / this.ButtonGrid.GridWidth);
                    height = Math.Ceiling((this.TextureImage.Height) * this.CurrentButton.Height / this.ButtonGrid.GridHeight);
                }
                Canvas.SetLeft(this.SelectionRectangle, x);
                Canvas.SetTop(this.SelectionRectangle, y);
                this.SelectionRectangle.Width = width;
                this.SelectionRectangle.Height = height;
            }
            else {
                this.SelectionRectangle.Visibility = Visibility.Hidden;
            }
        }

        private void WidthEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.WidthDone(sender, e);
            }
        }

        private void WidthDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Width = double.Parse(WidthBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void HeightEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.HeightDone(sender, e);
            }
        }

        private void HeightDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Height = double.Parse(HeightBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void GridWidthEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.GridWidthDone(sender, e);
            }
        }

        private void GridWidthDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.GridWidth = int.Parse(GridWidthBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void GridHeightEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.GridHeightDone(sender, e);
            }
        }

        private void GridHeightDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.GridHeight = int.Parse(GridHeightBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void XEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.XDone(sender, e);
            }
        }

        private void XDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.X = double.Parse(XBox.Text);
            }
            catch(FormatException ex) { }
        }

        private void YEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.YDone(sender, e);
            }
        }

        private void YDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Y = double.Parse(YBox.Text);
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void ZEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.ZDone(sender, e);
            }
        }

        private void ZDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Z = double.Parse(ZBox.Text);
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void PitchEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.PitchDone(sender, e);
            }
        }

        private void PitchDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Pitch = double.Parse(PitchBox.Text);
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void YawEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.YawDone(sender, e);
            }
        }

        private void YawDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Yaw = double.Parse(YawBox.Text);
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void RollEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.RollDone(sender, e);
            }
        }

        private void RollDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Roll = double.Parse(RollBox.Text);
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void AlphaEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.AlphaDone(sender, e);
            }
        }

        private void AlphaDone(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            try {
                this.ButtonGrid.Alpha = double.Parse(AlphaBox.Text);
            }
            catch (FormatException ex) { }
            UpdateDisplay();
        }

        private void OutlineChecked(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            this.ButtonGrid.Outline = (bool)this.OutlineBox.IsChecked;
            UpdateImage();
        }

        private void BorderChecked(object sender, RoutedEventArgs e) {
            if (this.ButtonGrid == null) { return; }
            this.ButtonGrid.Border = (bool)this.BorderBox.IsChecked;
            UpdateImage();
        }

        private void LockPositionChecked(object sender, RoutedEventArgs e) {
            if(this.ButtonGrid == null) { return; }
            this.ButtonGrid.Locked = (bool)this.LockPosition.IsChecked;
        }

        private void ButtonXEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.ButtonXDone(sender, e);
            }
        }

        private void ButtonXDone(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.X = int.Parse(ButtonXBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void ButtonYEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.ButtonYDone(sender, e);
            }
        }

        private void ButtonYDone(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.Y = int.Parse(ButtonYBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void ButtonWidthEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.ButtonWidthDone(sender, e);
            }
        }

        private void ButtonWidthDone(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.Width = int.Parse(ButtonWidthBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void ButtonHeightEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.ButtonHeightDone(sender, e);
            }
        }

        private void ButtonHeightDone(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.Height = int.Parse(ButtonHeightBox.Text);
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void FontSizeEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.FontSizeDone(sender, e);
            }
        }

        private void FontSizeDone(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.FontSize = double.Parse(FontSizeBox.Text)/100;
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void ButtonTextEnter(object sender, KeyEventArgs e) {
            /*if (e.Key == Key.Enter) {
                this.ButtonTextDone(sender, e);
            }*/
        }

        private void ButtonTextDone(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.Text = ButtonTextBox.Text;
                UpdateImage();
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private KeyCombo GetKeyCombo(Button button) {
            KeyboardInput.Instance.StartKeyboardInput();
            var dialog = new KeyDialog();
            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Show();
            button.IsEnabled = false;
            KeyCombo keypress = KeyboardInput.Instance.GetNextKey();
            dialog.Close();
            KeyboardInput.Instance.StopKeyboardInput();
            button.IsEnabled = true;
            return keypress;
        }

        private void SetKeypress(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            if (this.CurrentButton.Keypress == null) {
                this.CurrentButton.Keypress = GetKeyCombo(this.KeypressButton);
            }
            else {
                this.CurrentButton.Keypress = null;
            }
            UpdateDisplay();
        }

        private void NewButton(object sender, RoutedEventArgs e) {
            if(this.CurrentButton != null || this.SelectedX < 0 || this.SelectedY < 0) {
                return;
            }

            this.CurrentButton = this.ButtonGrid.NewButton(this.SelectedX, this.SelectedY);
            UpdateDisplayAndImage();
        }

        private void DeleteButton(object sender, RoutedEventArgs e) {
            if(this.CurrentButton == null) {
                return;
            }
            MessageBoxResult result = MessageBox.Show("Delete this button?", "Confirm", MessageBoxButton.YesNo);
            if(result == MessageBoxResult.Yes) {
                var temp = this.CurrentButton;
                this.CurrentButton = null;
                this.SelectedX = -1;
                this.SelectedY = -1;
                this.ButtonGrid.DeleteButton(temp);
                UpdateDisplayAndImage();
            }
        }

        private void GridSelected(object sender, EventArgs e) {
            this.ButtonGrid = (ButtonGrid)GridNameBox.SelectedItem;
            this.CurrentButton = null;
            this.SelectedX = -1;
            this.SelectedY = -1;
            UpdateDisplayAndImage();
        }

        private void GridNameEnter(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter) {
                GridNameDone(sender, e);
            }
        }

        private void GridNameDone(object sender, RoutedEventArgs e) {
            if(this.ButtonGrid == null) {
                return;
            }
            this.ButtonGrid.Name = GridNameBox.Text;
            UpdateDisplay();
        }

        private void NewGrid(object sender, RoutedEventArgs e) {
            if(this.Profile == null) {
                return;
            }
            this.CurrentButton = null;
            this.SelectedX = -1;
            this.SelectedY = -1;
            this.ButtonGrid = this.Profile.NewGrid();
            UpdateDisplayAndImage();
        }

        private void DeleteGrid(object sender, RoutedEventArgs e) {
            if(this.ButtonGrid == null || this.Profile == null) {
                return;
            }
            MessageBoxResult result = MessageBox.Show("Delete this button grid?", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) {
                var temp = this.ButtonGrid;
                this.ButtonGrid = null;
                this.CurrentButton = null;
                this.SelectedX = -1;
                this.SelectedY = -1;
                this.Profile.DeleteGrid(temp);
                UpdateDisplayAndImage();
            }
        }

        private void ProfileSelected(object sender, EventArgs e) {
            if (ProfileNameBox.SelectedItem != this.Profile) {
                this.Profile = (Profile)ProfileNameBox.SelectedItem;
                this.ButtonGrid = null;
                this.CurrentButton = null;
                this.SelectedX = -1;
                this.SelectedY = -1;
                if(this.Profile.grids.Count > 0) {
                    this.ButtonGrid = this.Profile.grids[0];
                }
                UpdateDisplayAndImage();
            }
        }

        private void ProfileNameEnter(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ProfileNameDone(sender, e);
            }
        }

        private void ProfileNameDone(object sender, RoutedEventArgs e) {
            if(this.Profile == null) {
                return;
            }
            this.Profile.Name = ProfileNameBox.Text;
            UpdateDisplay();
            UpdateProfileImage();
        }

        private void NewProfile(object sender, RoutedEventArgs e) {
            this.ButtonGrid = null;
            this.CurrentButton = null;
            this.SelectedX = -1;
            this.SelectedY = -1;
            this.Profile = Profiles.NewProfile();
            UpdateDisplayAndImage();
            UpdateProfileImage();
        }

        private void DeleteProfile(object sender, RoutedEventArgs e) {
            if (this.Profile == null) {
                return;
            }

            MessageBoxResult result = MessageBox.Show("Delete this profile?", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) {
                var temp = this.Profile;
                this.Profile = null;
                this.ButtonGrid = null;
                this.CurrentButton = null;
                this.SelectedX = -1;
                this.SelectedY = -1;
                Profiles.DeleteProfile(temp);
                UpdateDisplayAndImage();
                UpdateProfileImage();
            }
        }

        private void ProfileUp(object sender, RoutedEventArgs e) {
            if (this.Profile == null) {
                return;
            }
            Profiles.MoveProfileUp(this.Profile);
            UpdateDisplay();
            UpdateProfileImage();
        }

        private void ProfileDown(object sender, RoutedEventArgs e) {
            if (this.Profile == null) {
                return;
            }
            Profiles.MoveProfileDown(this.Profile);
            UpdateDisplay();
            UpdateProfileImage();
        }

        private void ButtonTypeSelected(object sender, EventArgs e) {
            if(this.CurrentButton == null) { return; }
            ButtonType type = (ButtonType)ButtonTypeBox.SelectedIndex;
            this.CurrentButton.ButtonType = type;
            UpdateDisplayAndImage();
        }

        private void DegreesEnter(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter) {
                DegreesDone(sender, e);
            }
        }

        private void DegreesDone(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.RotaryAngle = double.Parse(DegreesBox.Text);
            }
            catch(FormatException ex) { }
            UpdateDisplay();
        }

        private void CWSetKeypress(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            if (this.CurrentButton.CWKeypress == null) {
                this.CurrentButton.CWKeypress = GetKeyCombo(this.CWKeypressButton);
            }
            else {
                this.CurrentButton.CWKeypress = null;
            }
            UpdateDisplay();
        }

        private void CCWSetKeypress(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            if (this.CurrentButton.CCWKeypress == null) {
                this.CurrentButton.CCWKeypress = GetKeyCombo(this.CCWKeypressButton);
            }
            else {
                this.CurrentButton.CCWKeypress = null;
            }
            UpdateDisplay();
        }

        private void DefaultPositionEnter(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter) {
                DefaultPositionDone(sender, e);
            }
        }

        private void DefaultPositionDone(object sender, RoutedEventArgs e) {
            if(this.CurrentButton == null) { return; }
            try {
                this.CurrentButton.DefaultKeypress = int.Parse(this.DefaultPositionBox.Text) - 1;
            }
            catch (FormatException ex) { }
            UpdateDisplay();
        }

        private void RotaryPlus(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            this.CurrentButton.AddKeypress();
            UpdateDisplay();
        }

        private void RotaryMinus(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            this.CurrentButton.RemoveKeypress();
            UpdateDisplay();
        }

        private void MultiKeypressSet(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            int index = (int)((Button)sender).Tag;
            if (this.CurrentButton.MultiKeypresses[index] == null) {
                this.CurrentButton.SetMultiKeypress(index, GetKeyCombo((Button)sender));
            }
            else {
                this.CurrentButton.SetMultiKeypress(index, null);
            }
            UpdateDisplay();
        }

        private void OpenReference(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            var result = dialog.ShowDialog();
            if(result == true) {
                try {
                    BitmapImage image = new BitmapImage(new Uri(dialog.FileName));
                    if (image != null) {
                        this.ReferenceImage.Source = image;
                    }
                }
                catch(Exception ex) { }
            }
        }

        private void ClearReference(object sender, RoutedEventArgs e) {
            this.ReferenceImage.Source = null;
        }

        private void ToggleGrid(object sender, RoutedEventArgs e) {
            this.GridCanvas.Visibility = this.GridCanvas.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void HidePointerChecked(object sender, RoutedEventArgs e) {
            if(this.Profile == null) { return; }
            this.Profile.HidePointer = (bool)HidePointerCheckbox.IsChecked;
        }

        private void SetUpKeypress(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            if (this.CurrentButton.CWKeypress == null) {
                this.CurrentButton.CWKeypress = GetKeyCombo(this.UpKeypressButton);
            }
            else {
                this.CurrentButton.CWKeypress = null;
            }
            UpdateDisplay();
        }

        private void SetMidKeypress(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            if (this.CurrentButton.Keypress == null) {
                this.CurrentButton.Keypress = GetKeyCombo(this.MidKeypressButton);
            }
            else {
                this.CurrentButton.Keypress = null;
            }
            UpdateDisplay();
        }

        private void SetDownKeypress(object sender, RoutedEventArgs e) {
            if (this.CurrentButton == null) { return; }
            if (this.CurrentButton.CCWKeypress == null) {
                this.CurrentButton.CCWKeypress = GetKeyCombo(this.DownKeypressButton);
            }
            else {
                this.CurrentButton.CCWKeypress = null;
            }
            UpdateDisplay();
        }
    }
}
