using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace OctoUploader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public FileSystemWatcher watcher = null;
        //readonly Dictionary<string, long> timerFileHistory = new Dictionary<string, long>();

        readonly TaskbarIcon tbi = new TaskbarIcon();
        OctoprintUploader settingsWindow = null;

        public App ()
        {
            String logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\OctoUploader\\logs\\";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logDirectory + "logfile.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information("Logger started");

            System.Console.WriteLine(OctoUploader.Properties.Settings.Default.ToString());
            if (OctoUploader.Properties.Settings.Default.CallUpgrade)
            {
                OctoUploader.Properties.Settings.Default.Upgrade();
                OctoUploader.Properties.Settings.Default.CallUpgrade = false;
                OctoUploader.Properties.Settings.Default.Save();
            }

             Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/OctoUploader;component/Resources/octoprintupload.ico")).Stream;
            tbi.Icon = new System.Drawing.Icon(iconStream);
            tbi.ToolTipText = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            tbi.TrayMouseDoubleClick += Tbi_TrayMouseDoubleClick;
            tbi.MouseLeftButtonUp += Tbi_MouseLeftButtonUp;


            System.Windows.Controls.ContextMenu cm = new System.Windows.Controls.ContextMenu();

            System.Windows.Controls.MenuItem menuItemOpen =  new System.Windows.Controls.MenuItem { Header = "O_pen Octoprint" };
            menuItemOpen.Click += Open_Click;            

            System.Windows.Controls.MenuItem menuItemSettings = new System.Windows.Controls.MenuItem { Header = "S_ettings" };
            menuItemSettings.Click += Settings_Click;


            System.Windows.Controls.MenuItem menuItemExit = new System.Windows.Controls.MenuItem { Header = "E_xit" };
            menuItemExit.Click += Exit_Click;

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

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            LaunchOctoprint();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
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
            Log.Information("Watching {Folder} for server address {Server}",
                OctoUploader.Properties.Settings.Default.WatchLocation,
                OctoUploader.Properties.Settings.Default.ServerAddress);

            // setup system watcher
            if (Directory.Exists(watchFolder) && watcher == null )
            {
                Log.Information("Initializing filesystem watcher @ {Location}",
                 OctoUploader.Properties.Settings.Default.WatchLocation);


                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = watchFolder,
                    NotifyFilter = NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                watcher.Changed += Watcher_Changed;
            }
        }

        public void StopWatching ()
        {
            Log.Information("Uninitializing filesystem watcher.");
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= Watcher_Changed;
                watcher = null;
            }

        }


        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Log.Information("Change detected at Watch Location {Location}",
                e.FullPath);

            if (!File.Exists(e.FullPath)) return;

            if (e.ChangeType == WatcherChangeTypes.Changed && !IsFileLocked(e.FullPath)  )
            {
                UploadFile(e.FullPath);
            }

        }

        private bool IsFileLocked(string file)
        {
            const int ERROR_SHARING_VIOLATION = 32;
            const int ERROR_LOCK_VIOLATION = 33;

            //check that problem is not in destination file
            if (File.Exists(file) == true)
            {
                FileStream stream = null;
                try
                {
                    stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (Exception ex2)
                {
                    //_log.WriteLog(ex2, "Error in checking whether file is locked " + file);
                    int errorCode = Marshal.GetHRForException(ex2) & ((1 << 16) - 1);
                    if ((ex2 is IOException) && (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION))
                    {
                        Log.Information("IsFileLocked returning true with error code {ErrorCode}", errorCode );
                        return true;
                    }
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }
            }
            Log.Information("IsFileLocked returning false");
            return false;
        }


        public String UploadFile(String file)
        {

            var api = new OctoprintAPI
            {
                serverAddress = OctoUploader.Properties.Settings.Default.ServerAddress,
                apiKey = OctoUploader.Properties.Settings.Default.APIKey
            };

            if (!File.Exists(file)) return "";

            bool AutoStart = OctoUploader.Properties.Settings.Default.AutoStart;
            String result = api.UploadFile(file, AutoStart, AutoStart);

            if (api.lastResultCode == System.Net.HttpStatusCode.Created )
            {
                Log.Information("Successfully uploaded file to {Filename} to {Server}",
                    Path.GetFileName(file),
                    OctoUploader.Properties.Settings.Default.ServerAddress
                    );

                tbi.ShowBalloonTip("Octoprint Uploader", Path.GetFileName(file) + " uploaded successfully.", BalloonIcon.Info);

                if (OctoUploader.Properties.Settings.Default.DeleteOnUpload  )
                {
                    File.Delete(file);
                }
            }
            else
            {
                Log.Debug("Failed while uploading file {Filename} to {Server} with {ResultCode}",
                    Path.GetFileName(file),
                    OctoUploader.Properties.Settings.Default.ServerAddress,
                    api.lastResultCode
                    );
                tbi.ShowBalloonTip("Octoprint Uploader", "Could not upload file. Check your settings and ensure that Octoprint isn't already printing.", BalloonIcon.Error);
            }

            return result;
        }

        public void LaunchOctoprint ()
        {
            Log.Information ("Opening Octoprint using address {Server}",
                    OctoUploader.Properties.Settings.Default.ServerAddress
                    );
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
