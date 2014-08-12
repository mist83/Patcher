using System.Windows;
using System.Windows.Controls;

namespace Patcher
{
    /// <summary>
    /// Interaction logic for PatchOptions.xaml
    /// </summary>
    public partial class PatchOptionsWindow
    {
        public ViewModel VM { get { return (ViewModel)DataContext; } }

        public PatchOptionsWindow()
        {
            InitializeComponent();
        }
        public PatchOptionsWindow(ViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            VM.TargetDirectories.Remove((string)((MenuItem)sender).Tag);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select a folder that will be targeted to patch";

            dialog.ShowDialog();

            if (!string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                if (!VM.TargetDirectories.Contains(dialog.SelectedPath))
                {
                    VM.TargetDirectories.Add(dialog.SelectedPath);
                }
            }
        }
    }
}
