using System.Runtime.InteropServices;

namespace EnvisionSuite.Core.Interop;

/// <summary>
///   P/Invoke bindings for essential libc functions.
///   Uses LibraryImport for AOT compatibility and better performance.
/// </summary>
/// <remarks>
///   These are the core POSIX system calls needed for:
///   - Opening and closing file descriptors (open, close)
///   - Reading and writing data (read, write)
///   - Device control (ioctl)
///   - I/O multiplexing (poll)
///   - Error handling (strerror)
/// </remarks>
public static partial class Libc
{
  /// <summary>
  ///   Opens a file or device.
  /// </summary>
  /// <param name="path">Path to the file or device.</param>
  /// <param name="flags">Open flags (O_RDONLY, O_WRONLY, O_RDWR, O_NONBLOCK, etc.).</param>
  /// <returns>File descriptor on success, -1 on error (check GetLastError).</returns>
  [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
  public static partial Int32 Open(String path, Int32 flags);

  /// <summary>
  ///   Closes a file descriptor.
  /// </summary>s
  /// <param name="fileDescriptor">File descriptor to close.</param>
  /// <returns>0 on success, -1 on error.</returns>
  [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
  public static partial Int32 Close(Int32 fileDescriptor);

  /// <summary>
  ///   Reads data from a file descriptor.
  /// </summary>
  /// <param name="fileDescriptor">File descriptor to read from.</param>
  /// <param name="buf">Buffer to receive the data.</param>
  /// <param name="count">Maximum number of bytes to read.</param>
  /// <returns>Number of bytes read, 0 at EOF, -1 on error.</returns>
  [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
  public static unsafe partial IntPtr Read(Int32 fileDescriptor, void* buf, UIntPtr count);

  /// <summary>
  ///   Writes data to a file descriptor.
  /// </summary>
  /// <param name="fileDescriptor">File descriptor to write to.</param>
  /// <param name="buf">Buffer containing the data to write.</param>
  /// <param name="count">Number of bytes to write.</param>
  /// <returns>Number of bytes written, -1 on error.</returns>
  [LibraryImport("libc", EntryPoint = "write", SetLastError = true)]
  public static unsafe partial IntPtr Write(Int32 fileDescriptor, void* buf, UIntPtr count);

  /// <summary>
  ///   Performs a device-specific control operation (with integer argument).
  /// </summary>
  /// <param name="fileDescriptor">File descriptor of the device.</param>
  /// <param name="request">Device-specific request code.</param>
  /// <param name="value">Integer argument for the request.</param>
  /// <returns>0 on success (usually), -1 on error.</returns>
  [LibraryImport("libc", EntryPoint = "ioctl", SetLastError = true)]
  public static unsafe partial Int32 Ioctl(Int32 fileDescriptor, UIntPtr request, Int32 value);

  /// <summary>
  ///   Performs a device-specific control operation (with pointer argument).
  /// </summary>
  /// <param name="fileDescriptor">File descriptor of the device.</param>
  /// <param name="request">Device-specific request code.</param>
  /// <param name="arg">Pointer to argument structure.</param>
  /// <returns>0 on success (usually), -1 on error.</returns>
  [LibraryImport("libc", EntryPoint = "ioctl", SetLastError = true)]
  public static unsafe partial Int32 Ioctl(Int32 fileDescriptor, UIntPtr request, void* arg);

  /// <summary>
  ///   Waits for events on multiple file descriptors.
  ///   This is the core I/O multiplexing function used to efficiently wait
  ///   for input from multiple devices without busy-waiting.
  /// </summary>
  /// <param name="fileDescriptors">Array of PollfileDescriptor structures describing the file descriptors to monitor.</param>
  /// <param name="nfileDescriptors">Number of file descriptors in the array.</param>
  /// <param name="timeout">Timeout in milliseconds (-1 for infinite, 0 for non-blocking).</param>
  /// <returns>Number of fileDescriptors with events, 0 on timeout, -1 on error.</returns>
  [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
  public static unsafe partial Int32 Poll(PollfileDescriptor* fileDescriptors, UIntPtr nfileDescriptors, Int32 timeout);

  [LibraryImport("libc", EntryPoint = "strerror", StringMarshalling = StringMarshalling.Utf8)]
  private static partial IntPtr StrErrorpublic(Int32 errnum);

  /// <summary>
  ///   Gets a human-readable error message for an error number.
  /// </summary>
  /// <param name="errnum">Error number (from GetLastError).</param>
  /// <returns>Error message string.</returns>
  public static String StrError(Int32 errnum)
  {
    IntPtr ptr = StrErrorpublic(errnum);
    return Marshal.PtrToStringUTF8(ptr) ?? "Unknown error";
  }

  public static Int32 GetLastError() =>
    Marshal.GetLastPInvokeError();

  /// <summary>Open for reading only.</summary>
  public const Int32 O_RDONLY = 0x0000;

  /// <summary>Open for writing only.</summary>
  public const Int32 O_WRONLY = 0x0001;

  /// <summary>Open for reading and writing.</summary>
  public const Int32 O_RDWR = 0x0002;

  /// <summary>Non-blocking I/O - read/write return immediately if no data available.</summary>
  public const Int32 O_NONBLOCK = 0x0800;

  /// <summary>Data available to read.</summary>
  public const Int32 POLLIN = 0x0001;

  /// <summary>Writing now will not block.</summary>
  public const Int32 POLLOUT = 0x0004;

  /// <summary>Error condition on device.</summary>
  public const Int32 POLLERR = 0x0008;

  /// <summary>Hang up (device disconnected).</summary>
  public const Int32 POLLHUP = 0x0010;
  public const Int16 POLLNVAL = 0x0020;

  /// <summary>Interrupted system call - should retry.</summary>
  public const Int32 EINTR = 4;

  /// <summary>Resource temporarily unavailable (would block).</summary>
  public const Int32 EAGAIN = 11;
}