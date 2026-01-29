using System.Runtime.InteropServices;

namespace AravisSharp.Native;

/// <summary>
/// GError structure from GLib
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GError
{
    public uint Domain;
    public int Code;
    public IntPtr Message;
}
