# Meadow.Foundation.Sensors.Motion.Hcsens0040

**HCSENS0040 digital microwave motion sensor**

The **Hcsens0040** library is designed for the [Wilderness Labs](www.wildernesslabs.co) Meadow .NET IoT platform and is part of [Meadow.Foundation](https://developer.wildernesslabs.co/Meadow/Meadow.Foundation/).

The **Meadow.Foundation** peripherals library is an open-source repository of drivers and libraries that streamline and simplify adding hardware to your C# .NET Meadow IoT application.

For more information on developing for Meadow, visit [developer.wildernesslabs.co](http://developer.wildernesslabs.co/).

To view all Wilderness Labs open-source projects, including samples, visit [github.com/wildernesslabs](https://github.com/wildernesslabs/).

## Usage

```csharp
private Hcsens0040 sensor;

public override Task Initialize()
{
    Resolver.Log.Info("Initialize...");

    sensor = new Hcsens0040(Device.CreateDigitalInterruptPort(Device.Pins.D05, Meadow.Hardware.InterruptMode.EdgeBoth));
    sensor.OnMotionDetected += Sensor_OnMotionDetected;

    return Task.CompletedTask;
}

private void Sensor_OnMotionDetected(object sender)
{
    Resolver.Log.Info($"Motion detected {DateTime.Now}");
}

```
## How to Contribute

- **Found a bug?** [Report an issue](https://github.com/WildernessLabs/Meadow_Issues/issues)
- Have a **feature idea or driver request?** [Open a new feature request](https://github.com/WildernessLabs/Meadow_Issues/issues)
- Want to **contribute code?** Fork the [Meadow.Foundation](https://github.com/WildernessLabs/Meadow.Foundation) repository and submit a pull request against the `develop` branch


## Need Help?

If you have questions or need assistance, please join the Wilderness Labs [community on Slack](http://slackinvite.wildernesslabs.co/).
