using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;

namespace Patcher
{
    public static class Utility
    {
        public static void CompileExecutable(string outputAssembly, string rootDirectory, string targetDirectory, params string[] resourceFiles)
        {
            string embeddedResourceFile = Path.GetTempFileName();

            try
            {
                var filesWithoutDirectories = resourceFiles.Select(x => new FileInfo(x).Name).ToArray();

                var compilerParameters = new CompilerParameters
                {
                    GenerateExecutable = true,
                    OutputAssembly = outputAssembly,
                    CompilerOptions = "/target:winexe",
                };

                var input = new Dictionary<string, byte[]>();
                using (ResourceWriter rw = new ResourceWriter(embeddedResourceFile))
                {
                    foreach (var resourceFile in resourceFiles)
                    {
                        // Trim off the redundant data
                        string pathedResourceFile = new FileInfo(resourceFile).FullName.Substring(rootDirectory.Length - rootDirectory.Substring(rootDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1).Length);

                        input[pathedResourceFile] = File.ReadAllBytes(Path.Combine(rootDirectory, resourceFile));
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
                string templateFileName = @"C:\Users\mullman\Desktop\Patcher\Patcher\Template.cs";

                string source = File.ReadAllText(templateFileName);
                source = source.Replace("public static void CreatePatch()", "[System.STAThread]static void Main(string[] args)");
                source = source.Replace(" Debug.", " System.Console.");
                source = source.Replace("private static string targetDirectory = string.Empty; // TEXT: Target directory", string.Format("private static string targetDirectory = @\"{0}\";", targetDirectory));

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
