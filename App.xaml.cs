using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OctoUploader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public FileSystemWatcher watcher = new FileSystemWatcher();
        TaskbarIcon tbi = new TaskbarIcon();
        OctoprintUploader settingsWindow = null;

        public App ()
        {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/OctoUploader;component/Resources/octoprintupload.ico")).Stream;
            tbi.Icon = new System.Drawing.Icon(iconStream);
            tbi.ToolTipText = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            tbi.TrayMouseDoubleClick += Tbi_TrayMouseDoubleClick;
            tbi.MouseLeftButtonUp += Tbi_MouseLeftButtonUp;


            System.Windows.Controls.ContextMenu cm = new System.Windows.Controls.ContextMenu();

            System.Windows.Controls.MenuItem menuItemOpen =  new System.Windows.Controls.MenuItem { Header = "O_pen Octoprint" };
            menuItemOpen.Click += open_Click;            

            System.Windows.Controls.MenuItem menuItemSettings = new System.Windows.Controls.MenuItem { Header = "S_ettings" };
            menuItemSettings.Click += settings_Click;


            System.Windows.Controls.MenuItem menuItemExit = new System.Windows.Controls.MenuItem { Header = "E_xit" };
            menuItemExit.Click += exit_Click;

            cm.Items.Add(menuItemOpen);
            cm.Items.Add(new Separator());
            cm.Items.Add(menuItemSettings);
            cm.Items.Add(menuItemExit);
            tbi.ContextMenu = cm;

            if (OctoUploader.Properties.Settings.Default.ServerAddress!="")
            {
                StartWatching();
            }
            else
            {
                ShowSettings();
            }

        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            LaunchOctoprint();
        }

        private void settings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            tbi.Dispose();
            Application.Current.Shutdown();
        }

        private void Tbi_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tbi.ShowBalloonTip("Octoprint Uploader Running", "", BalloonIcon.Info);
        }


        public void StartWatching ()
        {
            String watchFolder = OctoUploader.Properties.Settings.Default.WatchLocation;
            Console.WriteLine("Starting the folder watcher on " + watchFolder);


            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/OctoUploader;component/Resources/octoprintupload.ico")).Stream;
            System.Drawing.Icon icon = new System.Drawing.Icon(iconStream);
            tbi.ShowBalloonTip("Octoprint Uploader", "Starting watching " + watchFolder, BalloonIcon.Info);

            // setup system watcher
            if (Directory.Exists(watchFolder))
            {
                watcher.Path = watchFolder;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.EnableRaisingEvents = true;

                watcher.Changed += Watcher_Changed;
            }
        }

        public void StopWatching ()
        {
            Console.WriteLine("Stopping the watcher.");
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath)) return;

            String result = UploadFile(e.FullPath);

            /*resultBox.Dispatcher.Invoke(
                new UpdateTextCallback(this.UpdateText),
                new object[] { result }
            );*/
        }


        public String UploadFile(String file)
        {

            OctoprintAPI api = new OctoprintAPI();
            api.serverAddress = OctoUploader.Properties.Settings.Default.ServerAddress;
            api.apiKey = OctoUploader.Properties.Settings.Default.APIKey;

            if (!File.Exists(file)) return "";

            bool AutoStart = OctoUploader.Properties.Settings.Default.AutoStart;
            String result = api.UploadFile(file, AutoStart, AutoStart);

            if (OctoUploader.Properties.Settings.Default.DeleteOnUpload == true && api.lastResultCode == System.Net.HttpStatusCode.Created)
            {
                File.Delete(file);
            }

            return result;
        }

        public void LaunchOctoprint ()
        {
            System.Diagnostics.Process.Start(OctoUploader.Properties.Settings.Default.ServerAddress);
        }

        public void ShowSettings()
        {
            if (settingsWindow == null)
            {
                StopWatching();

                settingsWindow = new OctoprintUploader();
                settingsWindow.Closed += Window_Closed;
                settingsWindow.Show();            
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StartWatching();
            settingsWindow = null;
        }

        private void Tbi_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            LaunchOctoprint();
        }
    }


}
