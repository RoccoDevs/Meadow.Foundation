﻿using Meadow.Hardware;
using Meadow.Peripherals.Sensors;
using Meadow.Peripherals.Sensors.Atmospheric;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Foundation.Sensors.Temperature
{
    /// <summary>
    /// TMP102 Temperature sensor object.
    /// </summary>    
    public class Tmp102 :
        FilterableChangeObservable<CompositeChangeResult<Units.Temperature>, Units.Temperature?>,
        ITemperatureSensor
    {
        /// <summary>
        ///     Indicate the resolution of the sensor.
        /// </summary>
        public enum Resolution : byte
        {
            /// <summary>
            ///     Operate in 12-bit mode.
            /// </summary>
            Resolution12Bits,

            /// <summary>
            ///     Operate in 13-bit mode.
            /// </summary>
            Resolution13Bits
        }

        /// <summary>
        ///     TMP102 sensor.
        /// </summary>
        private readonly II2cPeripheral tmp102;

        /// <summary>
        ///     Backing variable for the SensorResolution property.
        /// </summary>
        private Resolution _sensorResolution;

        /// <summary>
        ///     Get / set the resolution of the sensor.
        /// </summary>
        public Resolution SensorResolution {
            get { return _sensorResolution; }
            set {
                var configuration = tmp102.ReadRegisters(0x01, 2);
                if (value == Resolution.Resolution12Bits) {
                    configuration[1] &= 0xef;
                } else {
                    configuration[1] |= 0x10;
                }
                tmp102.WriteRegisters(0x01, configuration);
                _sensorResolution = value;
            }
        }

        /// <summary>
        /// The temperature, in degrees celsius (°C), from the last reading.
        /// </summary>
        public Units.Temperature? Temperature { get; protected set; }

        // internal thread lock
        private object _lock = new object();
        private CancellationTokenSource SamplingTokenSource;

        /// <summary>
        /// Gets a value indicating whether the analog input port is currently
        /// sampling the ADC. Call StartSampling() to spin up the sampling process.
        /// </summary>
        /// <value><c>true</c> if sampling; otherwise, <c>false</c>.</value>
        public bool IsSampling { get; protected set; } = false;

        public event EventHandler<CompositeChangeResult<Units.Temperature>> TemperatureUpdated = delegate { };

        /// <summary>
        ///     Create a new TMP102 object using the default configuration for the sensor.
        /// </summary>
        /// <param name="address">I2C address of the sensor.</param>
        public Tmp102(II2cBus i2cBus, byte address = 0x48)
        {
            tmp102 = new I2cPeripheral(i2cBus, address);

            var configuration = tmp102.ReadRegisters(0x01, 2);

            _sensorResolution = (configuration[1] & 0x10) > 0 ?
                                 Resolution.Resolution13Bits : Resolution.Resolution12Bits;
        }

        /// <summary>
        /// Convenience method to get the current sensor readings. For frequent reads, use
        /// StartSampling() and StopSampling() in conjunction with the SampleBuffer.
        /// </summary>
        // TODO: Make this async?
        public Units.Temperature Read()
        {
            Update();
            return Temperature;
        }

        /// <summary>
		/// Begin reading temperature data
		/// </summary>
        public void StartUpdating(int standbyDuration = 1000)
        {
            // thread safety
            lock (_lock) {
                if (IsSampling) return;

                // state muh-cheen
                IsSampling = true;

                SamplingTokenSource = new CancellationTokenSource();
                CancellationToken ct = SamplingTokenSource.Token;

                Units.Temperature oldtemperature;
                CompositeChangeResult<Units.Temperature> result;
                Task.Factory.StartNew(async () => 
                {
                    while (true) {
                        if (ct.IsCancellationRequested) {
                            // do task clean up here
                            observers.ForEach(x => x.OnCompleted());
                            break;
                        }
                        // capture history
                        oldtemperature = Temperature;

                        // read
                        Update(); //syncrhnous for this driver 

                        // build a new result with the old and new conditions
                        result = new CompositeChangeResult<Units.Temperature>(oldtemperature, Temperature);

                        // let everyone know
                        RaiseChangedAndNotify(result);

                        // sleep for the appropriate interval
                        await Task.Delay(standbyDuration);
                    }
                }, SamplingTokenSource.Token);
            }
        }

        /// <summary>
        /// Stops sampling the temperature.
        /// </summary>
        public void StopUpdating()
        {
            lock (_lock) 
            {
                if (!IsSampling) { return; }

                SamplingTokenSource?.Cancel();

                IsSampling = false;
            }
        }

        /// <summary>
        /// Update the Temperature property.
        /// </summary>
        public void Update()
        {
            var temperatureData = tmp102.ReadRegisters(0x00, 2);

            var sensorReading = 0;
            if (SensorResolution == Resolution.Resolution12Bits) {
                sensorReading = (temperatureData[0] << 4) | (temperatureData[1] >> 4);
            } else {
                sensorReading = (temperatureData[0] << 5) | (temperatureData[1] >> 3);
            }

            Temperature = new Units.Temperature((float)(sensorReading * 0.0625), Units.Temperature.UnitType.Celsius);            
        }

        protected void RaiseChangedAndNotify(CompositeChangeResult<Units.Temperature> changeResult)
        {
            TemperatureUpdated?.Invoke(this, changeResult);
            base.NotifyObservers(changeResult);
        }
    }
}