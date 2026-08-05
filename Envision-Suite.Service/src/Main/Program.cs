using EnvisionSuite.Core.Discovery;
using EnvisionSuite.Core.Input;
using EnvisionSuite.Core.Output;
using EnvisionSuite.Core.Services;

namespace EnvisionSuite.Service.src.Main;

public static class Program
{
  public static Int32 Main()
  {
    using CancellationTokenSource cancellationTokenSource = new();
    RegisterShutdownHandler(cancellationTokenSource);

    Console.WriteLine("SCUF Envision Pro V2 to Xbox Controller Bridge\nSearching for SCUF Envision Pro controller...");
    DiscoveredDevices? discoveredDevices = DeviceDiscovery.FindController();
    if (discoveredDevices is null)
    {
      WriteControllerNotFoundError();
      return (Int32)ExitCode.ControllerNotFound;
    }

    String hidrawDeviceStatus = String.IsNullOrEmpty(discoveredDevices.HidrawDevicePath)
      ? "not found (R2 trigger may not work)" : discoveredDevices.HidrawDevicePath;
    String additionalEvdevStatus = discoveredDevices.AdditionalEvdevPaths.Length > 0
      ? $"{discoveredDevices.AdditionalEvdevPaths.Length} found" : "None found";

    Console.WriteLine($"""
    Found evdev device: {discoveredDevices.EvdevDevicePath}
    Additional evdev devices: {additionalEvdevStatus}
    Found hidraw device: {hidrawDeviceStatus}
    """);

    Console.WriteLine("Opening input devices...");
    using EvdevReader? evdevReader = EvdevReader.Open(discoveredDevices.EvdevDevicePath);
    if (evdevReader is null)
    {
      Console.Error.WriteLine("Failed to open evdev device.");
      return (Int32)ExitCode.InputDeviceUnavailable;
    }

    using HidrawReader? hidrawReader = HidrawReader.Open(discoveredDevices.HidrawDevicePath);

    ExitCode exitCode = RunBridge(discoveredDevices, evdevReader, hidrawReader, cancellationTokenSource.Token);
    return (Int32)exitCode;
  }

  private static ExitCode RunBridge(DiscoveredDevices discoveredDevices, EvdevReader evdevReader, HidrawReader? hidrawReader, CancellationToken cancellationToken)
  {
    List<EvdevReader> additionalEvdevDevices = [];

    try
    {
      foreach (String additionalEvdevPath in discoveredDevices.AdditionalEvdevPaths)
      {
        EvdevReader? additionalEvdevReader = EvdevReader.Open(additionalEvdevPath);
        String resultMessage = $"[warning] Failed to grab {additionalEvdevPath}";

        if (additionalEvdevReader is not null)
        {
          additionalEvdevDevices.Add(additionalEvdevReader);
          resultMessage = $"Grabbed: {additionalEvdevPath}";
        }

        Console.WriteLine(resultMessage);
      }

      Console.WriteLine("\nCreating virtual Xbox controller...");
      using VirtualGamepad? virtualGamepad = VirtualGamepad.Create();
      if (virtualGamepad is null)
      {
        Console.Error.WriteLine("""
        Failed to create virtual gamepad.
        Make sure uinput module is loaded: sudo modprobe uinput
        """);
        return ExitCode.VirtualGamepadUnavailable;
      }

      Console.WriteLine("Created: Xbox Elite 2 Virtual Controller\n");

      BridgeService bridge = new(evdevReader, hidrawReader, virtualGamepad);
      bridge.Run(cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      return ExitCode.Success;
    }
    catch (Exception exception)
    {
      Console.Error.WriteLine($"Error: {exception.Message}");
      return ExitCode.Failure;
    }
    finally
    {
      for (Int32 index = additionalEvdevDevices.Count - 1; index >= 0; index--)
      {
        additionalEvdevDevices[index].Dispose();
      }
    }

    return ExitCode.Success;
  }

  private static void RegisterShutdownHandler(CancellationTokenSource cancellationTokenSource)
  {
    Console.CancelKeyPress += (_, e) =>
    {
      e.Cancel = true;
      cancellationTokenSource.Cancel();
      Console.WriteLine("\nShutdown requested...");
    };
  }

  private static void WriteControllerNotFoundError()
  {
    Console.Error.WriteLine("""
    Troubleshooting:
      1. Make sure the controller is connected
      2. Check if the device appears in: ls /dev/input/event*
      3. Check permissions: ls -la /dev/input/

    To grant permissions, create /etc/udev/rules.d/99-EnvisionSuite.Core.rules:
      SUBSYSTEM=="input", ATTRS{idVendor}=="1b1c", ATTRS{idProduct}=="3a05", MODE="0666"
      SUBSYSTEM=="hidraw", ATTRS{idVendor}=="1b1c", ATTRS{idProduct}=="3a05", MODE="0666"
      KERNEL=="uinput", MODE="0666"

    Then reload udev: sudo udevadm control --reload && sudo udevadm trigger
    """);
  }
}