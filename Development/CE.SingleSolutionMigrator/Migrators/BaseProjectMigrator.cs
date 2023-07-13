// BaseProjectMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;
using Microsoft.Build.Construction;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Migrators
{
    public abstract class BaseProjectMigrator : BaseMigrator
    {
        #region Constructor
        protected BaseProjectMigrator(PerforceFacade? p4, ProjectInSolution projectInSolution,
            IDictionary<string, Guid> projectToGuid, IDictionary<string, ProjectInSolution> assemblyToProject)
            : base(p4)
        {
            ProjectInSolution = projectInSolution;
            ProjectToGuid = projectToGuid;
            AssemblyToProject = assemblyToProject;
        }
        #endregion

        #region Public Methods
        public abstract void Migrate(ProgressBar pbar, string rootPath, string filename);
        #endregion

        #region Protected Methods
        protected void RemoveMSBuildTarget(ProgressBar pbar, ProjectRootElement project)
        {
            bool skipTargets = false;

            foreach (var import in project.Imports)
            {
                if (import.Project.Contains("Haestad.MSBuild.targets", StringComparison.OrdinalIgnoreCase))
                {
                    skipTargets = true;

                    if (Path.GetFileNameWithoutExtension(project.FullPath) == "BingMapsRESTToolkit.Standard")
                    {
                        skipTargets = false;
                        import.Parent.RemoveChild(import);
                    }
                    break;
                }
            }

            if (!skipTargets)
            {
                if (!project.FullPath.Contains(".Starter", StringComparison.OrdinalIgnoreCase))
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
            }
        }
        #endregion

        #region Protected Properties
        protected ProjectInSolution ProjectInSolution { get; }
        protected IDictionary<string, Guid> ProjectToGuid { get; }
        protected IDictionary<string, ProjectInSolution> AssemblyToProject { get; }
        #endregion
    }
}
