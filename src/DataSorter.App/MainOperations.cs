using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Globalization;
using Konsole;

namespace DataSorter.App;

public static class MainOperations
{
    private static readonly string[] SupportedImageFileExtensions = new[]
    {
        "jpg", "jpeg"
    };

    private static readonly string[] SupportedDocumentFileExtensions = new[]
    {
        "txt", "json",
        "pdf",
        "docx", "docm", "doc", "rtf",
        "xlsx", "xls", "xlsm",
        "pptx", "ppt", "pptm"
    };

    private const ConsoleColor InfoColor = ConsoleColor.Green;
    private const ConsoleColor WarningColor = ConsoleColor.Yellow;
    private const ConsoleColor ErrorColor = ConsoleColor.Red;

    private const ConsoleColor WarningBackgroundColor = ConsoleColor.DarkGray;
    private const ConsoleColor ErrorBackgroundColor = ConsoleColor.DarkRed;
    
    
    /// <summary>
    ///     This command organizes all files with a specific type inside the given folder in subfolders with the creation year and then the creation month
    ///     using the creation date from each file inside the folder.
    /// </summary>
    public static void HandleFiles(
        DirectoryInfo inputPath,
        DirectoryInfo? destinationPath,
        FileType fileType,
        bool verbose = false,
        bool shouldCopy = false,
        bool skipExistingFiles = false)
    {
        var supportedFileExtensions = fileType.GetSupportedFileExtensionsFromFileType();

        // Filtering by selected file type and ordering by the last write time.
        List<FileWithDateInfo> filteredFiles = GetAllFilteredAndOrderedFilesFromDirectory(inputPath, fileType, supportedFileExtensions);
        
        int countOfFilteredFiles = filteredFiles.Count;

        // Displays all execution informations.
        DisplayExecutionHeader(inputPath, destinationPath, fileType, supportedFileExtensions, countOfFilteredFiles, verbose, shouldCopy, skipExistingFiles);

        // Check if files where found else stop execution.
        if (countOfFilteredFiles <= 0)
        {
            DisplayStoppingExecutionMessage();
            return;
        }
        
        // Check if user is sure to execute.
        if (!CheckForUserConfirmation())
        {
            DisplayStoppingExecutionMessage();
            return;
        }

        // Display a new line for the rest of the informations.
        // Console.WriteLine();

        // Starting execution.
        
        ProgressBar? progressBar = null;

        if (!verbose)
            progressBar = new ProgressBar(PbStyle.DoubleLine, countOfFilteredFiles);

        int totalFilesHandled = 0;

        Stopwatch stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < countOfFilteredFiles; i++)
        {
            FileWithDateInfo fileInfo = filteredFiles[i];
            
            if (!verbose)
                progressBar?.Refresh(i + 1, "Handling: {0}\t[{1}]", fileInfo.File.Name, fileInfo.LastWriteTime);

            // Set all paths.
            string relativeDestFolder = Path.Combine(
                fileInfo.LastWriteTime.Year.ToString(),
                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(fileInfo.LastWriteTime.Month));
            
            // TODO: Test! -> Take the destination-path if set else use the input-path.
            string absoluteDestFolder = Path.Combine(
                destinationPath?.FullName ?? inputPath.FullName,
                relativeDestFolder);

            string absoluteDestFolderWithFileName = Path.Combine(
                absoluteDestFolder,
                fileInfo.File.Name);
            
            try
            {
                // Create all directories if they don't exist already.
                if (!Directory.Exists(absoluteDestFolder))
                    Directory.CreateDirectory(absoluteDestFolder);
                
                bool skipFile = File.Exists(absoluteDestFolderWithFileName) && skipExistingFiles;
                
                // Displays the verbose file-info. Should maybe not be used when handling a large amount of files because every file-info will be printed.
                DisplayVerboseFileInfo(verbose, i + 1, countOfFilteredFiles, fileInfo, skipFile);
                
                if (skipFile)
                    continue;
                
                // Move or copy the file to the destination folder.
                if (!shouldCopy)
                    fileInfo.File.MoveTo(absoluteDestFolderWithFileName);
                else
                    fileInfo.File.CopyTo(absoluteDestFolderWithFileName);
            }
            catch (Exception ex)
            {
                // When an error happend throw custom exception.
                throw new ExecutionFailedException(ex, fileInfo, absoluteDestFolder);
            }

            totalFilesHandled++;
        }

        stopwatch.Stop();

