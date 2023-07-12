// Arguments.cs
// Copyright (c) Kris Culin. All Rights Reserved.

using CommandLine;

namespace CE.SingleSolutionMigrator.CLI
{
    internal class Arguments
    {
        [Option('r', longName: "RootPath", Required = true, HelpText = "The full path to the source code being migrated.")]
        public required string RootPath { get; set; }
    }
}
