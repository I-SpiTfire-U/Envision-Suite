using System.Globalization;

namespace EnvisionSuite.Core.Discovery;

/// <summary>
///     Discovers Scuf Envision Pro V2 controller devices by scanning /sys/class.
///     Finds both evdev (input events) and hidraw (raw HID reports) devices
///     by matching the controller's USB vendor/product IDs.
/// </summary>
public static class DeviceDiscovery
{
  private const UInt16 ScufUSBVendorId = 0x1b1c;
  private const UInt16 ScufUSBProductId = 0x3a08;
  private const String ScufInputInterfaceNumber = "03";

  /// <summary>
  ///     Searches for connected Scuf Envision Pro V2 controller devices.
  ///     Scans /sys/class/input for evdev devices and /sys/class/hidraw for hidraw devices
  ///     matching the Scuf vendor/product IDs.
  /// </summary>
  /// <returns>
  ///     A <see cref="DiscoveredDevices" /> object containing paths to all controller devices,
  ///     or null if the controller is not found.
  /// </returns>
  public static DiscoveredDevices? FindController()
  {
    var (evdevPath, secondaryPaths) = FindAllEvdevDevices();
    var hidrawPath = FindHidrawDevice();

    if (evdevPath is null)
    {
      Console.Error.WriteLine($"""
      Error: Could not find Scuf Envision Pro controller evdev device.
      Looking for VID={ScufUSBVendorId:x4} PID={ScufUSBProductId:x4}
      """);
      return null;
    }

    if (hidrawPath is null)
    {
      Console.Error.WriteLine("[warning] Could not find hidraw device. R2 trigger may not work correctly.");
      hidrawPath = String.Empty;
    }

    return new DiscoveredDevices
    {
      EvdevDevicePath = evdevPath,
      HidrawDevicePath = hidrawPath,
      AdditionalEvdevPaths = secondaryPaths
    };
  }

  /// <summary>
  ///     Finds all evdev devices matching the Scuf controller.
  ///     The controller exposes multiple event devices - we identify the primary joystick
  ///     device (which has a js* handler) and collect secondary devices for grabbing.
  /// </summary>
  /// <returns>
  ///     A tuple containing the primary evdev path (or null if not found) and a list of
  ///     secondary evdev paths that should be grabbed to prevent input leakage.
  /// </returns>
  private static (String? primary, List<String> secondary) FindAllEvdevDevices()
  {
    const String inputClassPath = "/sys/class/input";
    List<(String path, Boolean hasJoystick)> allDevices = [];

    if (!Directory.Exists(inputClassPath))
    {
      return (null, []);
    }

    foreach (String eventDir in Directory.GetDirectories(inputClassPath, "event*"))
    {
      String deviceIdPath = Path.Combine(eventDir, "device", "id");
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

      try
      {
        String vendorStr = File.ReadAllText(vendorPath).Trim();
        String productStr = File.ReadAllText(productPath).Trim();

        if (UInt16.TryParse(vendorStr, NumberStyles.HexNumber, null, out UInt16 vendor) &&
            UInt16.TryParse(productStr, NumberStyles.HexNumber, null, out UInt16 product))
        {
          if (vendor == ScufUSBVendorId && product == ScufUSBProductId)
          {
            String eventName = Path.GetFileName(eventDir);
            String devPath = $"/dev/input/{eventName}";

            // Check if this device has a js* handler (indicates main joystick interface)
            // The main joystick device has EV_KEY capability for buttons
            Boolean hasJoystick = HasJoystickHandler(eventDir);

            allDevices.Add((devPath, hasJoystick));
          }
        }
      }
      catch (IOException) { } // Skip devices that cannot be read
    }

    // Sort: primary joystick device first, then secondary devices
    // The primary device is the one with the js* handler
    String? primary = null;
    List<String> secondary = [];

    foreach ((String path, Boolean hasJoystick) in allDevices)
    {
      if (hasJoystick && primary is null)
      {
        primary = path;
      }
      else
      {
        secondary.Add(path);
      }
    }

    // If no joystick device found, use the first one as primary
    if (primary is null && allDevices.Count > 0)
    {
      primary = allDevices[0].path;
      secondary = [.. allDevices.Skip(1).Select(d => d.path)];
    }

    return (primary, secondary);
  }

