// BaseMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace CE.SingleSolutionMigrator.Migrators
{
    public abstract class BaseMigrator
    {
        #region Constructor
        public BaseMigrator(PerforceFacade? p4)
        {
            P4 = p4;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Gets the assembly name for the given project file
        /// </summary>
        /// <param name="filename">The full path and filename of the project</param>
        /// <returns>
        /// The output assembly name, sans extension, and including the
        /// target framework if a C++ project
        /// </returns>
        /// <exception cref="ApplicationException">Thrown if the extension is not recognized</exception>
        protected string GetAssemblyName(string filename)
        {
            string ext = Path.GetExtension(filename);
            ProjectRootElement project = ProjectRootElement.Open(filename, ProjectCollection);

            if (ext == CSPROJ)
            {
                foreach (var propertyGroup in project.PropertyGroups)
                {
                    foreach (var property in propertyGroup.Properties)
                    {
                        if (property.ElementName == ASSEMBLYNAME)
                            return property.Value;
                    }
                }

                // AssemblyName is not specifically stated in project file.
                // Return the name of the project without the extension.
                return Path.GetFileNameWithoutExtension(filename);
            }
            else if (ext == VCXPROJ)
            {
                foreach (var propertyGroup in project.PropertyGroups)
                {
                    if (propertyGroup.Condition.Contains("Debug|"))
                        continue;

                    foreach (var property in propertyGroup.Properties)
                    {
                        if (property.ElementName == TARGETNAME)
                        {
                            string targetName = property.Value;
                            if (targetName.Contains("$(ProjectName)"))
                            {
                                targetName = targetName.Replace("$(ProjectName)",
                                    Path.GetFileNameWithoutExtension(filename));
                            }
                            else if (targetName == "$(TargetName)")
                            {
                                targetName = targetName.Replace("$(TargetName)",
                                    Path.GetFileNameWithoutExtension(filename));
                            }
                            string targetFramework = GetTargetFramework(filename);
                            if (!string.IsNullOrEmpty(targetFramework))
                                targetName = $"{targetName}.{targetFramework}";
                            return targetName;
                        }
                    }
                }

                return Path.GetFileNameWithoutExtension(filename);
            }

            throw new ApplicationException($"Unknown extension: {ext}");
        }
        protected string GetTargetFramework(string filename)
        {
            if (filename.Contains(NET6))
                return NET6;
            if (filename.Contains(NET472))
                return NET472;

            return string.Empty;
        }
        #endregion

        #region Protected Properties
        protected PerforceFacade? P4 { get; }
        protected ProjectCollection ProjectCollection { get; } = new ProjectCollection(ToolsetDefinitionLocations.Default);
        #endregion

        #region Constants
        protected const string CSPROJ = ".csproj";
        protected const string VCXPROJ = ".vcxproj";
        protected const string NET6 = "net6.0-windows";
        protected const string NET472 = "net472";
        protected const string ASSEMBLYNAME = "AssemblyName";
        protected const string TARGETNAME = "TargetName";
        protected const string NOWARNELEMENT = "NoWarn";
        protected const string OUTPUTPATHELEMENT = "OutputPath";
        protected const string INTERMEDIATEOUTPUTPATHELEMENT = "IntermediateOutputPath";
        protected const string DOCUMENTATIONFILEELEMENT = "DocumentationFile";
        protected const string OLDOUTPUTPATH = @"('$(MSBuildProjectDirectory)\..\..\..\Output\$(MSBuildProjectName)\bin\$(Configuration)\'))";
        protected const string NEWOUTPUTPATH = @"('$(MSBuildProjectDirectory)\..\..\..\..\..\Solutions\Output\$(MSBuildProjectName)\bin\$(Configuration)\'))";
        protected const string OLDINTERMEDIATEOUTPUTPATH = @"('$(MSBuildProjectDirectory)\..\..\..\Output\$(MSBuildProjectName)\obj'))";
        protected const string NEWINTERMEDIATEOUTPUTPATH = @"('$(MSBuildProjectDirectory)\..\..\..\..\..\Solutions\Output\$(MSBuildProjectName)\obj'))";
        protected const string NOWARN = "$(NoWarn)";
        protected const string REFERENCEELEMENT = "Reference";
        protected const string HINTPATHELEMENT = "HintPath";
        protected const string PROJECTREFERENCEELEMENT = "ProjectReference";
        #endregion
    }
}
