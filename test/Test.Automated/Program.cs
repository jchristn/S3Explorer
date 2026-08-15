using Test.Shared;
using Touchstone.Cli;

// Touchstone CLI runner. Executes the shared suites, renders a colored tabular
// pass/fail/skip summary, supports optional JSON export via `--results <path>`,
// and returns a CI-friendly exit code (non-zero when any test fails).
var resultsPath = ParseResultsPath(args);
return await ConsoleRunner.RunAsync(S3ExplorerTestSuites.All, sink: null, resultsPath: resultsPath);

static string? ParseResultsPath(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == "--results")
            return args[i + 1];
    return null;
}
