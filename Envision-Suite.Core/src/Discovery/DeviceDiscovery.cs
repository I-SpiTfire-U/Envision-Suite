using System.Collections.Immutable;
using System.Globalization;

namespace EnvisionSuite.Core.Discovery;

/// <summary>
///   Discovers Scuf Envision Pro V2 controller devices by scanning /sys/class.
///   Finds both evdev (input events) and hidraw (raw HID reports) devices
///   by matching the controller's USB vendor/product IDs.
/// </summary>
public static class DeviceDiscovery
{
  private const UInt16 ScufUSBVendorId = 0x1b1c;
  private const UInt16 ScufUSBProductId = 0x3a08;
  private const Byte ScufInputInterfaceNumber = 0x03;

  /// <summary>
  ///   Searches for connected Scuf Envision Pro V2 controller devices.
  ///   Scans /sys/class/input for evdev devices and /sys/class/hidraw for hidraw devices
  ///   matching the Scuf vendor/product IDs.
  /// </summary>
  /// <returns>
  ///   A <see cref="DiscoveredDevices" /> object containing paths to all controller devices,
  ///   or null if the controller is not found.
  /// </returns>
  public static DiscoveredDevices? FindController()
  {
    (String? evdevPath, ImmutableArray<String> additionalEvdevPaths) = FindAllEvdevDevices();
    String? hidrawDevicePath = FindHidrawDevice();

    if (evdevPath is null)
    {
      Console.Error.WriteLine($"""
      Error: Could not find Scuf Envision Pro controller evdev device.
      Looking for VID={ScufUSBVendorId:x4} PID={ScufUSBProductId:x4}
      """);
      return null;
    }

    if (hidrawDevicePath is null)
    {
      Console.Error.WriteLine("[warning] Could not find hidraw device. R2 trigger may not work correctly.");
    }

    return new DiscoveredDevices
    {
      EvdevDevicePath = evdevPath,
      HidrawDevicePath = hidrawDevicePath,
      AdditionalEvdevPaths = additionalEvdevPaths
    };
  }

  /// <summary>
  ///   Finds all evdev devices matching the Scuf controller.
  ///   The controller exposes multiple event devices - we identify the primary joystick
  ///   device (which has a js* handler) and collect secondary devices for grabbing.
  /// </summary>
  /// <returns>
  ///   A tuple containing the primary evdev path, or null if none was found,
  ///   and an immutable array of additional evdev paths that should be grabbed
  ///   to prevent input leakage.
  /// </returns>
  private static (String? primaryJoystickDevice, ImmutableArray<String> additionalEvdevPaths) FindAllEvdevDevices()
  {
    const String inputClassPath = "/sys/class/input";
    if (!Directory.Exists(inputClassPath))
    {
      return (null, []);
    }

    List<(String devicePath, Boolean hasJoystickHandler)> allEvdevDevices = [];

    String[] eventDirectories;
    try
    {
      eventDirectories = Directory.GetDirectories(inputClassPath, "event*");
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return (null, []);
    }

    foreach (String eventDirectory in eventDirectories)
    {
      String deviceIdPath = Path.Combine(eventDirectory, "device", "id");
      if (!Directory.Exists(deviceIdPath))
      {
        continue;
      }

      String vendorPath = Path.Combine(deviceIdPath, "vendor");
      String productPath = Path.Combine(deviceIdPath, "product");
      if (!File.Exists(vendorPath) || !File.Exists(productPath))
      {
        continue;
      }

      String vendorString;
      String productString;
      try
      {
        vendorString = File.ReadAllText(vendorPath).Trim();
        productString = File.ReadAllText(productPath).Trim();
      }
      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
      {
        continue;
      }

      Boolean vendorParsedSuccessfully = UInt16.TryParse(vendorString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt16 vendorId);
      Boolean productParsedSuccessfully = UInt16.TryParse(productString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt16 productId);
      if (!vendorParsedSuccessfully || !productParsedSuccessfully || vendorId != ScufUSBVendorId || productId != ScufUSBProductId)
      {
        continue;
      }

      String eventName = Path.GetFileName(eventDirectory);
      String devicePath = $"/dev/input/{eventName}";
      Boolean hasJoystickHandler = HasJoystickHandler(eventDirectory);

      allEvdevDevices.Add((devicePath, hasJoystickHandler));
    }

    String? primaryJoystickDevice = null;
    List<String> additionalEvdevPaths = [];
    foreach ((String devicePath, Boolean hasJoystickHandler) in allEvdevDevices)
    {
      if (hasJoystickHandler && primaryJoystickDevice is null)
      {
        primaryJoystickDevice = devicePath;
        continue;
      }
      additionalEvdevPaths.Add(devicePath);
    }

    if (primaryJoystickDevice is null && allEvdevDevices.Count > 0)
    {
      primaryJoystickDevice = allEvdevDevices[0].devicePath;
      additionalEvdevPaths = [.. allEvdevDevices.Skip(1).Select(d => d.devicePath)];
    }

    return (primaryJoystickDevice, additionalEvdevPaths.ToImmutableArray());
  }

