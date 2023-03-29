using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace UnityPackageExtractor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private BackgroundWorker worker;
        private string source_path = string.Empty;
        private string dest_path = string.Empty;
        private string dest_path_override = string.Empty;

        public MainWindow() {

            InitializeComponent();

            worker = new BackgroundWorker {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;

            ProgressBar.Value = 0;
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e) {
            Dispatcher.Invoke(() => {
                ProgressBar.Value = e.ProgressPercentage;
            });
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e) {
            Dispatcher.Invoke(() => {
                ProgressBar.Value = 100;
                TextBlock_MessageBar.Text = "Done";
            });
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e) {
            try {
                Dispatcher.BeginInvoke(new Action(() => {
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = 0;
                }));

                DirectoryInfo directoryInfo = new(source_path);

                FileInfo[] info = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

                var packs = info.GroupBy(x => Path.GetDirectoryName(x.FullName));

                bool overwrite = Dispatcher.Invoke(() => {
                    return (bool)CheckBox_Overwrite.IsChecked;
                });

                foreach (var pack in packs) {

                    string asset_name = "";
                    string asset = "";

                    foreach (var item in pack) {
                        if (Path.GetFileName(item.FullName) == "pathname") {
                            asset_name = item.FullName;
                        } else if (Path.GetFileName(item.FullName) == "asset" && Path.GetExtension(item.FullName) == "") {
                            asset = item.FullName;
                        }
                    }

                    if (!string.IsNullOrEmpty(asset_name) && !string.IsNullOrEmpty(asset)) {

                        string OriginalFileName = File.ReadAllText(asset_name);

                        string dest_address = GetDestPath(OriginalFileName);
                        string dest_address_dir = Path.GetDirectoryName(dest_address);

                        if (!Directory.Exists(dest_address_dir))
                            Directory.CreateDirectory(dest_address_dir);

                        File.Copy(asset, dest_address, overwrite);

                        Dispatcher.Invoke(() => {
                            TextBlock_MessageBar.Text = "Processing " + OriginalFileName;
                            ProgressBar.Value += (1.0f / packs.Count())*100;
                        });
                    }
                }

            } catch (Exception ex) {

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetDestPath(string fileName = "") {
            if (string.IsNullOrEmpty(fileName)) {

                return string.IsNullOrEmpty(dest_path_override) ? $@"{source_path}\Source\" : dest_path;

            } else {

                bool c = Dispatcher.Invoke(() => {
                    return (bool)CheckBox_KeepStructure.IsChecked;
                });

                if (c)
                    return $@"{dest_path}\{fileName}";

                return $@"{dest_path}\{Path.GetFileName(fileName)}";
            }
        }

        private void Button_ChangeSouceDir_Click(object sender, RoutedEventArgs e) {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    source_path = dialog.SelectedPath;
                    TextBox_SourceDir.Text = dialog.SelectedPath;
                    dest_path = GetDestPath();
                    TextBox_DestPath.Text = dest_path;
                }
            }
        }

        private void Button_ChangeDestDir_Click(object sender, RoutedEventArgs e) {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                dest_path = dialog.SelectedPath;
                TextBox_DestPath.Text = dialog.SelectedPath;
            }
        }

        private void Btn_Run_Click(object sender, RoutedEventArgs e) {

            if (string.IsNullOrEmpty(source_path)) {
                MessageBox.Show(this, "No path selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            worker.RunWorkerAsync();

        }
    }
}