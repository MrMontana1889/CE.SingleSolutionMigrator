// OutputCleanerTool.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Tools
{
    public class OutputCleanerTool
    {
        #region Constructor
        public OutputCleanerTool()
        {

        }
        #endregion

        #region Public Methods
        public void Execute(string rootPath, bool deleteAssemblies)
        {
            string thirdParties = Path.Combine(rootPath, "3rdParty");
            if (Directory.Exists(thirdParties))
            {
                List<string> thirdPartyComponents = new List<string>();
                thirdPartyComponents.Add("UPassau.GTL");

                foreach (var component in thirdPartyComponents)
                {
                    var componentPath = Path.Combine(thirdParties, component);
                    if (Directory.Exists(componentPath))
                    {
                        var assembliesPath = Path.Combine(componentPath, "Assemblies");
                        if (Directory.Exists(assembliesPath))
                        {
                            var files = Directory.GetFiles(assembliesPath, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                try { File.Delete(file); }
                                catch { }
                            }
                        }
                    }
                }
            }

            var componentsPath = Path.Combine(rootPath, "Components");
            if (Directory.Exists(componentsPath))
            {
                var directories = Directory.GetDirectories(componentsPath);
                using (var pbar = new ProgressBar(directories.Length, "Deleting output...",
                    new ProgressBarOptions
                    {
                        CollapseWhenFinished = true,
                        ProgressBarOnBottom = true,
                    }))
                {
                    foreach (var directory in directories)
                    {
                        pbar.Tick();
                        if (directory.Contains("Bentley."))
                            continue;

                        var outputPath = Path.Combine(directory, "Output");
                        if (Directory.Exists(outputPath))
                        {
                            try { Directory.Delete(outputPath, true); }
                            catch { }
                        }

                        var lkg = Path.Combine(directory, @"Assemblies\_LastKnownGood");
                        if (Directory.Exists(lkg))
                        {
                            var net472Path = Path.Combine(lkg, "net472");
                            if (Directory.Exists(net472Path))
                            {
                                if (deleteAssemblies)
                                {
                                    try { Directory.Delete(net472Path); }
                                    catch { }
                                }
                            }
                            else
                                Directory.CreateDirectory(net472Path);

                            var x86Path = Path.Combine(net472Path, "x86");
                            if (!Directory.Exists(x86Path))
                                Directory.CreateDirectory(x86Path);
                            var x64Path = Path.Combine(net472Path, "x64");
                            if (!Directory.Exists(x64Path))
                                Directory.CreateDirectory(x64Path);

                            var net6Path = Path.Combine(lkg, "net6.0-windows");
                            if (Directory.Exists(net6Path))
                            {
                                if (deleteAssemblies)
                                {
                                    try { Directory.Delete(net6Path); }
                                    catch { }
                                }
                            }
                            else
                                Directory.CreateDirectory(net6Path);

                            x86Path = Path.Combine(net6Path, "x86");
                            if (!Directory.Exists(x86Path))
                                Directory.CreateDirectory(x64Path);
                            x64Path = Path.Combine(net6Path, "x64");
                            if (!Directory.Exists(x64Path))
                                Directory.CreateDirectory(x64Path);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
