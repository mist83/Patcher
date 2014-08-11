using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel();
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            var fileDropList = ((DataObject)e.Data).GetFileDropList();
            if (fileDropList.Count == 0)
            {
                MessageBox.Show("Can only drop files");
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
                ((ViewModel)DataContext).Files.Add(file);
            }

            if (!VM.Validate())
            {
                foreach (string file in fileList)
                {
                    ((ViewModel)DataContext).Files.Remove(file);
                }

                string message =
    @"Cannot create patch from these files.
Check that:

- All files in the patch descend from the same root.
- There is at least one file to be patched at the root level.
- Existing files in this patch (if any) follow these rules.";
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            MySP.Visibility = Visibility.Visible;
            DashRect.Visibility = Visibility.Collapsed;
            UnHover();
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            Hover();
        }

        private void Hover()
        {
            DashRect.Visibility = Visibility.Visible;
            DropTextBlock.Foreground = Brushes.AliceBlue;
            DropTextBlock.Text = "Drop Here";
            DashRect.Stroke = DropTextBlock.Foreground;
            MySP.Opacity = 0.5;
        }

        private void UnHover()
        {
            DropTextBlock.Text = "Here";
            DropTextBlock.Foreground = Brushes.LightGray;
            DashRect.Stroke = Brushes.LightGray;
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

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Patch Destination";
            dialog.Filter = "Executable Files|*.exe";
            dialog.ShowDialog();

            if (dialog.FileName != string.Empty)
            {
                try
                {
                    Utility.CompileExecutable(dialog.FileName, VM.RootDirectory, VM.TargetDirectory, VM.Files.ToArray());
                    MessageBox.Show(string.Format("Patch created successfully:{0}{0}{1}.", Environment.NewLine, dialog.FileName), "Success", MessageBoxButton.OK, MessageBoxImage.None);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("{0}{1}{1}could not be created. The error message returned was:{1}{1}{2}", dialog.FileName, Environment.NewLine, ex), "Success", MessageBoxButton.OK, MessageBoxImage.None);
                }
            }
        }

        public ViewModel VM { get { return (ViewModel)DataContext; } }
    }
}
