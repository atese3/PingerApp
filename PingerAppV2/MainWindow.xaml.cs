using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PicoXLSX;
using Style = PicoXLSX.Style;

namespace PingerAppV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private BackgroundWorker backgroundWorker;
        private string _delimeter = string.Empty;
        private string _filePath = string.Empty;
        private ObservableCollection<string> _tempSuccessfulIp = new ObservableCollection<string>();
        private ObservableCollection<string> _tempFailedIp = new ObservableCollection<string>();

        private readonly List<string> _readLines = new List<string>();
        private ObservableCollection<string> _deviceIPs = new ObservableCollection<string>();
        private ObservableCollection<KeyValuePair<string, string>> _successfulIp = new ObservableCollection<KeyValuePair<string, string>>();
        private ObservableCollection<KeyValuePair<string, string>> _failedIp = new ObservableCollection<KeyValuePair<string, string>>();

        public ObservableCollection<string> DeviceIPs
        {
            get { return _deviceIPs; }
            set
            {
                _deviceIPs = value;
                OnPropertyChanged("DeviceIPs");
            }
        }
        public ObservableCollection<KeyValuePair<string, string>> SuccessfulIp
        {
            get { return _successfulIp; }
            set
            {
                _successfulIp = value;
                OnPropertyChanged("SuccessfulIp");
            }
        }
        public ObservableCollection<KeyValuePair<string, string>> FailedIp
        {
            get { return _failedIp; }
            set
            {
                _failedIp = value;
                OnPropertyChanged("FailedIp");
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (backgroundWorker != null)
            {
                backgroundWorker.CancelAsync();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void BtnSelectFoler_OnClick(object sender, RoutedEventArgs e)
        {
            BtnSelectFoler.Background = (Brush)new BrushConverter().ConvertFromString("#FFDDDDDD");
            var openFileDialog1 = new OpenFileDialog()
            {
                FileName = "Select a file",
                Title = "Get File"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                _filePath = openFileDialog1.FileName;
                BtnSelectFoler.Background = Brushes.GreenYellow;
            }
        }

        private void BtnStart_OnClick(object sender, RoutedEventArgs e)
        {

                VisualStateManager.GoToElementState(grid, StartStateOn.Name, true);
                var ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");

                using (var sr = new StreamReader(_filePath))
                {
                    while (!sr.EndOfStream)
                    {
                        //_readLines.Add(sr.ReadLine());
                        var temp = sr.ReadLine();
                        if (temp != null)
                        {
                            var result = ip.Matches(temp);
                            if (result.Count > 0)
                            {
                                if (result[0].Success)
                                {
                                    if (!DeviceIPs.Contains(result[0].ToString()))
                                    {
                                        DeviceIPs.Add(result[0].ToString());
                                    }
                                }
                            }
                        }
                    }
                }

                RectangleSelect.Visibility = Visibility.Visible;
                RectangleStart.Visibility = Visibility.Visible;
                backgroundWorker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true,
                    WorkerReportsProgress = true
                };
                backgroundWorker.DoWork += backgroundWorker_DoWork;
                backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
                backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
                backgroundWorker.RunWorkerAsync(DeviceIPs.ToList());

        }

        void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (StackPanelProgress.Visibility == Visibility.Collapsed)
            {
                StackPanelProgress.Visibility = Visibility.Visible;
            }
            var width = (e.ProgressPercentage * 320) / 100;
            RectangleProgress.Width = width;
            LabelProgress.Width = width < 34 ? 34 : width;
            LabelProgress.Content = "%" + e.ProgressPercentage;
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            VisualStateManager.GoToElementState(grid, StartStateOff.Name, true);
            LabelProgress.Content = "%100";
            RectangleProgress.Width = 320;
            LabelProgress.Width = 320;
            RectangleSelect.Visibility = Visibility.Collapsed;
            RectangleStart.Visibility = Visibility.Collapsed;
            ButtonExport.IsEnabled = true;
        }

        private IEnumerable<string[]> Split(IEnumerable<string> input)
        {
            return input.Select(item => item.Split(_delimeter.FirstOrDefault()));
        }

        private void StartPing(IEnumerable<string> ipLine, BackgroundWorker bw)
        {
            var iter = 0.0;
            var listCount = ipLine.Count();
            foreach (var line in ipLine)
            {
                try
                {

                    var myPing = new Ping();
                    var reply = myPing.Send(line, 100);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        var temp = new KeyValuePair<string, string>(line, IPStatus.Success.ToString());
                        if (!SuccessfulIp.Contains(temp))
                        {
                            if (Dispatcher != null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    SuccessfulIp.Add(temp);
                                });

                            }
                        }
                        continue;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
                var temp2 = new KeyValuePair<string, string>(line, IPStatus.TimedOut.ToString());

                if (!FailedIp.Contains(temp2))
                {
                    if (Dispatcher != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            FailedIp.Add(temp2);
                        });
                    }
                }
                var progress = (iter++ / listCount) * 100;
                bw.ReportProgress((int)progress);
            }
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var ipLine = e.Argument as IEnumerable<string>;
            if (ipLine != null)
            {
                var bw = (BackgroundWorker)sender;

                if (bw != null)
                {
                    StartPing(ipLine, bw);
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

        private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var date = DateTime.Now;
                var fileName = "Pinger" + date.Year + date.Month + date.Day + "_" + date.Hour + date.Minute +
                               date.Second + date.Millisecond;
                var workbook = new Workbook(fileName +  ".xlsx", "Status"); 
                var listValues = new List<KeyValuePair<string, string>>();
                listValues.AddRange(FailedIp);
                listValues.AddRange(SuccessfulIp);
                listValues = listValues.OrderBy(x => x.Key).ToList();
                workbook.CurrentWorksheet.AddNextCell("IP");
                workbook.CurrentWorksheet.AddNextCell("Status");
                workbook.CurrentWorksheet.GoToNextRow();
                foreach (var value in listValues)
                {
                    workbook.CurrentWorksheet.AddNextCell(value.Key);
                    workbook.CurrentWorksheet.AddNextCell(value.Value);
                    workbook.CurrentWorksheet.GoToNextRow();
                }
                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exporting Error: " + ex.Message);
            }
        }
    }
}