  /// <summary>
  ///   Checks if an event device has an associated js* (joystick) device.
  ///   The js* device indicates this is the main gamepad interface with button support,
  ///   as opposed to secondary interfaces like mouse or keyboard emulation.
  /// </summary>
  /// <param name="eventDirectory">Path to the event directory in /sys/class/input.</param>
  /// <returns>True if a js* sibling device exists, indicating this is the main joystick.</returns>
  private static Boolean HasJoystickHandler(String eventDirectory)
  {
    FileSystemInfo? linkTarget;
    try
    {
      linkTarget = Directory.ResolveLinkTarget(eventDirectory, true);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return false;
    }

    if (linkTarget is null)
    {
      return false;
    }

    String? parentDirectory = Path.GetDirectoryName(linkTarget.FullName);
    if (parentDirectory is null || !Directory.Exists(parentDirectory))
    {
      return false;
    }
    
    try
    {
      String[] jsEntries = Directory.GetDirectories(parentDirectory, "js*");
      return jsEntries.Length > 0;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return false;
    }
  }

  /// <summary>
  ///   Finds the hidraw device for the Scuf controller by scanning /sys/class/hidraw.
  ///   Parses the HID_ID field in uevent files to match the controller's vendor/product IDs.
  /// </summary>
  /// <returns>
  ///   Path to the hidraw device (e.g., "/dev/hidraw0"), or null if not found.
  /// </returns>
  private static String? FindHidrawDevice()
  {
    const String hidrawClassPath = "/sys/class/hidraw";
    if (!Directory.Exists(hidrawClassPath))
    {
      return null;
    }

    String[] hidrawDirectories;
    try
    {
      hidrawDirectories = Directory.GetDirectories(hidrawClassPath, "hidraw*");
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return null;
    }

    foreach (String hidrawDirectory in hidrawDirectories)
    {
      if (!IsUsbInterface(hidrawDirectory, ScufInputInterfaceNumber))
      {
        continue;
      }

      String ueventPath = Path.Combine(hidrawDirectory, "device", "uevent");
      if (!File.Exists(ueventPath))
      {
        continue;
      }

      String ueventContent;
      try
      {
        ueventContent = File.ReadAllText(ueventPath);
      }
      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
      {
        continue;
      }

      foreach (String line in ueventContent.Split('\n'))
      {
        if (!line.StartsWith("HID_ID=", StringComparison.Ordinal))
        {
          continue;
        }

        String[] parts = line[7..].Split(':');

        if (parts.Length < 3)
        {
          continue;
        }

        if (UInt32.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt32 vendor) &&
            UInt32.TryParse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt32 product) &&
            vendor == ScufUSBVendorId && product == ScufUSBProductId)
        {
          return $"/dev/{Path.GetFileName(hidrawDirectory)}";
        }
      }
    }

    return null;
  }

  private static Boolean IsUsbInterface(String hidrawDirectory, Byte expectedInterfaceNumber)
  {
    DirectoryInfo? deviceDirectory;

    try
    {
      deviceDirectory = Directory.ResolveLinkTarget(hidrawDirectory, returnFinalTarget: true) as DirectoryInfo;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return false;
    }

    while (deviceDirectory is not null)
    {
      String interfaceNumberPath = Path.Combine(deviceDirectory.FullName, "bInterfaceNumber");

      if (!File.Exists(interfaceNumberPath))
      {
        deviceDirectory = deviceDirectory.Parent;
        continue;
      }

      String interfaceNumberText;

      try
      {
        interfaceNumberText = File.ReadAllText(interfaceNumberPath).Trim();
      }
      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
      {
        return false;
      }

      return Byte.TryParse(interfaceNumberText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Byte interfaceNumber)
        && interfaceNumber == expectedInterfaceNumber;
    }

    return false;
  }
}