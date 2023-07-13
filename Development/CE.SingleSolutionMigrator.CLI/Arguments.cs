// Arguments.cs
// Copyright (c) Kris Culin. All Rights Reserved.

using CommandLine;

namespace CE.SingleSolutionMigrator.CLI
{
    internal class Arguments
    {
        [Option('r', longName: "RootPath", Required = true, HelpText = "The full path to the source code being migrated.")]
        public required string RootPath { get; set; }
        [Option('s', longName: "Solution", Required = true, HelpText = "The full path and filename of the solution to migrate.")]
        public required string Solution { get; set; }
        [Option('o', longName: "CleanOutput", Required = false, Default = "true", HelpText = "If true, deletes the output folders from all components and Solutions folder.")]
        public bool CleanOutput { get; set; }
        [Option('d', longName: "DeleteAssemblies", Required = false, Default = "true", HelpText = "Specifies whether assemblies in _LKG folders are deleted.")]
        public bool DeleteAssemblies { get; set; }

        [Option('p', longName: "Usep4", Group = "Perforce", Default = false, HelpText = "True to use p4 to check out files.")]
        public bool UseP4 { get; set; }
        [Option('v', longName: "Server", Group = "Perforce", Default = "", HelpText = "The perforce server")]
        public string ServerName { get; set; } = string.Empty;
        [Option('u', longName: "UserName", Group = "Perforce", Default = "", HelpText = "The user name for logging into p4.")]
        public string UserName { get; set; } = string.Empty;
        [Option('w', longName: "Password", Group = "Perforce", Default = "", HelpText = "The password to use to log into p4.")]
        public string Password { get; set; } = string.Empty;
        [Option('w', longName: "Workspace", Group = "Perforce", Default = "", HelpText = "The workspace to use for checkout.")]
        public string Workspace { get; set; } = string.Empty;
    }
}
