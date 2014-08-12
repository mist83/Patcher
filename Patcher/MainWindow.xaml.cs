using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Win32;

namespace Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Brush promptColor = Brushes.WhiteSmoke;
        private static Brush hoverColor = Brushes.AliceBlue;

        public MainWindow()
        {
            InitializeComponent();

            var location = new FileInfo(this.GetType().Assembly.Location);
            var directory = location.DirectoryName;
            if (File.Exists(Path.Combine(directory, "PatchSettings.xml")))
            {
                try
                {
                    XDocument document = XDocument.Load(Path.Combine(directory, "PatchSettings.xml"));
                    DataContext = new ViewModel(document.Descendants("Notes").Single().Value, document.Descendants("Target").Select(x => x.Value).ToArray());
                }
                catch { }
            }

            if (DataContext == null)
            {
                MessageBox.Show("A new settings file has been created that will be used for default values.", "Cannot Load Settings", MessageBoxButton.OK, MessageBoxImage.Information);

                XDocument document = ViewModel.CreateNewSettingsFile(@"C:\Program Files\My Application", @"C:\Program Files (x86)\My Application", @"C:\Program Files\My Application\Sub directory");
                document.Save("PatchSettings.xml");

                DataContext = new ViewModel(document.Descendants("Notes").Single().Value, document.Descendants("Target").Select(x => x.Value).ToArray());
            }

            Reset();
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            var fileDropList = ((DataObject)e.Data).GetFileDropList();
            if (fileDropList.Count == 0)
            {
                MessageBox.Show("You are only allowed to drag/drop files from the file system.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var fileList = new HashSet<string>();
            foreach (var file in fileDropList)
            {
                fileList.Add(file);
            }

            if (fileList.Any(x => (File.GetAttributes(x) & FileAttributes.Directory) == FileAttributes.Directory))
            {
                var directories = fileList.Where(x => (File.GetAttributes(x) & FileAttributes.Directory) == FileAttributes.Directory).ToArray();

                foreach (var directory in directories)
                {
                    if (fileList.Contains(directory))
                    {
                        fileList.Remove(directory);
                    }
                }

                foreach (var directory in directories)
                {
                    var files = new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        fileList.Add(file.FullName);
                    }
                }
            }

            foreach (string file in fileList.OrderBy(x => x))
            {
                if (!VM.Files.Contains(file))
                {
                    VM.Files.Add(file);
                }
            }

            UnHover();
            MySP.Visibility = Visibility.Visible;
            FileListStackPanel.Visibility = Visibility.Visible;
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            Hover();
        }

        private void Hover()
        {
            HoverGrid.Visibility = Visibility.Visible;

            DropTextBlock.Foreground = hoverColor;
            DropTextBlock.Text = "Drop Patch Files Here";
            DashRect.Stroke = hoverColor;
            MySP.Opacity = 0.5;
        }

        private void UnHover()
        {
            if (VM.Files.Any())
            {
                HoverGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                HoverGrid.Visibility = Visibility.Visible;

                DropTextBlock.Text = "Drag Patch Files Here";
                DropTextBlock.Foreground = promptColor;
                DashRect.Stroke = promptColor;
            }

            MySP.Opacity = 1;
        }

        private void Grid_DragLeave(object sender, DragEventArgs e)
        {
            UnHover();
        }

        private void Grid_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {

        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var optionsWindow = new PatchOptionsWindow(VM);
            optionsWindow.ShowDialog();
        }

        private void BuildPatch_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Patch Destination";
            dialog.Filter = "Executable Files|*.exe";
            dialog.ShowDialog();

            if (dialog.FileName != string.Empty)
            {
                this.IsEnabled = false;
                Thread t = new Thread(new ParameterizedThreadStart(CreatePatch));

                var patchOptions = new PatchOptions
                {
                    OutputAssembly = dialog.FileName,
                    RootDirectory = VM.RootDirectory,
                    PatchNoteContent = VM.Notes,
                    TargetDirectories = VM.TargetDirectories.ToArray(),
                    Files = VM.Files.ToArray(),
                };

                t.Start(patchOptions);
            }
        }

        private void CreatePatch(object parameter)
        {
            var patchOptions = (PatchOptions)parameter;

            try
            {
                Utility.CompileExecutable(patchOptions);

                Dispatcher.Invoke(() =>
                {
                    XDocument document = ViewModel.CreateNewSettingsFile(VM.TargetDirectories.ToArray());
                    document.Save("PatchSettings.xml");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(string.Format("{0}{1}{1}could not be created. The error message returned was:{1}{1}{2}", patchOptions.OutputAssembly, Environment.NewLine, ex), "Success", MessageBoxButton.OK, MessageBoxImage.None);
                    return;
                });
            }

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(string.Format("Patch created successfully:{0}{0}{1}.", Environment.NewLine, patchOptions.OutputAssembly), "Success", MessageBoxButton.OK, MessageBoxImage.None);
                this.IsEnabled = true;
            });
        }

        public ViewModel VM { get { return (ViewModel)DataContext; } }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            string toRemove = Path.Combine(VM.RootDirectory, (string)((MenuItem)sender).Tag);
            VM.Files.Remove(toRemove);

            if (!VM.Files.Any())
            {
                Reset();
            }
        }

        private void ClearFiles_Click(object sender, RoutedEventArgs e)
        {
            VM.Files.Clear();
            Reset();
        }

        private void Reset()
        {
            FileListStackPanel.Visibility = Visibility.Collapsed;
            MySP.Visibility = Visibility.Collapsed;
            HoverGrid.Visibility = Visibility.Visible;
        }
    }

    public class PatchOptions
    {
        public PatchOptions()
        {
            PatchNoteHeader = "Patch";
        }

        public string OutputAssembly { get; set; }

        public string RootDirectory { get; set; }

        public string[] TargetDirectories { get; set; }

        public string PatchNoteHeader { get; set; }

        public string PatchNoteContent { get; set; }

        public string[] Files { get; set; }
    }
}
