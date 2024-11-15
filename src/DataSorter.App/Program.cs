using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using DataSorter.App;

var inputPathSelectionCommand = new Option<DirectoryInfo>(
    aliases: Commands.INPUT_PATH,
    parseArgument: ValidationHelper.ValidateDirectoryInfo,
    description: "Used for selecting the input path.")
{
    IsRequired = true
};


var destinationPathSelectionCommand = new Option<DirectoryInfo?>(
    aliases: Commands.DESTINATION_PATH,
    parseArgument: ValidationHelper.ValidateDirectoryInfo,
    description: "Used for selecting the destination path.")
{
    IsRequired = false
};


var fileTypeSelectionCommand = new Option<FileType>(
    aliases: Commands.FILE_TYPE,
    description: "Used for selecting the file-type.")
{
    IsRequired = true
};


var verboseExecutionCommand = new Option<bool>(
    aliases: Commands.VERBOSE,
    description: "Verbose execution.")
{
    IsRequired = false
};


var shouldCopyCommand = new Option<bool>(
    aliases: Commands.COPY_FILES,
    description: "Used when the files shouldn't be moved but copied.")
{
    IsRequired = false
};


var skipExistingFilesCommand = new Option<bool>(
    aliases: Commands.SKIP_EXISTING,
    description: "Should already existing files be skipped else an error will be thrown when this happens.")
{
    IsRequired = false
};


RootCommand rootCommand = new("Organize all files with a given filetype in a specific folder with year and month subfolders. Can be used when all files are stored in one folder and if these should get organized by date in subfolders. This tool will MOVE all files from the input path to the destination if the files should get copied the copy command should get used.")
{
    inputPathSelectionCommand,
    destinationPathSelectionCommand,
    fileTypeSelectionCommand,
    verboseExecutionCommand,
    shouldCopyCommand,
    skipExistingFilesCommand
};

rootCommand.SetHandler(
    MainOperations.HandleFiles,
        inputPathSelectionCommand,
        destinationPathSelectionCommand,
        fileTypeSelectionCommand,
        verboseExecutionCommand,
        shouldCopyCommand,
        skipExistingFilesCommand);


Parser parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseExceptionHandler(MainOperations.ExceptionHandler)
    .Build();

parser.Invoke(args);