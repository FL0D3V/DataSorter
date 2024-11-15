namespace DataSorter.App;

public readonly record struct FileWithDateInfo(
    FileInfo File,
    DateTime LastWriteTime
);