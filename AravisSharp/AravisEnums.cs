namespace AravisSharp;

/// <summary>
/// Acquisition mode enumeration
/// </summary>
public enum ArvAcquisitionMode
{
    /// <summary>
    /// Continuous acquisition
    /// </summary>
    Continuous = 0,

    /// <summary>
    /// Single frame acquisition
    /// </summary>
    SingleFrame = 1,

    /// <summary>
    /// Multi-frame acquisition (requires frame count to be set)
    /// </summary>
    MultiFrame = 2
}

/// <summary>
/// Auto mode enumeration for exposure, gain, etc.
/// </summary>
public enum ArvAuto
{
    /// <summary>Manual mode (auto disabled)</summary>
    Off = 0,
    /// <summary>Single-shot auto (one adjustment then returns to manual)</summary>
    Once = 1,
    /// <summary>Continuous auto mode</summary>
    Continuous = 2
}

/// <summary>
/// Exposure mode — controls how exposure duration is determined
/// </summary>
public enum ArvExposureMode
{
    /// <summary>Exposure disabled; shutter stays open</summary>
    Off = 0,
    /// <summary>Timed exposure set via ExposureTime / ExposureAuto</summary>
    Timed = 1,
    /// <summary>Exposure duration equals the width of the trigger pulse</summary>
    TriggerWidth = 2,
    /// <summary>Separate trigger signals control start and end of exposure</summary>
    TriggerControlled = 3
}

/// <summary>
/// Buffer payload type — describes the kind of data in the buffer
/// </summary>
public enum ArvBufferPayloadType
{
    Unknown = -1,
    NoData = 0x0000,
    Image = 0x0001,
    RawData = 0x0002,
    File = 0x0003,
    ChunkData = 0x0004,
    ExtendedChunkData = 0x0005,
    Jpeg = 0x0006,
    Jpeg2000 = 0x0007,
    H264 = 0x000D,
    MultizoneImage = 0x0012,
    Multipart = 0x002A
}
