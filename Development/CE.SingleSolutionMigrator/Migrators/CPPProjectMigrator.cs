// CPPProjectMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Extensions;
using CE.SingleSolutionMigrator.Support;
using Microsoft.Build.Construction;
using NDepend.Path;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Migrators
{
    public class CPPProjectMigrator : BaseProjectMigrator
    {
        #region Constructor
        public CPPProjectMigrator(PerforceFacade p4, ProjectInSolution projectInSolution,
            IDictionary<string, Guid> projectToGuid, IDictionary<string, ProjectInSolution> assemblyToProject)
            : base(p4, projectInSolution, projectToGuid, assemblyToProject)
        {
        }
        #endregion

        #region Public Methods
        public override void Migrate(ProgressBar pbar, string rootPath, string filename)
        {
            ProjectRootElement project = ProjectRootElement.Open(filename);

            bool isNETProject = project.FullPath.Contains($"{NET472}", StringComparison.OrdinalIgnoreCase) ||
                project.FullPath.Contains($"{NET6}", StringComparison.OrdinalIgnoreCase);

            if (isNETProject)
            {
                RemoveMSBuildTarget(pbar, project);
            }

            using (var spawn = pbar.Spawn(project.PropertyGroups.Count, "Updating output location...",
                    new ProgressBarOptions
                    {
                        CollapseWhenFinished = true,
                        ProgressBarOnBottom = true,
                    }))
            {
                const string OUTDIR = @"$(SolutionDir)..\Output\$(ProjectName)\$(Platform)\$(Configuration)\";
                foreach (var propertyGroup in project.PropertyGroups)
                {
                    spawn.Tick();

                    if (propertyGroup.Label == "Globals")
                    {
                        bool projectNameFound = false;
                        foreach (var property in propertyGroup.Properties)
                        {
                            if (property.ElementName == CPPOUTDIRELEMENT)
                                property.Parent.RemoveChild(property);
                            if (property.ElementName == CPPINTERMEDIATEDIRELEMENT)
                                property.Parent.RemoveChild(property);
                            if (property.ElementName == PROJECTNAMEELEMENT)
                                projectNameFound = true;
                        }

                        if (!projectNameFound)
                        {
                            string projectName = Path.GetFileNameWithoutExtension(project.FullPath);
                            propertyGroup.AddProperty(PROJECTNAMEELEMENT, projectName);
                        }

                        propertyGroup.AddProperty(CPPOUTDIRELEMENT, OUTDIR);
                        propertyGroup.AddProperty(CPPINTERMEDIATEDIRELEMENT, OUTDIR);
                    }
                    else
                    {
                        foreach (var property in propertyGroup.Properties)
                        {
                            if (property.ElementName == CPPOUTDIRELEMENT)
                                property.Parent.RemoveChild(property);
                            if (property.ElementName == CPPINTERMEDIATEDIRELEMENT)
                                property.Parent.RemoveChild(property);
                        }
                    }
                }
            }

            if (!project.FullPath.Contains($"Haestad.Licensing{VCXPROJ}", StringComparison.OrdinalIgnoreCase))
            {
                using (var spawn = pbar.Spawn(project.Targets.Count, "Processing targets...",
                    new ProgressBarOptions
                    {
                        CollapseWhenFinished = true,
                        ProgressBarOnBottom = true,
                    }))
                {
                    foreach (var target in project.Targets)
                    {
                        spawn.Tick();

                        if (target.Name == "CopyNET472")
                            continue;

                        foreach (var task in target.Tasks)
                        {
                            if (task.Name == "CopyToLastKnownGood")
                                task.Parent.RemoveChild(task);
                        }

                        if (target.Tasks.Count == 0)
                            target.Parent.RemoveChild(target);
                    }
                }
            }

            AddHmiCoreLibReferenceIfNeeded(project);
            AddGTLReferenceIfNeeded(project);

            string targetFramework = GetTargetFramework(project.FullPath);

            if (!string.IsNullOrEmpty(targetFramework))
            {
                foreach (var itemGroup in project.ItemGroups)
                {
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

                            if (item.ElementName == "Reference")
                            {
                                string hintPath = string.Empty;
                                foreach (var meta in item.Metadata)
                                {
                                    if (meta.ElementName == "HintPath")
                                    {
                                        hintPath = meta.Value;
                                        break;
                                    }
                                }

                                if (!string.IsNullOrEmpty(hintPath))
                                {
                                    string assemblyName = Path.GetFileNameWithoutExtension(hintPath.Replace("$(TargetFramework)", targetFramework).Replace("$(Platform)", "x64"));
                                    if (AssemblyToProject.TryGetValue(assemblyName, out ProjectInSolution referencedProject))
                                    {
                                        ProjectRootElement referencedProjectRoot = ProjectRootElement.Open(referencedProject.AbsolutePath);

                                        var projectAbsolutePath = project.DirectoryPath.ToAbsoluteDirectoryPath();
                                        var referencedProjectAbsolutePath = referencedProjectRoot.DirectoryPath.ToAbsoluteDirectoryPath();
                                        var referencedProjectRelativePath = referencedProjectAbsolutePath.GetRelativePathFrom(projectAbsolutePath);

                                        var referencedProjectRelativeFilePath = Path.Combine($"{referencedProjectRelativePath}",
                                            Path.GetFileName(referencedProject.AbsolutePath));

                                        var reference = itemGroup.AddItem("ProjectReference", $@"$(MSBuildProjectDirectory)\{referencedProjectRelativeFilePath}");
                                        reference.AddMetadata("Project", $"{referencedProject.ProjectGuid.ToUpperInvariant()}");
                                        item.Parent.RemoveChild(item);
                                    }
                                    else
                                    {
                                        assemblyName = Path.GetFileNameWithoutExtension(hintPath);
                                        assemblyName = $"{assemblyName}.{targetFramework}";
                                        if (AssemblyToProject.TryGetValue(assemblyName, out ProjectInSolution cppReferencedProject))
                                        {
                                            ProjectRootElement cppReferencedProjectRoot = ProjectRootElement.Open(cppReferencedProject.AbsolutePath);

                                            var projectAbsolutePath = project.DirectoryPath.ToAbsoluteDirectoryPath();
                                            var referencedProjectAbsolutePath = cppReferencedProjectRoot.DirectoryPath.ToAbsoluteDirectoryPath();
                                            var referencedProjectRelativePath = referencedProjectAbsolutePath.GetRelativePathFrom(projectAbsolutePath);

                                            var referencedProjectRelativeFilePath = Path.Combine($"{referencedProjectRelativePath}",
                                                Path.GetFileName(cppReferencedProject.AbsolutePath));

                                            var reference = itemGroup.AddItem("ProjectReference", $@"$(MSBuildProjectDirectory)\{referencedProjectRelativeFilePath}");
                                            reference.AddMetadata("Project", $"{cppReferencedProject.ProjectGuid.ToUpperInvariant()}");
                                            item.Parent.RemoveChild(item);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var spawn = pbar.Spawn(project.ItemGroups.Count, "Updating project guids...",
                new ProgressBarOptions
                {
                    CollapseWhenFinished = true,
                    ProgressBarOnBottom = true,
                }))
            {
                foreach (var itemGroup in project.ItemGroups)
                {
                    spawn.Message = Path.GetFileNameWithoutExtension(project.FullPath);
                    spawn.Tick();

                    foreach (var item in itemGroup.Items)
                    {
                        if (item.ElementName == "ProjectReference")
                        {
                            foreach (var meta in item.Metadata)
                            {
                                if (meta.ElementName == "Project")
                                {
                                    string projectName = Path.GetFileName(item.Include);
                                    if (ProjectToGuid.TryGetValue(projectName, out var projectGuid))
                                    {
                                        if (meta.Value.ToUpperInvariant() != projectGuid.ToFormattedGuid())
                                            meta.Value = projectGuid.ToFormattedGuid();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            AddReferencePath(project);

            if (project.HasUnsavedChanges)
                project.Save();
        }
        #endregion

        #region Private Methods
        private void AddReferencePath(ProjectRootElement project)
        {
            string referencePaths = GetReferencePaths(project);

            foreach (var propertyGroup in project.PropertyGroups)
            {
                if (propertyGroup.Condition.Contains("'$(Configuration)|$(Platform)'=="))
                {
                    bool referencePathFound = false;
                    foreach (var property in propertyGroup.Properties)
                    {
                        if (property.ElementName == "ReferencePath")
                        {
                            property.Value = $"{referencePaths};$(ReferencePath)";
                            referencePathFound = true;
                            break;
                        }
                    }
                    if (!referencePathFound)
                    {
                        propertyGroup.AddProperty("ReferencePath", $"{referencePaths};$(ReferencePath)");
                    }
                }
            }
        }

        private string GetReferencePaths(ProjectRootElement project)
        {
            string targetFramework = GetTargetFramework(project.FullPath);
            List<string> references = new List<string>();
            foreach (var itemGroup in project.ItemGroups)
            {
                foreach (var item in itemGroup.Items)
                {
                    var include = item.Include;
                    string projectFileName = Path.GetFileName(include);
                    string projectName = Path.GetFileNameWithoutExtension(projectFileName);
                    if (Path.GetExtension(projectFileName) == CSPROJ)
                    {
                        string referencePath = $@"$(SolutionDir)..\Output\{projectName}\bin\$(Configuration)\{targetFramework}";
                        references.Add(referencePath);
                    }
                    else if (Path.GetExtension(projectFileName) == VCXPROJ)
                    {
                        string referencePath = $@"$(SolutionDir)..\Output\{projectName}\$(Platform)\$(Configuration)";
                        references.Add(referencePath);
                    }
                }
            }

            return string.Join(";", references);
        }

        private void AddHmiCoreLibReferenceIfNeeded(ProjectRootElement project)
        {
            if ((project.FullPath.Contains("Haestad.Calculations.Pressure.ResultsReader", StringComparison.OrdinalIgnoreCase) ||
                project.FullPath.Contains("DWNetworkParser", StringComparison.OrdinalIgnoreCase)) &&
                !project.FullPath.Contains("DWNetworkParserLib", StringComparison.OrdinalIgnoreCase))
            {
                bool hmiCoreLibAdded = false;
                foreach (var itemGroup in project.ItemGroups)
                {
                    foreach (var item in itemGroup.Items)
                    {
                        if (item.Include.Contains("HmiCoreLib"))
                        {
                            hmiCoreLibAdded = true;
                            break;
                        }
                    }
                    if (hmiCoreLibAdded)
                        break;
                }

                if (!hmiCoreLibAdded)
                {
                    if (ProjectToGuid.TryGetValue($"HmiCoreLib{VCXPROJ}", out Guid coreLibGuid))
                    {
                        if (AssemblyToProject.TryGetValue($"HmiCoreLib", out ProjectInSolution hmiCoreLibProject))
                        {
                            string currentDir = Environment.CurrentDirectory;
                            try
                            {
                                var coreAbsolutePath = hmiCoreLibProject.AbsolutePath.ToAbsoluteDirectoryPath();
                                var resultsPath = project.FullPath.ToAbsoluteDirectoryPath();
                                var relativePath = coreAbsolutePath.GetRelativePathFrom(resultsPath);

                                var relPath = $"{relativePath}".Replace(@"..\..\..\..\Haestad.CoreLib",
                                    @"..\..\..\..\Components\Haestad.CoreLib");

                                var itemGroup = project.AddItemGroup();
                                var reference = itemGroup.AddItem("ProjectReference", $"{relPath}");
                                reference.Label = "HmiCoreLib";
                                reference.AddMetadata("Project", hmiCoreLibProject.ProjectGuid);
                            }
                            finally
                            {
                                Environment.CurrentDirectory = currentDir;
                            }
                        }
                    }
                }

                //$(SolutionDir)..\Ouptut\HmiCoreLib\$(Platform)\$(Configuration);
                //$(SolutionDir)..\Output\GTL_static\$(Platform)\$(Configuration);
                //$(SolutionDir)..\Output\Haestad.DWNetworkParserLib\$(Platform)\$(Configuration);
                //%(AdditionalLibraryDirectories)

                // Check to see if the AdditionalLibraryDirectories contains the relative path to HmiCoreLib output using $(SolutionDir)
                // <ItemDefinitionGroup> -> <Link> -> <AdditionalLibraryDirectories>
                var coreLibPath = @"$(SolutionDir)..\Output\HmiCoreLib\$(Platform)\$(Configuration)";
                foreach (var itemDef in project.ItemDefinitionGroups)
                {
                    foreach (var def in itemDef.ItemDefinitions)
                    {
                        if (def.ElementName == "Link")
                        {
                            foreach (var meta in def.Metadata)
                            {
                                if (meta.ElementName == "AdditionalLibraryDirectories")
                                {
                                    if (!meta.Value.Contains(coreLibPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        List<string> libDirectories = new List<string>(meta.Value.Split(';', StringSplitOptions.RemoveEmptyEntries));
                                        libDirectories.Insert(0, coreLibPath);
                                        meta.Value = string.Join(";", libDirectories);
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
        private void AddGTLReferenceIfNeeded(ProjectRootElement project)
        {
            if ((project.FullPath.Contains("Haestad.Network", StringComparison.OrdinalIgnoreCase)
                || project.FullPath.Contains("DWNetworkParser", StringComparison.OrdinalIgnoreCase))
                && (!project.FullPath.Contains("NetworkLib") && !project.FullPath.Contains("DWNetworkParserLib", StringComparison.OrdinalIgnoreCase)))
            {
                bool gtlFound = false;
                foreach (var itemGroup in project.ItemGroups)
                {
                    foreach (var item in itemGroup.Items)
                    {
                        if (item.Include.Contains("GTL_static", StringComparison.OrdinalIgnoreCase))
                        {
                            gtlFound = true;
                            break;
                        }
                    }
                    if (gtlFound)
                        break;
                }

                if (!gtlFound)
                {
                    if (ProjectToGuid.TryGetValue($"GTL_static{VCXPROJ}", out Guid gtlGuid))
                    {
                        if (AssemblyToProject.TryGetValue("GTLstatic", out ProjectInSolution gtlProject))
                        {
                            string currentDir = Environment.CurrentDirectory;
                            try
                            {
                                Environment.CurrentDirectory = project.DirectoryPath;

                                var gtlAbsolutePath = gtlProject.AbsolutePath.ToAbsoluteDirectoryPath();
                                var networkAbsolute = project.FullPath.ToAbsoluteDirectoryPath();
                                var relativePath = gtlAbsolutePath.GetRelativePathFrom(networkAbsolute);

                                var relPath = $"{relativePath}".Replace(@"\3rdParty\", @"\Aspen\3rdParty\");

                                var itemGroup = project.AddItemGroup();
                                var reference = itemGroup.AddItem("ProjectReference", $"{relPath}");
                                reference.Label = "GTL_static";
                                reference.AddMetadata("Project", gtlProject.ProjectGuid);
                            }
                            finally
                            {
                                Environment.CurrentDirectory = currentDir;
                            }
                        }
                    }
                }

                //$(SolutionDir)..\Output\GTL_static\$(Platform)\$(Configuration);
                var gtlLibPath = @"$(SolutionDir)..\Output\GTL_static\$(Platform)\$(Configuration)";

                //$(SolutionDir)..\Output\Haestad.DWNetworkParserLib\$(Platform)\$(Configuration);
                var parserLibPath = @"$(SolutionDir)..\Output\Haestad.DWNetworkParserLib\$(Platform)\$(Configuration)";

                foreach (var itemDef in project.ItemDefinitionGroups)
                {
                    foreach (var def in itemDef.ItemDefinitions)
                    {
                        if (def.ElementName == "Link")
                        {
                            foreach (var meta in def.Metadata)
                            {
                                if (meta.ElementName == "AdditionalLibraryDirectories")
                                {
                                    // check to make sure the GTL relative path is present.
                                    // Applies to Haestad.Network or Haestad.DWNetworkParser.
                                    if (!meta.Value.Contains(gtlLibPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        List<string> libDirectories = new List<string>(meta.Value.Split(';', StringSplitOptions.RemoveEmptyEntries));
                                        libDirectories.Insert(0, gtlLibPath);
                                        meta.Value = string.Join(";", libDirectories);
                                    }

                                    if (project.FullPath.Contains("Haestad.DWNetworkParser", StringComparison.OrdinalIgnoreCase) &&
                                        !project.FullPath.Contains("DWNetworkParserLib", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Only check for the DWNetworkParserLib path if the project is Haestad.DWNetworkParser.
                                        if (!meta.Value.Contains(parserLibPath, StringComparison.OrdinalIgnoreCase))
                                        {
                                            List<string> libDirectories = new List<string>(meta.Value.Split(';', StringSplitOptions.RemoveEmptyEntries));
                                            libDirectories.Insert(0, parserLibPath);
                                            meta.Value = string.Join(";", libDirectories);
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
        }
        #endregion

        #region Private Constants
        private const string CPPOUTDIRELEMENT = "OutDir";
        private const string CPPINTERMEDIATEDIRELEMENT = "IntDir";
        private const string PROJECTNAMEELEMENT = "ProjectName";
        #endregion
    }
}
