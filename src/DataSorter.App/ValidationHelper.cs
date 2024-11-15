using System.CommandLine.Parsing;

namespace DataSorter.App;

public static class ValidationHelper
{
    public static DirectoryInfo ValidateDirectoryInfo(ArgumentResult result)
    {
        var path = result.Tokens.First().Value;
            
        if (string.IsNullOrWhiteSpace(path))
        {
            result.ErrorMessage = "Path was not set!";
            return null!;
        }
        
        var directoryInfo = new DirectoryInfo(path);

        if (!directoryInfo.Exists)
            result.ErrorMessage = $"Given directory was not found! ({path})";

        return directoryInfo;
    }
}