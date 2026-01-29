using System.Runtime.InteropServices;

namespace AravisSharp.Native;

/// <summary>
/// Cross-platform library name resolution for Aravis
/// </summary>
public static class AravisLibrary
{
    /// <summary>
    /// Gets the platform-specific Aravis library name
    /// </summary>
    public static string GetLibraryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: aravis-0.8.dll or aravis-0.8-0.dll depending on build
            return "aravis-0.8-0.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux (x64 and ARM): libaravis-0.8.so.0
            return "libaravis-0.8.so.0";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: libaravis-0.8.dylib or libaravis-0.8.0.dylib
            return "libaravis-0.8.0.dylib";
        }
        else
        {
            // Fallback
            return "aravis-0.8";
        }
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
    /// Checks if Aravis is likely installed on the system
    /// </summary>
    public static bool IsAravisAvailable()
    {
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
