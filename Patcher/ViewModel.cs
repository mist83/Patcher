using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patcher
{
    public class ViewModel
    {
        public ViewModel()
        {
            Files = new ObservableCollection<string>();
        }

        public ObservableCollection<string> Files { get; private set; }

        public string TargetDirectory { get { return @"C:\Program Files (x86)\Adobe\Reader 11.0"; } } // Just a random program files directory

        public string RootDirectory { get; private set; }

        public bool Validate()
        {
            var files = Files;
            //files = new ObservableCollection<string>
            //{
            //    @"C:\Temp\abc.dll",
            //    @"C:\Program Files\MyProgram\Sub directory 2\temp_dir\f.dll",
            //    @"C:\Program Files\MyProgram\a.dll",
            //    @"C:\Program Files\MyProgram\b.dll",
            //    @"C:\Program Files\MyProgram\Sub directory 1\c.dll",
            //    @"C:\Program Files\MyProgram\Sub directory 1\d.dll",
            //    @"C:\Program Files\MyProgram\Sub directory 2\e.dll",
            //};

            var directories = files.Select(x => new FileInfo(x).DirectoryName).OrderBy(x => x.Length).ToArray();

            RootDirectory = directories[0];
            for (int i = 1; i < directories.Length; i++)
            {
                if (!directories[i].StartsWith(RootDirectory, StringComparison.InvariantCultureIgnoreCase))
                {
                    RootDirectory = null;
                    return false;
                }
            }

            return true;
        }
    }
}
