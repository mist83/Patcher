using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Patcher
{
    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel(string notes, params string[] targetDirectories)
        {
            Files = new ObservableCollection<string>();
            TargetDirectories = new ObservableCollection<string>();

            Notes = notes;
            foreach (var item in targetDirectories)
            {
                TargetDirectories.Add(item);
            }

            Files.CollectionChanged += Files_CollectionChanged;
            TargetDirectories.CollectionChanged += TargetDirectories_CollectionChanged;
        }

        public static XDocument CreateNewSettingsFile(params string[] targetDirectories)
        {
            XDocument document = new XDocument();

            var root = new XElement("Patch");
            document.Add(root);

            root.Add(new XElement("Notes", "Add notes for your patch here."));

            var targetDirectoriesElement = new XElement("TargetDirectories");
            foreach (var item in targetDirectories)
            {
                targetDirectoriesElement.Add(new XElement("Target", item));
            }
            root.Add(targetDirectoriesElement);

            return document;
        }

        private void Files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("RootDirectory");
            RaisePropertyChanged("RelativeFiles");
            RaisePropertyChanged("CanPatch");
        }

        private void TargetDirectories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("CanPatch");
        }

        public ObservableCollection<string> Files { get; private set; }

        public ObservableCollection<string> TargetDirectories { get; private set; }

        public IEnumerable<string> RelativeFiles
        {
            get
            {
                string rootDirectory = RootDirectory;

                var trimmed = Files.Select(x => x.Substring(rootDirectory.Length).Trim(Path.DirectorySeparatorChar));
                return trimmed;
            }
        }

        public bool CanPatch { get { return Files.Any() && TargetDirectories.Any(); } }

        public string Notes { get; set; }

        public string RootDirectory
        {
            get
            {
                if (!Files.Any())
                {
                    return string.Empty;
                }

                if (Files.Select(x => Path.GetPathRoot(x)).Distinct().Count() != 1)
                {
                    throw new Exception("Files must come from the same drive");
                }

                var directories = Files.Select(x => new FileInfo(x).DirectoryName).OrderBy(x => x.Length).ToArray();

                var firstDirectory = directories.First();
                var rootDirectory = string.Empty;

                for (int i = 0; i <= firstDirectory.Length; i++)
                {
                    var currentPart = firstDirectory.Substring(0, i);
                    bool isRoot = directories.All(x => x.StartsWith(currentPart, StringComparison.InvariantCultureIgnoreCase));
                    if (!isRoot)
                    {
                        break;
                    }

                    rootDirectory = currentPart;
                }

                return rootDirectory.Trim(Path.DirectorySeparatorChar);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            var d = PropertyChanged;
            if (d != null && !string.IsNullOrWhiteSpace(propertyName))
            {
                d(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
