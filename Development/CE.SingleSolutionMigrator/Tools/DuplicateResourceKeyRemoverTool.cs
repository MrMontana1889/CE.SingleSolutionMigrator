// DuplicateResourceKeyRemoverTool.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using System.Xml;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Tools
{
    public class DuplicateResourceKeyRemoverTool
    {
        #region Constructor
        public DuplicateResourceKeyRemoverTool()
        {

        }
        #endregion

        #region Public Methods
        public void Execute(string rootPath, bool includeSubDirectories)
        {
            var resxFiles = Directory.GetFiles(rootPath, "*.resx",
                includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            using (var pbar = new ProgressBar(resxFiles.Length, "Removing duplicate keys...",
                new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Red,
                    BackgroundColor = ConsoleColor.White,
                    ForegroundColorDone = ConsoleColor.Blue,
                }))
            {
                foreach (var file in resxFiles)
                {
                    pbar.Tick(file);

                    var doc = new XmlDocument();
                    doc.Load(file);

                    var nodes = doc.SelectNodes("//data");
                    var keys = new HashSet<string>(nodes.Count);
                    using (var spawn = pbar.Spawn(nodes.Count, "Processing resx...",
                        new ProgressBarOptions
                        {
                            CollapseWhenFinished = true,
                        }))
                    {
                        bool wasModified = false;
                        foreach (XmlNode node in nodes)
                        {
                            spawn.Tick();

                            var key = node.Attributes["name"].Value;
                            if (!keys.Contains(key))
                                keys.Add(key);
                            else
                            {
                                node.ParentNode.RemoveChild(node);
                                wasModified = true;
                            }
                        }

                        if (wasModified)
                            doc.Save(file);
                    }
                }
            }
            #endregion
        }
    }
}
