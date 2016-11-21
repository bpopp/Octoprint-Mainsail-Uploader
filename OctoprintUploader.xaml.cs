using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace OctoUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OctoprintUploader : Window
    {

        public delegate void ChangeHandler ();

        public event ChangeHandler Changed;

        protected virtual void OnChanged()
        {
            Changed?.Invoke();
        }

        public OctoprintUploader()
        {
            InitializeComponent();

            // init properties
            this.serverAddress.Text = Properties.Settings.Default.ServerAddress;
            this.apiKey.Text = Properties.Settings.Default.APIKey;
            this.watchFolder.Text = Properties.Settings.Default.WatchLocation;
            this.removeUploads.IsChecked = Properties.Settings.Default.DeleteOnUpload;
            this.autoStart.IsChecked = Properties.Settings.Default.AutoStart;
            this.relaunchOnStartup.IsChecked = Properties.Settings.Default.RelaunchOnStartup;

        }

        public delegate void UpdateTextCallback(String message);
        /*private void UpdateText(string message)
        {
            resultBox.Text = message;
        }*/


        private void watchButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            Properties.Settings.Default.ServerAddress = this.serverAddress.Text;
            Properties.Settings.Default.APIKey = this.apiKey.Text;
            Properties.Settings.Default.WatchLocation = this.watchFolder.Text;
            Properties.Settings.Default.DeleteOnUpload = (bool)this.removeUploads.IsChecked;
            Properties.Settings.Default.AutoStart = (bool)this.autoStart.IsChecked;
            Properties.Settings.Default.RelaunchOnStartup = (bool)this.relaunchOnStartup.IsChecked;

            Properties.Settings.Default.Save();

            if ( this.relaunchOnStartup.IsChecked == true )
            {
                rkApp.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName, System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
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
    }
}
