// Program.cs
// Copyright (c) 2023 Bentley Systems, Incorporated. All Rights Reserved.

using CE.SingleSolutionMigrator.CLI;
using CE.SingleSolutionMigrator.Migrators;
using CE.SingleSolutionMigrator.Tools;
using CommandLine;

Parser.Default.ParseArguments<Arguments>(args)
    .WithParsed<Arguments>(o =>
    {
        TargetsMigrator targetsMigrator = new TargetsMigrator(null);
        targetsMigrator.Migrate(o.RootPath);

        if (o.CleanOutput)
        {
            OutputCleanerTool outputCleaner = new OutputCleanerTool();
            outputCleaner.Execute(o.RootPath, o.DeleteAssemblies);
        }

        SolutionMigrator migrator = new SolutionMigrator(null);
        migrator.Migrate(o.RootPath, o.Solution);
    });