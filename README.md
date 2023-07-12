# CE.SingleSolutionMigrator

Provides some API that will take a given solution and convert assembly references to project references if source is available.  The root path to the source is required in order to determine that.  Handles multitargeted projects that use net472 and net6.0-windows in order to handle C++ projects that target these frameworks.
