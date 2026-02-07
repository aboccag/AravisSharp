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
    /// <summary>
    /// Manual mode (auto disabled)
    /// </summary>
    Off = 0,

    /// <summary>
    /// Single-shot auto (performs one automatic adjustment then returns to manual)
    /// </summary>
    Once = 1,

    /// <summary>
    /// Continuous auto mode
    /// </summary>
    Continuous = 2
}
