using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows;

namespace Patcher
{
    public static class Utility
    {
        public static void CompileExecutable(PatchOptions options)
        {
            string embeddedResourceFile = Path.GetTempFileName();

            try
            {
                var filesWithoutDirectories = options.Files.Select(x => new FileInfo(x).Name).ToArray();

                var compilerParameters = new CompilerParameters
                {
                    GenerateExecutable = true,
                    OutputAssembly = options.OutputAssembly,
                    CompilerOptions = "/target:winexe", // Make it a WPF app as opposed to a console app
                };

                var input = new Dictionary<string, byte[]>();
                using (ResourceWriter rw = new ResourceWriter(embeddedResourceFile))
                {
                    foreach (var resourceFile in options.Files)
                    {
                        // Trim off the redundant data
                        string pathedResourceFile = new FileInfo(resourceFile).FullName.Substring(options.RootDirectory.Length - options.RootDirectory.Substring(options.RootDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1).Length);

                        input[pathedResourceFile] = File.ReadAllBytes(Path.Combine(options.RootDirectory, resourceFile));
                        rw.AddResourceData(pathedResourceFile, "ResourceTypeCode.ByteArray", input[pathedResourceFile]);
                    }
                }

                using (ResourceReader item = new ResourceReader(embeddedResourceFile))
                {
                    var enumerator = item.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var key = (string)enumerator.Key;
                        string resourceType;
                        byte[] resourceData;

                        item.GetResourceData(key, out resourceType, out resourceData);

                        if (resourceData.Length != input[key].Length)
                        {
                            // Output retrieved not the same as input
                            throw new ApplicationException();
                        }

                        for (int i = 0; i < input[key].Length; i++)
                        {
                            if (input[key][i] != resourceData[i])
                            {
                                // Output retrieved not the same as input
                                throw new ApplicationException();
                            }
                        }

                        Debug.WriteLine(string.Join(" ", resourceData));
                    }
                }

                compilerParameters.EmbeddedResources.Add(embeddedResourceFile);

                var assemblies = typeof(PatchTemplate).Assembly.GetReferencedAssemblies().ToList();
                var assemblyLocations = assemblies.Select(a => Assembly.ReflectionOnlyLoad(a.FullName).Location).ToList();
                assemblyLocations.Add(typeof(PatchTemplate).Assembly.Location);
                compilerParameters.ReferencedAssemblies.AddRange(assemblyLocations.ToArray());

                // Hack to get WindowsBase into the referenced assemblies list - don't want to do any work to figure this out
                Console.WriteLine(typeof(AttachedPropertyBrowsableAttribute).Name);

                // Read the source template
                string templateFileName = Path.Combine(new FileInfo(typeof(MainWindow).Assembly.Location).DirectoryName, "Template.cs");

                #region Process the file replacements to build valid code

                var originalLines = File.ReadAllLines(templateFileName);
                StringBuilder total = new StringBuilder();
                var newLines = new List<string>();
                for (int i = 0; i < originalLines.Length; i++)
                {
                    var line = originalLines[i];
                    line = line.Replace(" Debug.", " System.Console.");
                    line = line.Replace("<<PATCH_NOTE_HEADER>>", options.PatchNoteHeader);
                    line = line.Replace("<<PATCH_NOTE_CONTENT>>", options.PatchNoteContent);

                    if (line.Contains("public static void CreatePatch()"))
                    {
                        line = line.Replace("public static void CreatePatch()", "[System.STAThread]static void Main(string[] args)");
                    }
                    else if (line.EndsWith("// TEXT: Target directories"))
                    {
                        line = string.Format("private static string[] targetDirectories = new string[]{{ {0} }};", string.Join(", ", options.TargetDirectories.Select(x => string.Format("@\"{0}\"", x))));
                    }

                    // Add the line
                    newLines.Add(line);
                }

                string source = string.Join(Environment.NewLine, newLines);

                #endregion

                var provider = CodeDomProvider.CreateProvider("CSharp");
                var compilerResults = provider.CompileAssemblyFromSource(compilerParameters, source);
                if (compilerResults.Errors.Count > 0)
                {
                    throw new Exception(compilerResults.Errors[0].ErrorText);
                }
            }
            finally
            {
                File.Delete(embeddedResourceFile);
            }
        }
    }
}
