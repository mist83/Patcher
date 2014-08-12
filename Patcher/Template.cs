using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Patcher
{
    class PatchTemplate
    {
        private static DateTime patchLoadTime;
        private static string[] targetDirectories = null; // TEXT: Target directories
        private static string targetDirectory;

        private static Dictionary<string, byte[]> _Resources = null;

        private static Button runButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(4),
            Padding = new Thickness(4),
            FontSize = 14,
            Content = "Apply Patch",
            IsEnabled = false,
        };

        public static void CreatePatch()
        {
            targetDirectory = GetTargetDirectory();

            Window window = new Window
            {
                Title = "Patch title", // TEXT: Patch title
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanMinimize,
                Width = 525,
                Height = 350,
            };

            #region It feels like too much effort to figure out how to lay out the window by parsing an existing XAML document, so i just do it all in code here

            Grid mainGrid = new Grid { Margin = new Thickness(4) };
            window.Content = mainGrid;
            mainGrid.RowDefinitions.Add(new RowDefinition());
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            GroupBox groupBox = new GroupBox
            {
                Header = new TextBlock
                {
                    FontSize = 14,
                    Text = "<<PATCH_NOTE_HEADER>>",
                    Margin = new Thickness(4)
                },

                Content = new TextBox
                {
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    FontSize = 14,
                    Text = "<<PATCH_NOTE_CONTENT>>"
                }
            };

            mainGrid.Children.Add(groupBox);

            var hyperlinkTextBlock = new TextBlock
            {
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4),
            };

            var hyperlink = new Hyperlink();
            hyperlink.Inlines.Add(new Run("Details and Options (Advanced)..."));
            hyperlinkTextBlock.Inlines.Add(hyperlink);
            hyperlink.Click += hyperlink_Click;

            Grid.SetRow(hyperlinkTextBlock, 1);
            mainGrid.Children.Add(hyperlinkTextBlock);

            runButton.Click += runButton_Click;

            Grid.SetRow(runButton, 1);
            mainGrid.Children.Add(runButton);

            #endregion

            var application = Application.Current;
            if (application == null)
            {
                application = new Application();
                application.Run(window);
            }
            else
            {
                window.Show();
            }
        }

        static void runButton_Click(object sender, RoutedEventArgs e)
        {
            var methods = typeof(PatchTemplate).GetMethods();
            if (methods.Any(x => x.Name == "CreatePatch"))
            {
                // This code will only run in debug mode
                var tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, "Hello, world! This text was written to a file from the debug code of the patcher.");
            }

            patchLoadTime = DateTime.Now;

            Debug.WriteLine("Output directory: " + targetDirectories);
            foreach (var item in Resources)
            {
                var target = Path.Combine(targetDirectory, item.Key);
                var result = MessageBox.Show(target, "OK to Create/Replace?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ReplaceFile(target, item.Value);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    break;
                }
            }
        }

        private static string GetTargetDirectory()
        {
            foreach (var targetDirectory in targetDirectories)
            {
                if (Directory.Exists(targetDirectory))
                {
                    runButton.IsEnabled = true;
                    return targetDirectory;
                }
            }

            return string.Empty;
        }

        private static Dictionary<string, byte[]> Resources
        {
            get
            {
                if (_Resources == null)
                {
                    var assembly = typeof(PatchTemplate).Assembly;

                    _Resources = new Dictionary<string, byte[]>();
                    foreach (var resource in typeof(PatchTemplate).Assembly.GetManifestResourceNames())
                    {
                        var stream = assembly.GetManifestResourceStream(resource);
                        using (ResourceReader reader = new ResourceReader(stream))
                        {
                            var enumerator = reader.GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                string key = (string)enumerator.Key;
                                string resourceType;
                                byte[] data;
                                reader.GetResourceData(key, out resourceType, out data);

                                _Resources[key] = data.ToArray();
                            }
                        }
                    }
                }

                return _Resources;
            }
        }

        static void hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Window window = new Window
            {
                Title = "Details and Options (Advanced)",
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanMinimize,
                Width = 525,
                Height = 350,
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition());
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var groupBox = new GroupBox
            {
                Header = new TextBlock
                {
                    FontSize = 14,
                    Margin = new Thickness(4),
                    Text = "Files Included in this Patch",
                },

                Margin = new Thickness(4),
            };
            mainGrid.Children.Add(groupBox);

            var destinationGroupBox = new GroupBox
            {
                Header = new TextBlock
                {
                    FontSize = 14,
                    Margin = new Thickness(4),
                    Text = "Target Directory",
                },

                Margin = new Thickness(4),
            };
            Grid.SetRow(destinationGroupBox, 1);

            Grid targetGrid = new Grid();
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition());

            TextBox targetTextBox = new TextBox { FontSize = 14, IsReadOnly = true, Margin = new Thickness(4), Text = targetDirectory };
            Grid.SetColumn(targetTextBox, 1);
            targetGrid.Children.Add(targetTextBox);

            Button targetButton = new Button
            {
                Margin = new Thickness(4),
                Tag = targetTextBox,
            };
            targetButton.Content = new TextBlock { Margin = new Thickness(4), FontSize = 14, Text = "Change...", };
            targetButton.Click += targetButton_Click;

            targetGrid.Children.Add(targetButton);

            destinationGroupBox.Content = targetGrid;

            mainGrid.Children.Add(destinationGroupBox);

            window.Content = mainGrid;

            ListBox listBox = new ListBox();
            groupBox.Content = listBox;

            foreach (var item in Resources)
            {
                var tb = new TextBlock { FontSize = 14, Margin = new Thickness(4), Text = item.Key };
                listBox.Items.Add(tb);
            }

            window.ShowDialog();
        }

        static void targetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            dialog.ShowDialog();
            if (!string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                TextBox textBox = (TextBox)((Button)sender).Tag;
                textBox.Text = dialog.SelectedPath;
                targetDirectory = dialog.SelectedPath;
            }
        }

        private static void ReplaceFile(string destination, byte[] content)
        {
            // Remove the original file
            if (File.Exists(destination))
            {
                // always create a backup of the file we're overwriting
                string suffix = string.Format(patchLoadTime.ToString(".yyyy_MM_dd__HH_mm_ss") + ".patchbackup");
                File.Copy(destination, destination + suffix);

                File.Delete(destination);
            }

            // Write the new file in its place
            if (!Directory.Exists(new FileInfo(destination).DirectoryName))
            {
                Directory.CreateDirectory(new FileInfo(destination).DirectoryName);
            }

            File.WriteAllBytes(destination, content);
        }
    }
}