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
        public BaseMigrator(PerforceFacade p4)
        {
            P4 = p4;
        }
        #endregion

        #region Public Methods
        public abstract void Migrate(string rootPath, string filename);
        #endregion

        #region Protected Methods
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
                    foreach (var property in propertyGroup.Properties)
                    {
                        if (property.ElementName == TARGETNAME)
                            return $"{property.Value}.{GetTargetFramework(filename)}";
                    }
                }
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
        protected PerforceFacade P4 { get; }
        protected ProjectCollection ProjectCollection { get; } = new ProjectCollection(ToolsetDefinitionLocations.Default);
        #endregion

        #region Constants
        protected const string CSPROJ = ".csproj";
        protected const string VCXPROJ = ".vcxproj";
        protected const string NET6 = "net6.0-windows";
        protected const string NET472 = "net472";
        protected const string ASSEMBLYNAME = "AssemblyName";
        protected const string TARGETNAME = "TargetName";
        #endregion
    }
}
