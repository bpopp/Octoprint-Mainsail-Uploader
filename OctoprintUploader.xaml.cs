using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace OctoUploader 
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OctoprintUploader : Window
    {

        public delegate void ChangeHandler ();

        public event ChangeHandler Changed;
        ObservableCollection<OctoPrinter> PrinterList { get; set; }
        OctoPrinter currentItem = null;


        protected virtual void OnChanged()
        {
            Changed?.Invoke();
        }

        public OctoprintUploader()
        {
            InitializeComponent();


            // init properties
            this.removeUploads.IsChecked = Properties.Settings.Default.DeleteOnUpload;
            this.autoStart.IsChecked = Properties.Settings.Default.AutoStart;
            this.relaunchOnStartup.IsChecked = Properties.Settings.Default.RelaunchOnStartup;

            PrinterList = OctoPrinter.LoadFromSettings();            
            this.serverList.ItemsSource = PrinterList;

        }

        public delegate void UpdateTextCallback(String message);
        /*private void UpdateText(string message)
        {
            resultBox.Text = message;
        }*/


        private void watchButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            Properties.Settings.Default.DeleteOnUpload = (bool)this.removeUploads.IsChecked;
            Properties.Settings.Default.AutoStart = (bool)this.autoStart.IsChecked;
            Properties.Settings.Default.RelaunchOnStartup = (bool)this.relaunchOnStartup.IsChecked;

            // clear existing (if any)
            Properties.Settings.Default.ServerAddresses.Clear();
            Properties.Settings.Default.APIKeys.Clear();
            Properties.Settings.Default.WatchLocations.Clear();

            foreach ( OctoPrinter printer in this.PrinterList )
            {
                Properties.Settings.Default.ServerAddresses.Add(printer.ServerAddress);
                Properties.Settings.Default.APIKeys.Add(printer.APIKey);
                Properties.Settings.Default.WatchLocations.Add(printer.WatchFolder);
            }

            Properties.Settings.Default.Save();

            if ( this.relaunchOnStartup.IsChecked == true )
            {
                rkApp.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName, System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else if ( rkApp.GetValue (System.Diagnostics.Process.GetCurrentProcess().ProcessName) != null  )
            {
                rkApp.DeleteValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            }

            OnChanged();        

            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {

            base.OnClosing(e);
        }

        private void folderSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.watchFolder.Text = dialog.FileName;
                this.watchFolder.Focus();
                Keyboard.ClearFocus();
            }
        }

        private void checkButton_click(object sender, RoutedEventArgs e)
        {
            OctoprintAPI api = new OctoprintAPI();
            api.serverAddress = this.serverAddress.Text;
            api.apiKey = this.apiKey.Text;

            String version = api.GetVersion();
            switch ( api.lastResultCode )
            {
                case System.Net.HttpStatusCode.OK:
                    lblWarning.Content = "Successfully connected.";
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    lblWarning.Content = "Invalid API Code.";
                    break;
                default:
                    lblWarning.Content = "Could not connect.";
                    break;
            }
        }

        private void addButton_click(object sender, RoutedEventArgs e)
        {
            currentItem = new OctoPrinter() { WatchFolder = "C:\\Octopi" };
            this.PrinterList.Add ( currentItem );
            this.serverList.SelectedItem = currentItem;
            this.serverAddress.Focus();
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            if (serverList.SelectedItem != null)
            {
                this.PrinterList.RemoveAt(serverList.SelectedIndex);
            }
        }

        private void serverList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
/*            var selectedItemText = (serverList.SelectedItem ?? "(none)").ToString();
            MessageBox.Show("Selected: " + selectedItemText);
*/          removeButton.IsEnabled = true;

            if ( serverList.SelectedItem != null )
            {
                currentItem = (OctoPrinter) serverList.SelectedItem;
                if (currentItem != null ) {
                    this.serverAddress.Text = currentItem.ServerAddress;
                    this.apiKey.Text = currentItem.APIKey;
                    this.watchFolder.Text = currentItem.WatchFolder;

                }
            }
        }
    }
}
