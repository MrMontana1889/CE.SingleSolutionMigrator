// TargetsMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;
using Microsoft.Build.Construction;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Migrators
{
    public class TargetsMigrator : BaseMigrator
    {
        #region Constructor
        public TargetsMigrator(PerforceFacade p4)
            : base(p4)
        {
        }
        #endregion

        #region Public Methods
        public void Migrate(string rootPath)
        {
            string targetsPath = Path.Combine(rootPath, @"Components\_Targets");
            UpdateTargetsFiles(targetsPath);

            targetsPath = Path.Combine(rootPath, @"Products\_Targets");
            UpdateTargetsFiles(targetsPath);

            string props = Path.Combine(rootPath, "Directory.Build.props");
            UpdateProps(props);
        }
        #endregion

        #region Private Methods
        private void UpdateTargetsFiles(string targetsPath)
        {
            if (!Directory.Exists(targetsPath)) return;

            string[] files = Directory.GetFiles(targetsPath, "*.targets", SearchOption.AllDirectories);
            using (var pbar = new ProgressBar(files.Length, "Processing targets files...",
                new ProgressBarOptions
                {
                    CollapseWhenFinished = true,
                    ProgressBarOnBottom = true,
                }))
            {
                foreach (string file in files)
                {
                    pbar.Tick();
                    pbar.Message = Path.GetFileName(file);

                    ProjectRootElement target = ProjectRootElement.Open(file);

                    bool pathsAdded = false;

                    foreach (var propertyGroup in target.PropertyGroups)
                    {
                        foreach (var property in propertyGroup.Properties)
                        {
                            if (Path.GetFileName(file).StartsWith("Test"))
                            {
                                if (property.ElementName == OUTPUTPATHELEMENT ||
                                    property.ElementName == INTERMEDIATEOUTPUTPATHELEMENT)
                                    pathsAdded = true;
                            }
                            else
                            {
                                if (property.ElementName == OUTPUTPATHELEMENT)
                                {
                                    property.Value = property.Value.Replace(@"'$(MSBuildProjectDirectory)\..\..\Output\",
                                        @"'$(MSBuildProjectDirectory)\..\..\..\..\Solutions\Output\");
                                }
                                if (property.ElementName == INTERMEDIATEOUTPUTPATHELEMENT)
                                {
                                    property.Value = property.Value.Replace(@"'$(MSBuildProjectDirectory)\..\..\Output\",
                                        @"'$(MSBuildProjectDirectory)\..\..\..\..\Solutions\Output\");
                                }
                            }
                        }

                        if (Path.GetFileName(file).StartsWith("Test") && !pathsAdded)
                        {
                            if (!pathsAdded)
                            {
                                propertyGroup.AddProperty("OutputPath",
                                    @"$([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)\..\..\Output\$(MSBuildProjectName)\bin\$(Platform)\$(Configuration)\'))");
                                propertyGroup.AddProperty("IntermediateOutputPath",
                                    @"$([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)\..\..\Output\$(MSBuildProjectName)\obj\$(Platform)\$(Configuration)\'))");
                                pathsAdded = true;
                            }
                        }
                    }

                    if (target.HasUnsavedChanges)
                        target.Save();
                }

                pbar.Message = "Process complete.";
            }
        }
        private void UpdateProps(string props)
        {
            ProjectRootElement project = ProjectRootElement.Open(props);

            foreach (var propertyGroup in project.PropertyGroups)
            {
                bool accelerateBuildsFound = false;
                bool outputPathUpdated = false;
                bool noWarnAdded = false;

                foreach (var property in propertyGroup.Properties)
                {
                    if (property.ElementName == "BaseIntermediateOutputPath")
                    {
                        if (!outputPathUpdated)
                        {
                            property.Value = property.Value.Replace(@"..\..\Output\$(MSBuildProjectName)\obj",
                                @"..\..\..\..\Solutions\Output\$(MSBuildProjectName)\obj");
                            outputPathUpdated = true;
                        }
                    }
                    if (property.ElementName == "AccelerateBuildsInVisualStudio")
                    {
                        accelerateBuildsFound = true;
                    }

                    if (!noWarnAdded)
                    {
                        propertyGroup.AddProperty("NoWarn", "MS83568");
                        noWarnAdded = true;
                    }
                }

                if (!accelerateBuildsFound)
                {
                    propertyGroup.AddProperty("AccelerateBuildsInVisualStudio", bool.TrueString);
                    propertyGroup.AddProperty("ProduceReferenceAssembly", bool.TrueString);
                }
            }

            if (project.HasUnsavedChanges)
                project.Save();
        }
        #endregion
    }
}
