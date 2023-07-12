# CE.SingleSolutionMigrator

Provides a simple API that will take a given solution and convert assembly references to project references if source is available.  The root path to the source is required in order to determine that.  Handles multitargeted projects that use net472 and net6.0-windows in order to handle C++ projects that target these frameworks.

The solution is assembed to include all the necessary projects in order to correctly build.  Use CE.SolutionBuilder to build a solution file given the project information.
