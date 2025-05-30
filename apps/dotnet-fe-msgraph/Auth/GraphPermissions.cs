namespace DotnetFeMsGraph.Auth;

public static class GraphPermissions
{
    // User permissions
    public const string UserRead = "User.Read";
    public const string UserReadWrite = "User.ReadWrite";
    public const string UserReadAll = "User.Read.All";
    public const string UserReadWriteAll = "User.ReadWrite.All";
    
    // Mail permissions
    public const string MailRead = "Mail.Read";
    public const string MailSend = "Mail.Send";
    public const string MailReadWrite = "Mail.ReadWrite";
    
    // Calendar permissions
    public const string CalendarRead = "Calendars.Read";
    public const string CalendarReadWrite = "Calendars.ReadWrite";
    
    // Files permissions
    public const string FilesRead = "Files.Read";
    public const string FilesReadAll = "Files.Read.All";
    public const string FilesReadWrite = "Files.ReadWrite";
    public const string FilesReadWriteAll = "Files.ReadWrite.All";
    
    // Directory permissions
    public const string DirectoryReadAll = "Directory.Read.All";
    public const string DirectoryReadWriteAll = "Directory.ReadWrite.All";
    
    // List of all supported permissions
    public static readonly string[] AllPermissions = 
    {
        UserRead,
        UserReadWrite,
        UserReadAll,
        UserReadWriteAll,
        MailRead,
        MailSend,
        MailReadWrite,
        CalendarRead,
        CalendarReadWrite,
        FilesRead,
        FilesReadAll,
        FilesReadWrite,
        FilesReadWriteAll,
        DirectoryReadAll,
        DirectoryReadWriteAll
    };
}