namespace DataSorter.App;

public sealed class ExecutionFailedException : Exception
{
    public ExecutionFailedException(Exception baseException, FileWithDateInfo fileInfo, string absoluteDestFolder)
        : base(baseException.Message, baseException)
    {
        FileInfo = fileInfo;
        AbsoluteDestFolder = absoluteDestFolder;
    }

    
    public FileWithDateInfo FileInfo { get; private init; }
    public string AbsoluteDestFolder { get; private init; }
}