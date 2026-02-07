using System.Reflection;
using System.Runtime.InteropServices;

namespace AravisSharp.Native;

/// <summary>
/// Cross-platform library name resolution for Aravis.
/// Call <see cref="RegisterResolver"/> once at application startup (before any P/Invoke call)
/// to enable automatic native-library resolution from the NuGet runtimes/ folder.
/// </summary>
public static class AravisLibrary
{
    private static bool _resolverRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Registers a <see cref="NativeLibrary.SetDllImportResolver"/> for both
    /// <see cref="AravisNative"/> and <see cref="AravisSharp.Generated.AravisGenerated"/>
    /// so that the logical name "aravis-0.8" is mapped to the correct platform-specific
    /// file at runtime.
    /// </summary>
    public static void RegisterResolver()
    {
        lock (_lock)
        {
            if (_resolverRegistered) return;

            NativeLibrary.SetDllImportResolver(
                typeof(AravisNative).Assembly,
                ResolveDllImport);

            _resolverRegistered = true;
        }
    }

    /// <summary>
    /// The DllImport resolver callback.
    /// </summary>
    private static IntPtr ResolveDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Determine which set of candidate names to use
        string[]? candidates = null;

        if (libraryName == AravisNative.LibraryName)
            candidates = GetPlatformLibraryNames();
        else if (libraryName == GLibNative.GObjectLibraryName)
            candidates = GetPlatformGObjectNames();
        else if (libraryName == GLibNative.GLibLibraryName)
            candidates = GetPlatformGLibNames();

        if (candidates is null)
            return IntPtr.Zero;

        // 1. Try bare names (OS searches PATH / LD_LIBRARY_PATH / rpath)
        foreach (var name in candidates)
        {
            if (NativeLibrary.TryLoad(name, out var handle))
                return handle;
        }

        // 2. Probe runtimes/{rid}/native/ relative to the assembly location
        var assemblyDir = Path.GetDirectoryName(typeof(AravisLibrary).Assembly.Location);
        if (assemblyDir is not null)
        {
            var rid = GetRuntimeIdentifier();
            var runtimeNativeDir = Path.Combine(assemblyDir, "runtimes", rid, "native");

            foreach (var name in candidates)
            {
                var full = Path.Combine(runtimeNativeDir, name);
                if (NativeLibrary.TryLoad(full, out var handle))
                    return handle;
            }
        }

        // Fallback: let the default resolver try
        return IntPtr.Zero;
    }

    /// <summary>
    /// Returns the set of file names to try on the current platform.
    /// </summary>
    private static string[] GetPlatformLibraryNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new[]
            {
                "libaravis-0.8-0.dll",   // MinGW / MSYS2 build (lib prefix)
                "aravis-0.8-0.dll",      // MSVC build (no lib prefix)
                "aravis-0.8.dll",
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new[]
            {
                "libaravis-0.8.0.dylib",
                "libaravis-0.8.dylib",
            };
        }

        // Linux / other
        return new[]
        {
            "libaravis-0.8.so.0",
            "libaravis-0.8.so",
        };
    }

    /// <summary>
    /// Returns the set of GObject library file names to try on the current platform.
    /// </summary>
    private static string[] GetPlatformGObjectNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new[] { "libgobject-2.0-0.dll", "gobject-2.0-0.dll" };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new[] { "libgobject-2.0.0.dylib", "libgobject-2.0.dylib" };
        return new[] { "libgobject-2.0.so.0", "libgobject-2.0.so" };
    }

    /// <summary>
    /// Returns the set of GLib library file names to try on the current platform.
    /// </summary>
    private static string[] GetPlatformGLibNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new[] { "libglib-2.0-0.dll", "glib-2.0-0.dll" };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new[] { "libglib-2.0.0.dylib", "libglib-2.0.dylib" };
        return new[] { "libglib-2.0.so.0", "libglib-2.0.so" };
    }

    /// <summary>
    /// Returns the .NET runtime identifier for the current platform.
    /// </summary>
    private static string GetRuntimeIdentifier()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64   => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86   => "x86",
            Architecture.Arm   => "arm",
            _ => "x64"
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"win-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))     return $"osx-{arch}";
        return $"linux-{arch}";
    }

    /// <summary>
    /// Gets the platform-specific Aravis library name (for display purposes).
    /// </summary>
    public static string GetLibraryName()
    {
        return GetPlatformLibraryNames()[0];
    }

    /// <summary>
    /// Gets detailed platform information
    /// </summary>
    public static string GetPlatformInfo()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        var os = RuntimeInformation.OSDescription;
        var framework = RuntimeInformation.FrameworkDescription;
        
        return $"OS: {os}\nArchitecture: {arch}\nFramework: {framework}";
    }

    /// <summary>
    /// Checks if Aravis is likely installed on the system.
    /// Automatically registers the resolver if not already done.
    /// </summary>
    public static bool IsAravisAvailable()
    {
        RegisterResolver();

        try
        {
            // Try to load the library by calling a simple function
            AravisNative.arv_update_device_list();
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch
        {
            // Other errors mean library was found but call failed
            return true;
        }
    }

    /// <summary>
    /// Gets installation instructions for the current platform
    /// </summary>
    public static string GetInstallationInstructions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return @"
Windows Installation:
1. Download Aravis Windows build from: https://github.com/AravisProject/aravis/releases
2. Install the MSI package or extract ZIP to C:\Program Files\Aravis
3. Add Aravis\bin to your PATH environment variable
4. Restart your application

Alternative: Use vcpkg
  vcpkg install aravis

Or: Download pre-built DLL from AravisSharp NuGet package (when available)
";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            
            if (arch == Architecture.X64)
            {
                return @"
Linux x64 Installation:

Ubuntu/Debian:
  sudo apt-get update
  sudo apt-get install libaravis-0.8-0 libaravis-dev

Fedora/RHEL:
  sudo dnf install aravis aravis-devel

Arch Linux:
  sudo pacman -S aravis

From source:
  git clone https://github.com/AravisProject/aravis.git
  cd aravis
  meson build
  cd build
  ninja
  sudo ninja install
";
            }
            else if (arch == Architecture.Arm64 || arch == Architecture.Arm)
            {
                return @"
Linux ARM/ARM64 Installation:

Raspberry Pi OS / Debian ARM:
  sudo apt-get update
  sudo apt-get install libaravis-0.8-0 libaravis-dev

Ubuntu ARM:
  sudo apt-get install libaravis-0.8-0

Build from source (recommended for ARM):
  sudo apt-get install libxml2-dev libglib2.0-dev libusb-1.0-0-dev
  git clone https://github.com/AravisProject/aravis.git
  cd aravis
  meson build -Dintrospection=disabled -Dviewer=disabled
  cd build
  ninja
  sudo ninja install
  sudo ldconfig
";
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return @"
macOS Installation:

Using Homebrew:
  brew install aravis

From source:
  git clone https://github.com/AravisProject/aravis.git
  cd aravis
  meson build
  cd build
  ninja
  sudo ninja install
";
        }

        return "Platform not recognized. Please install Aravis from source: https://github.com/AravisProject/aravis";
    }
}
