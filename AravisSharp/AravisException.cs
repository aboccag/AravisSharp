using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Exception thrown by Aravis operations. When it comes from a native <c>GError</c>,
/// <see cref="Domain"/> and <see cref="Code"/> identify the error.
/// </summary>
public class AravisException : Exception
{
    public AravisException(string message) : base(message)
    {
    }

    public AravisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private AravisException(string message, string? domain, int code) : base(message)
    {
        Domain = domain;
        Code = code;
    }

    /// <summary>
    /// The domain of the native <c>GError</c>, as the string its GQuark stands for
    /// (for example <c>arv-device-error-quark</c> or <c>arv-gc-error-quark</c>).
    /// <c>null</c> when the exception does not come from a <c>GError</c>, or in the unlikely
    /// case of a <c>GError</c> without a domain.
    /// </summary>
    public string? Domain { get; }

    /// <summary>
    /// The code of the native <c>GError</c> within <see cref="Domain"/>, for example a value of
    /// the C enum <c>ArvDeviceError</c> for <c>arv-device-error-quark</c>.
    /// <c>null</c> when the exception does not come from a <c>GError</c>: 0 is a valid code in
    /// most domains (<c>ARV_DEVICE_ERROR_WRONG_FEATURE</c>), so it cannot mean "no code".
    /// </summary>
    public int? Code { get; }

    /// <summary>
    /// The one place that turns a native <c>GError*</c> into an exception: copies its message,
    /// domain and code, frees the <c>GError</c> and sets <paramref name="error"/> to
    /// <see cref="IntPtr.Zero"/>, so the caller's own cleanup cannot free it a second time.
    /// The caller throws the result.
    /// </summary>
    internal static AravisException TakeGError(ref IntPtr error)
    {
        if (error == IntPtr.Zero)
            return new AravisException("Unknown error");

        try
        {
            var gerror = Marshal.PtrToStructure<GError>(error);
            var message = Marshal.PtrToStringUTF8(gerror.Message) ?? "Unknown error";
            string? domain = null;
            if (gerror.Domain != 0)
            {
                // Owned by GLib: never freed.
                var domainPtr = GLibNative.g_quark_to_string(gerror.Domain);
                if (domainPtr != IntPtr.Zero)
                    domain = Marshal.PtrToStringUTF8(domainPtr);
            }

            return new AravisException(message, domain, gerror.Code);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }
}
