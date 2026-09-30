namespace AravisSharp.Examples;

/// <summary>
/// Chooses the camera an example opens.
/// </summary>
/// <remarks>
/// The examples reconfigure and stream from the camera they open. <c>new Camera()</c> (or
/// <c>new Camera(null)</c>) opens whichever device Aravis enumerates first, and on a shared
/// network that can be a GigE camera belonging to another application. So the examples
/// never open a camera implicitly: the device ID comes from the <c>--device</c> command-line
/// argument or the <see cref="DeviceIdEnvironmentVariable"/> environment variable, is picked
/// automatically only when the Aravis fake camera is the single device visible, and is
/// otherwise asked for by index. Aravis leaves its "Fake" interface disabled unless
/// <c>arv_enable_interface("Fake")</c> is called, which the examples do not do, so in
/// practice they always ask unless a device ID is given.
/// </remarks>
public static class CameraPicker
{
    /// <summary>Environment variable naming the device ID the examples open.</summary>
    public const string DeviceIdEnvironmentVariable = "ARAVIS_EXAMPLE_DEVICE_ID";

    /// <summary>
    /// Device ID given on the command line (<c>--device &lt;id&gt;</c>); takes precedence over
    /// <see cref="DeviceIdEnvironmentVariable"/>.
    /// </summary>
    public static string? CommandLineDeviceId { get; set; }

    /// <summary>
    /// Discovers the cameras and returns the device ID of the one to open, or <c>null</c>
    /// when none was found or the user cancelled.
    /// </summary>
    public static string? Choose()
    {
        return ExplicitDeviceId() ?? ChooseFrom(Discover());
    }

    /// <summary>
    /// Same as <see cref="Choose()"/>, for an example that has already discovered the cameras.
    /// </summary>
    public static string? Choose(IReadOnlyList<CameraInfo> cameras)
    {
        return ExplicitDeviceId() ?? ChooseFrom(cameras);
    }

    /// <summary>
    /// True when <c>--device</c> or <see cref="DeviceIdEnvironmentVariable"/> names the camera,
    /// which may then be opened even if discovery did not list it (e.g. a GigE camera on
    /// another subnet, addressed by IP).
    /// </summary>
    public static bool HasExplicitDeviceId => GetExplicitDeviceId(out _) != null;

    private static string? GetExplicitDeviceId(out string source)
    {
        var fromCommandLine = CommandLineDeviceId?.Trim();
        if (!string.IsNullOrEmpty(fromCommandLine))
        {
            source = "--device";
            return fromCommandLine;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(DeviceIdEnvironmentVariable)?.Trim();
        if (!string.IsNullOrEmpty(fromEnvironment))
        {
            source = DeviceIdEnvironmentVariable;
            return fromEnvironment;
        }

        source = string.Empty;
        return null;
    }

    private static string? ExplicitDeviceId()
    {
        var deviceId = GetExplicitDeviceId(out var source);
        if (deviceId != null)
            Console.WriteLine($"Using camera '{deviceId}' ({source}).");
        return deviceId;
    }

    private static List<CameraInfo> Discover()
    {
        Console.WriteLine("Discovering cameras...");
        return CameraDiscovery.DiscoverCameras();
    }

    private static string? ChooseFrom(IReadOnlyList<CameraInfo> cameras)
    {
        if (cameras.Count == 0)
        {
            Console.WriteLine("No cameras found!");
            return null;
        }

        if (cameras.Count == 1 && IsFakeCamera(cameras[0]))
        {
            Console.WriteLine($"Using the Aravis fake camera: {cameras[0]}\n");
            return cameras[0].DeviceId;
        }

        Console.WriteLine($"Found {cameras.Count} camera(s):");
        for (int i = 0; i < cameras.Count; i++)
        {
            Console.WriteLine($"  [{i}] {cameras[i]}");
            Console.WriteLine($"      Device ID: {cameras[i].DeviceId}");
        }
        Console.WriteLine($"(Pass --device <id> or set {DeviceIdEnvironmentVariable} to skip this prompt.)");

        Console.Write("\nIndex of the camera to open (empty to cancel): ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("No camera selected.");
            return null;
        }
        if (!int.TryParse(input, out int index) || index < 0 || index >= cameras.Count)
        {
            Console.WriteLine($"Invalid index '{input}'. No camera selected.");
            return null;
        }

        Console.WriteLine();
        return cameras[index].DeviceId;
    }

    private static bool IsFakeCamera(CameraInfo camera) =>
        string.Equals(camera.Protocol, "Fake", StringComparison.OrdinalIgnoreCase) ||
        camera.DeviceId.StartsWith("Fake_", StringComparison.OrdinalIgnoreCase);
}
