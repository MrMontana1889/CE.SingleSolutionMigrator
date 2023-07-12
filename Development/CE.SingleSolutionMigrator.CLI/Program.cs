using CE.SingleSolutionMigrator.CLI;
using CommandLine;

Parser.Default.ParseArguments<Arguments>(args)
    .WithParsed<Arguments>(o =>
    {
        Console.WriteLine(o.RootPath);
    });