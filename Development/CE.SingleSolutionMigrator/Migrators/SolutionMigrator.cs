// SolutionMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;
using Microsoft.Build.Construction;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Migrators
{
    public class SolutionMigrator : BaseMigrator
    {
        #region Constructor
        public SolutionMigrator(PerforceFacade? p4)
            : base(p4)
        {
        }
        #endregion

        #region Public Methods
        public void Migrate(string rootPath, string filename)
        {
            Console.WriteLine($"Starting solution migration...{Path.GetFileName(filename)}");

            if (!File.Exists(filename))
            {
                Console.WriteLine($"\tThe solution {filename} was not found.");
                return;
            }

            SolutionFile solution = SolutionFile.Parse(filename);

            using (var pbar = new ProgressBar(solution.ProjectsInOrder.Count, "Setting up for migration...",
                new ProgressBarOptions
                {
                    BackgroundCharacter = '-',
                    ForegroundColor = ConsoleColor.Red,
                    BackgroundColor = ConsoleColor.White,
                    ForegroundColorDone = ConsoleColor.Blue,
                    ProgressBarOnBottom = true,
                    CollapseWhenFinished = true,
                }))
            {
                foreach (var projectInSolution in solution.ProjectsInOrder)
                {
                    pbar.Tick();

                    if (projectInSolution.ProjectType == SolutionProjectType.SolutionFolder)
                        continue;

                    string assemblyName = GetAssemblyName(projectInSolution.AbsolutePath);
                    ProjectToGuid.Add(Path.GetFileName(projectInSolution.AbsolutePath), new Guid(projectInSolution.ProjectGuid));
                    AssemblyToProject.Add(assemblyName, projectInSolution);
                }

                pbar.Message = "Setup complete.";
            }

            Console.WriteLine("");

            using (var pbar = new ProgressBar(solution.ProjectsInOrder.Count, "Migrating projects...",
                new ProgressBarOptions
                {
                    BackgroundCharacter = '|',
                    ForegroundColor = ConsoleColor.Blue,
                    BackgroundColor = ConsoleColor.White,
                    ForegroundColorDone = ConsoleColor.Red,
                    ProgressBarOnBottom = true,
                    CollapseWhenFinished = true,
                }))
            {
                foreach (var projectInSolution in solution.ProjectsInOrder)
                {
                    pbar.Tick();

                    if (projectInSolution.ProjectType == SolutionProjectType.SolutionFolder)
                        continue;

                    pbar.Message = Path.GetFileNameWithoutExtension(projectInSolution.AbsolutePath);
                    BaseProjectMigrator migrator = GetProjectMigrator(projectInSolution);
                    migrator.Migrate(pbar, rootPath, projectInSolution.AbsolutePath);
                }

                pbar.Message = "Deleting solution output...";
                string solutionOutput = Path.GetDirectoryName(filename) + @"\..\Output";
                if (Directory.Exists(solutionOutput))
                {
                    try { Directory.Delete(solutionOutput, true); }
                    catch { }
                }

                pbar.Message = "Migration complete.";
            }
        }
        #endregion

        #region Private Methods
        private BaseProjectMigrator GetProjectMigrator(ProjectInSolution projectInSolution)
        {
            string ext = Path.GetExtension(projectInSolution.AbsolutePath);
            if (ext == CSPROJ)
                return new CSharpProjectMigrator(P4, projectInSolution, ProjectToGuid, AssemblyToProject);
            if (ext == VCXPROJ)
                return new CPPProjectMigrator(P4, projectInSolution, ProjectToGuid, AssemblyToProject);

            throw new ApplicationException($"Extension not recognized: {ext}");
        }
        #endregion

        #region Private Properties
        private IDictionary<string, Guid> ProjectToGuid { get; } = new Dictionary<string, Guid>();
        private IDictionary<string, ProjectInSolution> AssemblyToProject { get; } = new Dictionary<string, ProjectInSolution>();
        #endregion
    }
}
