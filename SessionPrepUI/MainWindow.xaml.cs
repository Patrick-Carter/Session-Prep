using SessionPrepLibrary;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SessionPrepUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ObservableCollection<AudioFiles> audioFilesList = new ObservableCollection<AudioFiles>();
        AudioProcesser audioProcesser = new AudioProcesser();
        float normalizeValue;


        public MainWindow()
        {
            InitializeComponent();
            audioFileListView.ItemsSource = audioFilesList;
            filePathTextBox.Text = SessionPrepUI.Properties.Settings.Default.filePathOutput;
            normalizeToTextBox.Text = SessionPrepUI.Properties.Settings.Default.defaultDBFS;
            conversionCheckBox.IsChecked = SessionPrepUI.Properties.Settings.Default.conversionIsChecked;
            normalizeCheckBox.IsChecked = SessionPrepUI.Properties.Settings.Default.normIsChecked;
            blankFileCheckBox.IsChecked = SessionPrepUI.Properties.Settings.Default.blankIsChecked;
        }

        private void AddFileButtonClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

            fileDialog.Multiselect = true;
            fileDialog.Filter = "(*.wav, *.aiff, *.mp3, *.aac, *.mp4)|*.wav;*.aiff;*.mp3;*.aac;*.mp4;";
            Nullable<bool> dialogOK = fileDialog.ShowDialog();

            foreach (var file in fileDialog.FileNames)
            {
                var onlyFileName = System.IO.Path.GetFileNameWithoutExtension(file);
                audioFilesList.Add(new AudioFiles() { FilePath = file, Name = onlyFileName + ".wav" });
            }
        }

        private void RemoveFileButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = audioFileListView.SelectedItems.Cast<AudioFiles>().ToList();

            foreach (AudioFiles item in selectedItems)
            {
                audioFilesList.Remove(item);
            }
        }

        private void ProcessButtonClick(object sender, RoutedEventArgs e)
        {
            string outputFolder = filePathTextBox.Text;
            bool removeBlankFiles = (bool)blankFileCheckBox.IsChecked;

            if ((bool)normalizeCheckBox.IsChecked)
            {
                try
                {
                    normalizeValue = float.Parse(normalizeToTextBox.Text);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Normalize value must be between -30 and 0 dbFS");
                }
            }
            

            // processes based on selected 
            try
            {
                progressIndicator.IsBusy = true;

                if ((bool)normalizeCheckBox.IsChecked && (bool)conversionCheckBox.IsChecked)
                {
                    Task.Factory.StartNew(() =>
                        {
                            foreach (AudioFiles files in audioFilesList)
                            {
                                bool process = true;

                                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                {
                                    progressIndicator.BusyContent = string.Format(($"Processing {files.Name}..."), files);
                                }));

                                if (removeBlankFiles)
                                {
                                    if (audioProcesser.CheckForEmptyFile(files.FilePath))
                                    {
                                        process = false;
                                    }
                                }

                                if (process)
                                {
                                    audioProcesser.ConvertChannels(files.FilePath, outputFolder, files.Name);
                                    audioProcesser.Normalize($@"{outputFolder}\{files.Name}", outputFolder, files.Name, normalizeValue);
                                }
                            }
                        }
                    ).ContinueWith((task) =>
                        {
                            progressIndicator.IsBusy = false;
                            audioFilesList.Clear();
                            System.Windows.MessageBox.Show("Processing Complete!");
                        }, TaskScheduler.FromCurrentSynchronizationContext()
                    );
                }
                else if ((bool)normalizeCheckBox.IsChecked)
                {
                    Task.Factory.StartNew(() =>
                    {
                        foreach (AudioFiles files in audioFilesList)
                        {
                            bool process = true;

                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                progressIndicator.BusyContent = string.Format(($"Processing {files.Name}..."), files);
                            }));

                            if (removeBlankFiles)
                            {
                                if (audioProcesser.CheckForEmptyFile(files.FilePath))
                                {
                                    process = false;
                                }
                            }
                            if (process)
                            {
                                audioProcesser.Normalize(files.FilePath, outputFolder, files.Name, normalizeValue);
                            }
                        }
                    }
                    ).ContinueWith((task) =>
                    {
                        progressIndicator.IsBusy = false;
                        audioFilesList.Clear();
                        System.Windows.MessageBox.Show("Processing Complete!");
                    }, TaskScheduler.FromCurrentSynchronizationContext()
                    );
                }
                else if ((bool)conversionCheckBox.IsChecked)
                {
                    Task.Factory.StartNew(() =>
                    {
                        foreach (AudioFiles files in audioFilesList)
                        {
                            bool process = true;
                            
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                progressIndicator.BusyContent = string.Format(($"Processing {files.Name}..."), files);
                            }));

                            if (removeBlankFiles)
                            {
                                if (audioProcesser.CheckForEmptyFile(files.FilePath))
                                {
                                    process = false;
                                }
                            }
                            if (process)
                            {
                                audioProcesser.ConvertChannels(files.FilePath, outputFolder, files.Name);
                            }
                        }
                    }
                    ).ContinueWith((task) =>
                    {
                        progressIndicator.IsBusy = false;
                        audioFilesList.Clear();
                        System.Windows.MessageBox.Show("Processing Complete!");
                    }, TaskScheduler.FromCurrentSynchronizationContext()
                    );
                }
                else
                {
                    System.Windows.MessageBox.Show("Please select normalize and/or stereo conversion checkbox");
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Please select a valid file path");
            }
        }

        private void setFilePathButtonClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fileDialog = new FolderBrowserDialog();

            DialogResult result = fileDialog.ShowDialog();

            filePathTextBox.Text = fileDialog.SelectedPath;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SessionPrepUI.Properties.Settings.Default.filePathOutput = filePathTextBox.Text;
            SessionPrepUI.Properties.Settings.Default.defaultDBFS = normalizeToTextBox.Text;
            SessionPrepUI.Properties.Settings.Default.conversionIsChecked = (bool)conversionCheckBox.IsChecked;
            SessionPrepUI.Properties.Settings.Default.normIsChecked = (bool)normalizeCheckBox.IsChecked;
            SessionPrepUI.Properties.Settings.Default.blankIsChecked = (bool)blankFileCheckBox.IsChecked;

            SessionPrepUI.Properties.Settings.Default.Save();
        }
    }
}
