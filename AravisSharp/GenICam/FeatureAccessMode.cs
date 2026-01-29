namespace AravisSharp.GenICam;

/// <summary>
/// Represents the access mode of a GenICam feature
/// </summary>
public enum FeatureAccessMode
{
    /// <summary>Feature not implemented</summary>
    NotImplemented = 0,
    
    /// <summary>Feature not available (depends on other features)</summary>
    NotAvailable = 1,
    
    /// <summary>Feature write-only</summary>
    WriteOnly = 2,
    
    /// <summary>Feature read-only</summary>
    ReadOnly = 3,
    
    /// <summary>Feature read and write</summary>
    ReadWrite = 4,
    
    /// <summary>Unknown or undefined</summary>
    Undefined = 5
}

/// <summary>
/// Represents the type of a GenICam feature node
/// </summary>
public enum FeatureType
{
    Unknown,
    Integer,
    Float,
    String,
    Boolean,
    Command,
    Enumeration,
    Category,
    Register
}

/// <summary>
/// Represents the visibility level of a GenICam feature
/// </summary>
public enum FeatureVisibility
{
    Beginner = 0,
    Expert = 1,
    Guru = 2,
    Invisible = 3,
    Undefined = 4
}
