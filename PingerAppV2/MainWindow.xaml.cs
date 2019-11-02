using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace PingerAppV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _delimeter = string.Empty;
        private string _filePath = string.Empty;
        private readonly List<string> _readLines = new List<string>();
        private ObservableCollection<string> _deviceIPs = new ObservableCollection<string>();
        private ObservableCollection<string> _successfulIp = new ObservableCollection<string>();
        private ObservableCollection<string> _failedIp = new ObservableCollection<string>();

        public ObservableCollection<string> DeviceIPs
        {
            get { return _deviceIPs; }
            set
            {
                _deviceIPs = value;
                OnPropertyChanged("DeviceIPs");
            }
        }
        public ObservableCollection<string> SuccessfulIp
        {
            get { return _successfulIp; }
            set
            {
                _successfulIp = value;
                OnPropertyChanged("SuccessfulIp");
            }
        }
        public ObservableCollection<string> FailedIp
        {
            get { return _failedIp; }
            set
            {
                _failedIp = value;
                OnPropertyChanged("FailedIp");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void BtnSelectFoler_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new OpenFileDialog()
            {
                FileName = "Select a file",
                Title = "Get File"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                _filePath = openFileDialog1.FileName;
            }
        }

        private void BtnStart_OnClick(object sender, RoutedEventArgs e)
        {
            _delimeter = TxtDelimeter.Text;
            if (string.IsNullOrEmpty(_delimeter))
            {
                MessageBox.Show("Delimeter Should be Determined Before Start.");
            }
            else
            {
                using (var sr = new StreamReader(_filePath))
                {
                    while (!sr.EndOfStream)
                    {
                        _readLines.Add(sr.ReadLine());
                    }
                }
            }

            var splicedLines = Split(_readLines.Skip(1).ToList());
            foreach (var lineArrays in splicedLines)
            {
                var ip = string.Empty;
                foreach (var item in lineArrays)
                {
                    IPAddress temp;
                    if (IPAddress.TryParse(item, out temp))
                    {
                        ip = item;
                        break;
                    }
                }

                if (!DeviceIPs.Contains(ip))
                {
                    DeviceIPs.Add(ip);
                }
            }

            StartPing(DeviceIPs.ToList());
        }

        private IEnumerable<string[]> Split(IEnumerable<string> input)
        {
            return input.Select(item => item.Split(_delimeter.FirstOrDefault()));
        }

        private void StartPing(IEnumerable<string> ipLine)
        {
            VisualStateManager.GoToElementState(grid, StartStateOn.Name, true);
            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.RunWorkerAsync(ipLine);
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var ipLine = e.Argument as IEnumerable<string>;
            if (ipLine != null)
            {
                if (Dispatcher != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var line in ipLine)
                        {
                            try
                            {
                                var myPing = new Ping();
                                var reply = myPing.Send(line, 100);
                                if (reply != null && reply.Status == IPStatus.Success)
                                {
                                    if (!SuccessfulIp.Contains(line))
                                    {
                                        SuccessfulIp.Add(line);
                                    }
                                    continue;
                                }
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                            if (!FailedIp.Contains(line))
                            {
                                FailedIp.Add(line);
                            }
                        }
                        VisualStateManager.GoToElementState(grid, StartStateOff.Name, true);
                    });
                }
            }
        }

        // Property Change function to notify clients
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
