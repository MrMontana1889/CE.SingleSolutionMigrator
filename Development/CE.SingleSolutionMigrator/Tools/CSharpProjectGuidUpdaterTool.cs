// ProjectGuidUpdaterTool.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Tools
{
    public class CSharpProjectGuidUpdaterTool
    {
        #region Constructor
        public CSharpProjectGuidUpdaterTool()
        {

        }
        #endregion

        #region Public Methods
        public void Execute(string directory)
        {
            var directories = Directory.GetDirectories(directory, "Development*", SearchOption.AllDirectories);

            using (ProgressBar pbar = new ProgressBar(directories.Length, "Processing projects...",
                new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Red,
                    BackgroundColor = ConsoleColor.White,
                    ForegroundColorDone = ConsoleColor.Blue,
                    CollapseWhenFinished = true,
                }))
            {
                foreach (var dir in directories)
                {
                    pbar.Tick();
                    pbar.Message = dir;

                    if (dir.Contains(@"\Bentley."))
                        continue;

                    var files = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(dir, "*.vcxproj", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(dir, "*.sln", SearchOption.AllDirectories))).ToList();

                    IDictionary<string, Guid> projectGuids = new Dictionary<string, Guid>(files.Count);

                    foreach (var file in files)
                    {
                        string ext = Path.GetExtension(file);
                        switch (ext)
                        {
                            case ".csproj":
                                UpdateCSharpProject(file, projectGuids);
                                break;
                            case ".vcxproj":
                                UpdateCPPProject(file, projectGuids);
                                break;
                            case ".sln":
                                UpdateSolution(file, projectGuids);
                                break;
                        }
                    }
                }

                pbar.Message = "Update complete.";
            }
        }
        #endregion

        #region Private Methods
        private void UpdateCSharpProject(string file, IDictionary<string, Guid> projectGuids)
        {
            ProjectCollection projectCollection = new ProjectCollection(ToolsetDefinitionLocations.Default);
            ProjectRootElement project = null;
            try { project = ProjectRootElement.Open(file, projectCollection, true); }
            catch { project = null; }
            if (project == null) return;

            bool projectGuidFound = false;
            Guid projectGuid = Guid.Empty;
            foreach (var propertyGroup in project.PropertyGroups)
            {
                foreach (var property in propertyGroup.Properties)
                {
                    if (property.ElementName == "ProjectGuid")
                    {
                        projectGuid = new Guid(property.Value);
                        projectGuidFound = true;
                        break;
                    }
                }
                if (projectGuidFound)
                    break;
            }

            if (!projectGuidFound)
            {
                var propertyGroup = project.PropertyGroups.FirstOrDefault();
                if (propertyGroup == null)
                    propertyGroup = project.AddPropertyGroup();

                projectGuid = Guid.NewGuid();
                propertyGroup.AddProperty("ProjectGuid", $"{{{projectGuid}}}".ToUpperInvariant());
            }

            if (projectGuid != Guid.Empty)
            {
                projectGuids.Add(file, projectGuid);
            }

            if (project.HasUnsavedChanges)
                project.Save();
        }
        private void UpdateCPPProject(string file, IDictionary<string, Guid> projectGuids)
        {
            ProjectCollection projectCollection = new ProjectCollection(ToolsetDefinitionLocations.Default);
            ProjectRootElement project = ProjectRootElement.Open(file, projectCollection, true);

            foreach (var itemGroup in project.ItemGroups)
            {
                foreach (var item in itemGroup.Items)
                {
                    if (item.ElementName == "ProjectReference")
                    {
                        foreach (var proj in projectGuids)
                        {
                            var refFilename = Path.GetFileName(item.Include);
                            if (Path.GetExtension(refFilename) != ".csproj")
                                continue;       // Ignore vcxproj in this case.

                            var filename = Path.GetFileName(proj.Key);
                            if (filename.ToLowerInvariant() == refFilename.ToLowerInvariant())
                            {
                                foreach (var meta in item.Metadata)
                                {
                                    if (meta.ElementName == "Project")
                                    {
                                        meta.Value = $"{{{proj.Value}}}".ToUpperInvariant();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (project.HasUnsavedChanges)
                project.Save();
        }
        private void UpdateSolution(string file, IDictionary<string, Guid> projectGuids)
        {
            string sln = $"{file}.bak";

            IDictionary<Guid, Guid> oldGuidToNewGuid = new Dictionary<Guid, Guid>(projectGuids.Count);

            MakeSolutionFilePass1(file, projectGuids, sln, oldGuidToNewGuid);
            MakeSolutionFilePass2(file, projectGuids, sln, oldGuidToNewGuid);

            File.Copy(sln, file, true);
            File.Delete(sln);
        }

        private void MakeSolutionFilePass1(string file, IDictionary<string, Guid> projectGuids, string sln, IDictionary<Guid, Guid> oldGuidToNewGuid)
        {
            using (FileStream readStream = new FileStream(file, FileMode.Open))
            {
                using (FileStream writeStream = new FileStream(sln, FileMode.Create))
                {
                    using (StreamReader reader = new StreamReader(readStream, Encoding.UTF8))
                    {
                        using (StreamWriter writer = new StreamWriter(writeStream, Encoding.UTF8))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                if (line == null) break;

                                if (line.TrimStart().StartsWith("Project(\"{9A19103F-16F7-4668-BE54-9A1E7A4F7556}\")"))
                                {
                                    var tokens1 = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens1 != null && tokens1.Length != 2)
                                    {
                                        writer.WriteLine(line);
                                        continue;
                                    }

                                    var tokens2 = tokens1[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var item in projectGuids)
                                    {
                                        string filename = Path.GetFileName(item.Key);
                                        string refFilename = Path.GetFileName(tokens2[1]).Replace("\"", "");
                                        if (filename.ToLowerInvariant() == refFilename.ToLowerInvariant())
                                        {
                                            Guid oldGuid = Guid.Parse(tokens2[2].Replace("\"", ""));
                                            line = line.Replace($"{{{oldGuid}}}".ToUpperInvariant(), $"{{{item.Value}}}".ToUpperInvariant());
                                            oldGuidToNewGuid.Add(oldGuid, item.Value);
                                        }
                                    }
                                }

                                if (line.TrimStart().StartsWith("{"))
                                {
                                    foreach (var item in oldGuidToNewGuid)
                                    {
                                        if (line.Contains($"{{{item.Key}}}".ToUpperInvariant()))
                                        {
                                            string oldGuid = $"{{{item.Key}}}".ToUpperInvariant();
                                            string newGuid = $"{{{item.Value}}}".ToUpperInvariant();
                                            line = line.Replace(oldGuid, newGuid);
                                            break;
                                        }
                                    }
                                }

                                writer.WriteLine(line);
                            }
                        }
                    }
                }
            }
        }
        private void MakeSolutionFilePass2(string file, IDictionary<string, Guid> projectGuids, string sln, IDictionary<Guid, Guid> oldGuidToNewGuid)
        {
            using (FileStream readStream = new FileStream(file, FileMode.Open))
            {
                using (FileStream writeStream = new FileStream(sln, FileMode.Create))
                {
                    using (StreamReader reader = new StreamReader(readStream, Encoding.UTF8))
                    {
                        using (StreamWriter writer = new StreamWriter(writeStream, Encoding.UTF8))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                if (line == null) break;

                                if (line.TrimStart().StartsWith("{"))
                                {
                                    foreach (var item in oldGuidToNewGuid)
                                    {
                                        string oldGuid = $"{{{item.Key}}}".ToUpperInvariant();
                                        string newGuid = $"{{{item.Value}}}".ToUpperInvariant();
                                        line = line.Replace(oldGuid, newGuid);
                                    }
                                }

                                writer.WriteLine(line);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
