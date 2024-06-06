using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OctoUploader
{
    public class OctoPrinter
    {
        String serverAddress;
        String apiKey;
        String watchFolder;
        FileSystemWatcher watcher;

        public String ServerAddress
        {
            get { return serverAddress; }
            set { serverAddress = value; }
        }

        public String APIKey
        {
            get { return apiKey; }
            set { apiKey = value; }
        }

        public String WatchFolder
        {
            get { return watchFolder; }
            set { watchFolder = value; }
        }

        public FileSystemWatcher FileSystemWatcher
        {
            get { return watcher; }
            set { watcher = value; }
        }

        public static ObservableCollection<OctoPrinter> LoadFromSettings()
        {
            ObservableCollection<OctoPrinter> PrinterList = new ObservableCollection<OctoPrinter>();
            
            if (Properties.Settings.Default.ServerAddresses.Count > 0)
            {
                for (var c = 0; c < Properties.Settings.Default.ServerAddresses.Count; c++)
                {
                    var printer1 = new OctoPrinter()
                    {
                        ServerAddress = Properties.Settings.Default.ServerAddresses[c],
                        APIKey = Properties.Settings.Default.APIKeys[c],
                        WatchFolder = Properties.Settings.Default.WatchLocations[c]
                    };
                    PrinterList.Add(printer1);
                }
            }
            else if (PrinterList.Count == 0)
            {
                var printer1 = new OctoPrinter()
                {
                    ServerAddress = Properties.Settings.Default.ServerAddress,
                    APIKey = Properties.Settings.Default.APIKey,
                    WatchFolder = Properties.Settings.Default.WatchLocation
                };
                PrinterList.Add(printer1);
            }
            return PrinterList;
        }

    }

}
