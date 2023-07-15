// CSharpProjectMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;
using Microsoft.Build.Construction;
using NDepend.Path;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Migrators
{
    public class CSharpProjectMigrator : BaseProjectMigrator
    {
        #region Constructor
        public CSharpProjectMigrator(PerforceFacade p4, ProjectInSolution projectInSolution,
            IDictionary<string, Guid> projectToGuid, IDictionary<string, ProjectInSolution> assemblyToProject)
            : base(p4, projectInSolution, projectToGuid, assemblyToProject)
        {
        }
        #endregion

        #region Public Methods
        public override void Migrate(ProgressBar pbar, string rootPath, string filename)
        {
            ProjectRootElement project = ProjectRootElement.Open(filename);

            using (var spawn = pbar.Spawn(project.Imports.Count, "Processing imports...",
                new ProgressBarOptions
                {
                    CollapseWhenFinished = true,
                    ProgressBarOnBottom = true,
                }))
            {
                foreach (var import in project.Imports)
                {
                    spawn.Tick();

                    if (import.Project.Contains(@"..\..\..\..\") ||
                        import.Project.Contains("_Targets"))
                    {
                        if (import.Project.Contains("_Targets") && import.Project.Contains("Copy"))
                            import.Parent.RemoveChild(import);
                    }
                }
            }

            using (var spawn = pbar.Spawn(project.PropertyGroups.Count, "Processing properties...",
                new ProgressBarOptions
                {
                    CollapseWhenFinished = true,
                    ProgressBarOnBottom = true,
                }))
            {
                foreach (var propertyGroup in project.PropertyGroups)
                {
                    spawn.Tick();

                    foreach (var property in propertyGroup.Properties)
                    {
                        if (property.ElementName == NOWARNELEMENT)
                        {
                            if (!property.Value.Contains(NOWARN))
                            {
                                property.Value = property.Value.Replace(";", ",");
                                if (!property.Value.EndsWith(","))
                                    property.Value = property.Value + $",{NOWARN}";
                                else
                                    property.Value = property.Value + $"{NOWARN}";
                            }
                        }
                        if (property.ElementName == OUTPUTPATHELEMENT)
                        {
                            if (property.Value.Contains(OLDOUTPUTPATH))
                                property.Value = property.Value.Replace(OLDOUTPUTPATH, NEWOUTPUTPATH);
                        }
                        if (property.ElementName == INTERMEDIATEOUTPUTPATHELEMENT)
                        {
                            if (property.Value.Contains(OLDINTERMEDIATEOUTPUTPATH))
                                property.Value = property.Value.Replace(OLDINTERMEDIATEOUTPUTPATH, NEWINTERMEDIATEOUTPUTPATH);
                        }
                        if (property.ElementName == DOCUMENTATIONFILEELEMENT)
                            property.Parent.RemoveChild(property);
                    }
                }
            }

            RemoveMSBuildTarget(pbar, project);

            foreach (var itemGroup in project.ItemGroups)
            {
                string conditionFramework = string.Empty;
                if (project.FullPath.Contains(".Starter", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(itemGroup.Condition))
                    {
                        if (itemGroup.Condition.StartsWith("'$(TargetFramework)'", StringComparison.OrdinalIgnoreCase))
                        {
                            var tokens = itemGroup.Condition.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
                            if (tokens != null && tokens.Length == 2)
                            {
                                conditionFramework = tokens[1].Replace("'", "");
                            }
                        }
                    }
                }

                using (var spawn = pbar.Spawn(itemGroup.Items.Count, "Processing references...",
                    new ProgressBarOptions
                    {
                        CollapseWhenFinished = true,
                        ProgressBarOnBottom = true,
                    }))
                {
                    foreach (var item in itemGroup.Items)
                    {
                        spawn.Tick();

                        if (item.ElementName == REFERENCEELEMENT)
                        {
                            string hintPath = string.Empty;
                            foreach (var meta in item.Metadata)
                            {
                                if (meta.ElementName == HINTPATHELEMENT)
                                {
                                    hintPath = meta.Value;
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(hintPath))
                            {
                                string assemblyName = Path.GetFileNameWithoutExtension(hintPath.Replace("$(TargetFramework)", "net472").Replace("$(Platform)", "x64"));
                                if (AssemblyToProject.TryGetValue(assemblyName, out ProjectInSolution referencedProject))
                                {
                                    ProjectRootElement referencedProjectRoot = ProjectRootElement.Open(referencedProject.AbsolutePath);

                                    var projectAbsolutePath = project.DirectoryPath.ToAbsoluteDirectoryPath();
                                    var referencedProjectAbsolutePath = referencedProjectRoot.DirectoryPath.ToAbsoluteDirectoryPath();
                                    var referencedProjectRelativePath = referencedProjectAbsolutePath.GetRelativePathFrom(projectAbsolutePath);

                                    var referencedProjectRelativeFilePath = Path.Combine($"{referencedProjectRelativePath}",
                                        Path.GetFileName(referencedProject.AbsolutePath));

                                    itemGroup.AddItem(PROJECTREFERENCEELEMENT, $"{referencedProjectRelativeFilePath}");
                                    item.Parent.RemoveChild(item);
                                }
                                else
                                {
                                    assemblyName = Path.GetFileNameWithoutExtension(hintPath);
                                    assemblyName = $"{assemblyName}.{conditionFramework}";
                                    if (assemblyName.EndsWith("."))
                                    {
                                        conditionFramework = NET472;
                                        assemblyName = $"{assemblyName}{NET472}";
                                    }
                                    if (AssemblyToProject.TryGetValue(assemblyName, out ProjectInSolution cppReferencedProject))
                                    {
                                        ProjectRootElement referencedProjectRoot = ProjectRootElement.Open(cppReferencedProject.AbsolutePath);

                                        var projectAbsolutePath = project.DirectoryPath.ToAbsoluteDirectoryPath();
                                        var referencedProjectAbsolutePath = referencedProjectRoot.FullPath.ToAbsoluteDirectoryPath();
                                        var referencedProjectRelativePath = referencedProjectAbsolutePath.GetRelativePathFrom(projectAbsolutePath);

                                        var referencedProjectRelativeFilePath = $"{referencedProjectRelativePath}";

                                        if (!string.IsNullOrEmpty(conditionFramework))
                                            referencedProjectRelativeFilePath = referencedProjectRelativeFilePath.Replace($"{conditionFramework}", "$(TargetFramework)");

                                        itemGroup.AddItem(PROJECTREFERENCEELEMENT, $"{referencedProjectRelativeFilePath}");
                                        item.Parent.RemoveChild(item);
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
        #endregion
    }
}