        // When everything is finished display the success message.
        DisplaySuccessMessage(totalFilesHandled, countOfFilteredFiles, stopwatch.Elapsed, shouldCopy);
    }


    private static void DisplayStoppingExecutionMessage()
    {
        Console.WriteLine("Stopping execution.");
    }


    public static void ExceptionHandler(Exception exception, InvocationContext context)
    {
        Console.WriteLine();
        
        switch (exception)
        {
            case ExecutionFailedException efe:
                efe.HandleExecutionFailedException();
                break;
            
            default:
                exception.HandleDefaultException();
                break;
        }
        
        Console.WriteLine();

        DisplayStoppingExecutionMessage();
    }


    private static void DisplayErrorMessage(Type? exceptionType, string? exceptionMessage)
    {
        Console.BackgroundColor = ErrorBackgroundColor;
        Console.Write($"[{exceptionType?.Name ?? "ERROR"}]");
        Console.ResetColor();
        
        Console.ForegroundColor = ErrorColor;
        Console.Error.Write($": {exceptionMessage ?? "An error has occurred."}");
        Console.ResetColor();
        Console.WriteLine();
    }

    
    private static void HandleExecutionFailedException(this ExecutionFailedException efe)
    {
        DisplayErrorMessage(efe.InnerException?.GetType(), efe?.Message);
        
        if (efe is null)
            return;
        
        Console.Write("File-Name: ");
        Console.ForegroundColor = WarningColor;
        Console.Error.Write($"{efe.FileInfo.File.Name}");
        Console.ResetColor();
        Console.Write($", Destination-Path: ");
        Console.ForegroundColor = WarningColor;
        Console.Error.Write($"{efe.AbsoluteDestFolder}");
        Console.ResetColor();
        Console.WriteLine();
    }
    

    private static void HandleDefaultException(this Exception e)
    {
        DisplayErrorMessage(e?.GetType(), e?.Message);
    }


    private static void DisplayExecutionHeader(
        DirectoryInfo inputPath,
        DirectoryInfo? destinationPath,
        FileType type,
        string[]? supportedFileExtensions,
        int countOfFiles,
        bool verbose,
        bool shouldCopy,
        bool skipExistingFiles)
    {
        // TODO: Maybe implement this way because its prettier.
        // var box = Window.OpenBox("Execution Settings", 100, 10);
        // box.WriteLine(InfoColor, "Selected Directory: {0}", inputPath);
        
        // Displays all execution informations.
        Console.Write("Selected Directory:\t\t");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(inputPath);
        Console.ResetColor();

        if (destinationPath != null)
        {
            Console.Write("Destination Directory:\t\t");
            Console.ForegroundColor = InfoColor;
            Console.WriteLine(destinationPath);
            Console.ResetColor();
        }
        
        Console.Write("Selected File-Type:\t\t");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(type.ConvertFileTypeToString());
        Console.ResetColor();
        
        if (supportedFileExtensions != null)
        {
            var fileExtns = supportedFileExtensions
                .Aggregate("", (last, current) => !string.IsNullOrWhiteSpace(last) ? last + ", " + current : current);
            
            Console.Write("Supported File-Extensions:\t");
            Console.ForegroundColor = InfoColor;
            Console.WriteLine(fileExtns);
            Console.ResetColor();
        }

        Console.Write("Skip existing files:\t\t");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(skipExistingFiles.BoolToString());
        Console.ResetColor();

        Console.Write("Files will get:\t\t\t");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(shouldCopy.ExecutionTypeToString());
        Console.ResetColor();

        Console.Write("Verbose:\t\t\t");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(verbose.BoolToString());
        Console.ResetColor();
        
        Console.Write("Files:\t\t\t\t");
        // if (countOfFiles <= 0)
            Console.BackgroundColor = WarningBackgroundColor;
        Console.ForegroundColor = InfoColor;
        Console.Write(countOfFiles);
        Console.ResetColor();
        // if (countOfFiles <= 0)
            Console.BackgroundColor = WarningBackgroundColor;
        Console.Write(" file{0} found", countOfFiles != 1 ? "s" : String.Empty);
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine();
    }


    private static string ExecutionTypeToString(this bool shouldCopy) => shouldCopy ? "copied" : "moved";
    
    private static string BoolToString(this bool value) => value ? "yes" : "no";


    private static bool CheckForUserConfirmation()
    {
        Console.WriteLine("Do you want to execute this operation?");

        ConsoleKeyInfo inputKey;
        while (true)
        {
            Console.Write("Yes[");
            Console.ForegroundColor = InfoColor;
            Console.Write("Y");
            Console.ResetColor();
            Console.Write("] / No[");
            Console.ForegroundColor = InfoColor;
            Console.Write("N");
            Console.ResetColor();
            Console.Write("]: ");
            
            inputKey = Console.ReadKey();

            if (inputKey.Key == ConsoleKey.Y || inputKey.Key == ConsoleKey.N)
                break;
            
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine();
        
        if (inputKey.Key == ConsoleKey.N)
            return false;
        
        return true;
    }


    private static void DisplayVerboseFileInfo(bool verbose, int currentIndex, int totalFileCount, FileWithDateInfo fileInfo, bool willBeSkipped)
    {
        if (!verbose)
            return;

        Console.ForegroundColor = InfoColor;
        Console.Write($"{currentIndex}/{totalFileCount}.\t");
        Console.ResetColor();

        Console.Write(" File-Name: ");
        
        Console.ForegroundColor = InfoColor;
        Console.Write($"{fileInfo.File.Name}");
        Console.ResetColor();

        Console.Write(",\tType: ");
        
        Console.ForegroundColor = InfoColor;
        Console.Write($"{fileInfo.File.Extension[1..]}");
        Console.ResetColor();

        Console.Write(",\tLast-Write-Time: ");
        
        Console.ForegroundColor = InfoColor;
        Console.Write($"{fileInfo.LastWriteTime}");
        Console.ResetColor();

        if (willBeSkipped)
        {
            Console.Write("\t(");
        
            Console.ForegroundColor = WarningColor;
            Console.Write($"SKIPPED");
            Console.ResetColor();

            Console.Write(")");
        }

        Console.WriteLine();
    }


    private static void DisplaySuccessMessage(int totalHandledFiles, int totalFiles, TimeSpan timeTaken, bool shouldCopy = false)    {

        int skippedFiles = totalFiles - totalHandledFiles;

        Console.WriteLine();
        Console.ForegroundColor = InfoColor;
        Console.Write(totalHandledFiles);
        Console.ResetColor();

        Console.Write(" file{0} successfully {1} (skipped ", totalHandledFiles > 1 ? "s" : String.Empty, shouldCopy.ExecutionTypeToString());
        
        Console.ForegroundColor = WarningColor;
        Console.Write(skippedFiles);
        Console.ResetColor();
        Console.WriteLine(")");

        Console.Write("This took: ");
        Console.ForegroundColor = InfoColor;
        Console.Write(timeTaken.ToPrettyFormat());
        Console.ResetColor();
        Console.WriteLine();
    }


    private static List<FileWithDateInfo> GetAllFilteredAndOrderedFilesFromDirectory(DirectoryInfo directoryInfo, FileType fileType, string[]? supportedFileExtensions)
    {
        var filesEnumerable = directoryInfo.EnumerateFiles();

        // Filtering by selected file type and ordering by the last write time.
        var filteredFiles = (supportedFileExtensions != null
            ? filesEnumerable.Where(x => CheckIfExtensionIsInList(x.Extension, supportedFileExtensions))
            : filesEnumerable)
            .Select(x => new FileWithDateInfo(
                x,
                GetLastWriteTimeOfFile(x, fileType, supportedFileExtensions)))
            .OrderBy(x => x.LastWriteTime)
            .ToList();
        
        return filteredFiles;
    }


    /// <summary>
    ///     Get the supported file extensions based on the selected filetype.
    /// </summary>
    private static string[]? GetSupportedFileExtensionsFromFileType(this FileType fileType) => fileType switch
    {
        FileType.Images => SupportedImageFileExtensions,
        FileType.Documents => SupportedDocumentFileExtensions,
        FileType.AllFiles => null,
        _ => null
    };

    
    /// <summary>
    ///     Checks if the given file-extension is in the given list. This function will remove the leading dot of the extension.
    /// </summary>
    private static bool CheckIfExtensionIsInList(string fileExtension, string[] toCheckIn)
        => toCheckIn.Contains(fileExtension[1..]);

    
    /// <summary>
    ///     Converts the file-type enum to the printable string representation.
    /// </summary>
    private static string? ConvertFileTypeToString(this FileType fileType) => fileType switch
    {
        FileType.Images => "Images",
        FileType.Documents => "Documents",
        FileType.AllFiles => "All files",
        _ => null
    };


    /// <summary>
    ///     Loads the last write time of the given file.
    /// </summary>
    private static DateTime GetLastWriteTimeOfFile(FileInfo info, FileType type, string[]? fileExtensions) => type switch
    {
        FileType.Documents => info.LastAccessTime,
        // TODO: Maybe implement checks for different images (jpg, png, ...).
        FileType.Images => ImageHelper.GetDateTakenFromImage(info),
        // Check if image.
        FileType.AllFiles => CheckIfExtensionIsInList(info.Extension, SupportedImageFileExtensions)
            // TODO: Maybe implement checks for different images (jpg, png, ...).
            ? ImageHelper.GetDateTakenFromImage(info)
            // Else normal file.
            : info.LastWriteTime,
        _ => info.LastWriteTime
    };
}