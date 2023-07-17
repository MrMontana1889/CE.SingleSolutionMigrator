// FileAttributeTool.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using ShellProgressBar;

namespace CE.SingleSolutionMigrator.Tools
{
    public class FileAttributeTool
    {
        #region Constructor
        public FileAttributeTool()
        {

        }
        #endregion

        #region Public Methods
        public void Execute(string rootPath, bool isReadOnly)
        {
            var files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
            using (var pbar = new ProgressBar(files.Length, "Applying file attribute...",
                new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Red,
                    BackgroundColor = ConsoleColor.White,
                    ForegroundColorDone = ConsoleColor.Blue,
                    DisplayTimeInRealTime = true,
                }))
            {
                foreach (var file in files)
                {
                    pbar.Tick(file);

                    if (isReadOnly)
                        File.SetAttributes(file, File.GetAttributes(file) & FileAttributes.ReadOnly);
                    else
                        File.SetAttributes(file, File.GetAttributes(file) ^ FileAttributes.ReadOnly);
                }

                pbar.Message = "Process completed.";
            }
        }
        #endregion
    }
}
