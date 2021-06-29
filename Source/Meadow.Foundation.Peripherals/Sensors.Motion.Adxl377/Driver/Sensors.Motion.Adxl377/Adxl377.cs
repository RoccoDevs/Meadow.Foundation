﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Foundation.Spatial;
using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Motion;
using Meadow.Units;

namespace Meadow.Foundation.Sensors.Motion
{
    // TODO: add Temperature
    /// <summary>
    ///     Driver for the ADXL377 triple axis accelerometer.
    ///     +/- 200g
    /// </summary>
    public class Adxl377 : SamplingSensorBase<Acceleration3D>, IAccelerometer
    {
        //==== events
        public event EventHandler<IChangeResult<Acceleration3D>> Acceleration3DUpdated;

        //==== internals
        /// <summary>
        /// Analog input channel connected to the x axis.
        /// </summary>
        protected readonly IAnalogInputPort xPort;

        /// <summary>
        /// Analog input channel connected to the x axis.
        /// </summary>
        protected readonly IAnalogInputPort yPort;

        /// <summary>
        /// Analog input channel connected to the x axis.
        /// </summary>
        protected readonly IAnalogInputPort zPort;

        /// <summary>
        /// Voltage that represents 0g.  This is the supply voltage / 2.
        /// </summary>
        //protected float ZeroGVoltage => SupplyVoltage / 2f;

        //==== properties
        /// <summary>
        /// Minimum value that can be used for the update interval when the
        /// sensor is being configured to generate interrupts.
        /// </summary>
        public const ushort MinimumPollingPeriod = 100;

        /// <summary>
        /// Volts per G for the X axis.
        /// </summary>
        protected double XVoltsPerG { get; }

        /// <summary>
        /// Volts per G for the X axis.
        /// </summary>
        protected double YVoltsPerG { get; }

        /// <summary>
        /// Volts per G for the X axis.
        /// </summary>
        protected double ZVoltsPerG { get; }

        /// <summary>
        /// Power supply voltage applied to the sensor.  This will be set (in the constructor)
        /// to 3.3V by default.
        /// </summary>
        protected double SupplyVoltage { get; }

        public Acceleration3D? Acceleration3D => Conditions;
        protected double range;

        /// <summary>
        /// Create a new ADXL337 sensor object.
        /// </summary>
        /// <param name="xPin">Analog pin connected to the X axis output from the ADXL337 sensor.</param>
        /// <param name="yPin">Analog pin connected to the Y axis output from the ADXL337 sensor.</param>
        /// <param name="zPin">Analog pin connected to the Z axis output from the ADXL337 sensor.</param>
        public Adxl377(IAnalogInputController device,
            IPin xPin, IPin yPin, IPin zPin)
        {
            xPort = device.CreateAnalogInputPort(xPin);
            yPort = device.CreateAnalogInputPort(yPin);
            zPort = device.CreateAnalogInputPort(zPin);

            range = 400d; // +- 200G
            //range = 6d; // +- 6G

            SupplyVoltage = 3.3f;
        }


        protected override void RaiseEventsAndNotify(IChangeResult<Acceleration3D> changeResult)
        {
            Acceleration3DUpdated?.Invoke(this, changeResult);
            base.RaiseEventsAndNotify(changeResult);
        }

        protected override Task<Acceleration3D> ReadSensor()
        {
            return Task.Run(async () => {
                var x = await xPort.Read();
                var y = await yPort.Read();
                var z = await zPort.Read();

                return new Acceleration3D(
                    new Acceleration((x.Volts - (SupplyVoltage / 2)) / (SupplyVoltage / range), Acceleration.UnitType.Gravity),
                    new Acceleration((y.Volts - (SupplyVoltage / 2)) / (SupplyVoltage / range), Acceleration.UnitType.Gravity),
                    new Acceleration((z.Volts - (SupplyVoltage / 2)) / (SupplyVoltage / range), Acceleration.UnitType.Gravity)
                    );
            });
        }

        ///// <summary>
        ///// Get the raw analog input values from the sensor.
        ///// </summary>
        ///// <returns>Vector object containing the raw sensor data from the analog pins.</returns>
        //public async Task<(Voltage XVolts, Voltage YVolts, Voltage ZVolts)> GetRawSensorData()
        //{
        //    var x = await xPort.Read();
        //    var y = await yPort.Read();
        //    var z = await zPort.Read();

        //    return (x, y, z);
        //}
    }
}