  /// <summary>
  ///     Checks if an event device has an associated js* (joystick) device.
  ///     The js* device indicates this is the main gamepad interface with button support,
  ///     as opposed to secondary interfaces like mouse or keyboard emulation.
  /// </summary>
  /// <param name="eventDir">Path to the event directory in /sys/class/input.</param>
  /// <returns>True if a js* sibling device exists, indicating this is the main joystick.</returns>
  private static Boolean HasJoystickHandler(String eventDir)
  {
    // Check if there's a js* device associated with this event device.
    // The js* device is a sibling of the event* device under the same input device.
    // Structure: /sys/class/input/eventX -> /devices/.../inputN/eventX
    //            /sys/class/input/jsY    -> /devices/.../inputN/jsY
    // So we check for js* siblings in the parent directory.
    try
    {
      // eventDir is like /sys/class/input/event5 (symlink to /devices/.../inputN/event5)
      // We need to resolve the symlink and check for sibling js* directories
      FileSystemInfo? linkTarget = Directory.ResolveLinkTarget(eventDir, true);
      if (linkTarget is null)
      {
        return false;
      }

      String realPath = linkTarget.FullName;
      String? parentDir = Path.GetDirectoryName(realPath);

      if (parentDir is not null && Directory.Exists(parentDir))
      {
        // Look for js* entries as siblings of the event device
        String[]? jsEntries = Directory.GetDirectories(parentDir, "js*");
        if (jsEntries.Length > 0)
        {
          return true;
        }
      }
    }
    catch (IOException) { } // Ignore errors, assume not a joystick

    return false;
  }

  /// <summary>
  ///     Finds the hidraw device for the Scuf controller by scanning /sys/class/hidraw.
  ///     Parses the HID_ID field in uevent files to match the controller's vendor/product IDs.
  /// </summary>
  /// <returns>
  ///     Path to the hidraw device (e.g., "/dev/hidraw0"), or null if not found.
  /// </returns>
  private static String? FindHidrawDevice()
  {
    const String hidrawClassPath = "/sys/class/hidraw";

    if (!Directory.Exists(hidrawClassPath))
    {
      return null;
    }

    foreach (String hidrawDir in Directory.GetDirectories(hidrawClassPath, "hidraw*"))
    {
      if (!IsUsbInterface(hidrawDir, ScufInputInterfaceNumber))
      {
        continue;
      }

      String ueventPath = Path.Combine(hidrawDir, "device", "uevent");

      if (!File.Exists(ueventPath))
      {
        continue;
      }

      try
      {
        String ueventContent = File.ReadAllText(ueventPath);

        foreach (String line in ueventContent.Split('\n'))
        {
          if (!line.StartsWith("HID_ID="))
          {
            continue;
          }

          String[] parts = line[7..].Split(':');

          if (parts.Length < 3)
          {
            continue;
          }

          if (UInt32.TryParse(parts[1], NumberStyles.HexNumber, null, out UInt32 vendor) &&
              UInt32.TryParse(parts[2],NumberStyles.HexNumber, null, out UInt32 product) &&
              vendor == ScufUSBVendorId && product == ScufUSBProductId)
          {
            return $"/dev/{Path.GetFileName(hidrawDir)}";
          }
        }
      }
      catch (IOException) { } // Device may have disappeared during enumeration.
    }

    return null;
  }

  private static Boolean IsUsbInterface(String hidrawDir, String expectedInterfaceNumber)
  {
    try
    {
      DirectoryInfo? current = Directory.ResolveLinkTarget(hidrawDir, returnFinalTarget: true) as DirectoryInfo;

      while (current is not null)
      {
        String interfaceNumberPath = Path.Combine(current.FullName, "bInterfaceNumber");

        if (File.Exists(interfaceNumberPath))
        {
          String interfaceNumber = File.ReadAllText(interfaceNumberPath).Trim();
          return interfaceNumber.Equals(expectedInterfaceNumber, StringComparison.OrdinalIgnoreCase);
        }

        current = current.Parent;
      }
    }
    catch (IOException) { } // Device may have disappeared during enumeration.

    return false;
  }
}