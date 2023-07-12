// SolutionMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Migrators
{
    public class SolutionMigrator : BaseMigrator
    {
        #region Constructor
        public SolutionMigrator(PerforceFacade p4)
            : base(p4)
        {
        }
        #endregion

        #region Public Methods
        public override void Migrate(string rootPath, string filename)
        {
            Console.WriteLine($"Starting solution migration...{Path.GetFileName(filename)}");

            SolutionFile solution = SolutionFile.Parse(filename);

            using (var pbar = new ProgressBar(solution.ProjectsInOrder.Count, "Processing projects...",
                new ProgressBarOptions
                {
                    BackgroundCharacter = '-',
                    ForegroundColor = ConsoleColor.Red,
                    BackgroundColor = ConsoleColor.White,
                    ForegroundColorDone = ConsoleColor.Blue,
                    ProgressBarOnBottom = true,
                }))
            {
                foreach (var projectInSolution in solution.ProjectsInOrder)
                {
                    pbar.Tick();

                    if (projectInSolution.ProjectType == SolutionProjectType.SolutionFolder)
                        continue;

                    string assemblyName = GetAssemblyName(projectInSolution.AbsolutePath);
                }
            }
        }
        #endregion

        #region Private Methods
        #endregion
    }
}
