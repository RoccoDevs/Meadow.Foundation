﻿using Meadow.Hardware;
using Meadow.Peripherals.Sensors;
using Meadow.Units;
using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Foundation.Sensors.Distance
{
    /// <summary>
    /// Represents the ME007YS serial distance sensor
    /// </summary>
    public class Me007ys : PollingSensorBase<Length>, IRangeFinder, ISleepAwarePeripheral
    {
        /// <summary>
        /// Raised when the value of the reading changes
        /// </summary>
        public event EventHandler<IChangeResult<Length>> DistanceUpdated = delegate { };

        /// <summary>
        /// Distance from sensor to object
        /// </summary>
        public Length? Distance { get; protected set; }

        //The baud rate is 9600, 8 bits, no parity, with one stop bit
        readonly ISerialPort serialPort;

        static readonly int portSpeed = 9600;

        static readonly byte[] readBuffer = new byte[16];

        private TaskCompletionSource<Length> dataReceivedTaskCompletionSource;

        bool createdSerialPort = false;


        /// <summary>
        /// Creates a new ME007YS object communicating over serial
        /// </summary>
        /// <param name="device">The device conected to the sensor</param>
        /// <param name="serialPortName">The serial port</param>
        public Me007ys(IMeadowDevice device, SerialPortName serialPortName)
            : this(device.CreateSerialPort(serialPortName, portSpeed))
        {
            createdSerialPort = true;
        }

        /// <summary>
        /// Creates a new ME007YS object communicating over serial
        /// </summary>
        /// <param name="serialMessage">The serial message port</param>
        public Me007ys(ISerialPort serialMessage)
        {
            serialPort = serialMessage;
        }

        /// <summary>
        /// Start a distance measurement
        /// </summary>
        public void MeasureDistance()
        {
            _ = Read();
        }

        /// <summary>
        /// Convenience method to get the current sensor reading
        /// </summary>
        public override Task<Length> Read()
        {
            return ReadSensor();
        }

        /// <summary>
        /// Read the distance from the sensor
        /// </summary>
        /// <returns></returns>
        protected override Task<Length> ReadSensor()
        {
            return ReadSingleValueAsync();
        }

        /// <summary>
        /// Raise distance change event for subscribers
        /// </summary>
        /// <param name="changeResult"></param>
        protected override void RaiseEventsAndNotify(IChangeResult<Length> changeResult)
        {
            DistanceUpdated?.Invoke(this, changeResult);
            base.RaiseEventsAndNotify(changeResult);
        }

        /// <summary>
        /// Start updating distances
        /// </summary>
        /// <param name="updateInterval">The interval used to notify external subscribers</param>
        public override void StartUpdating(TimeSpan? updateInterval)
        {
            lock (samplingLock)
            {
                if(serialPort.IsOpen == false) 
                {
                    serialPort.Open();
                }

                base.StartUpdating(updateInterval);
            }
        }

        /// <summary>
        /// Stop sampling 
        /// </summary>
        public override void StopUpdating()
        {
            lock (samplingLock)
            {
                serialPort?.Close();
                base.StopUpdating();
            }
        }

        //This sensor will write a single byte of 0xFF alternating with 
        //3 bytes: 2 bytes for distance and a 3rd for the checksum
        //when 3 bytes are available we know we have a distance reading ready
        public async Task<Length> ReadSingleValueAsync()
        {
            if (serialPort.IsOpen == false)
                serialPort.Open();

            dataReceivedTaskCompletionSource = new TaskCompletionSource<Length>();

            serialPort.DataReceived += SerialPortDataReceived;

            var result = await dataReceivedTaskCompletionSource.Task;

            serialPort.DataReceived -= SerialPortDataReceived;

            return result;
        }

        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var len = serialPort.BytesToRead;
            serialPort.Read(readBuffer, 0, Math.Min(len, readBuffer.Length));
            if (len == 3)
            {
                var mm = readBuffer[0] << 8 | readBuffer[1];

                if (mm != 0)
                {
                    var length = new Length(mm, Length.UnitType.Millimeters);
                    dataReceivedTaskCompletionSource.SetResult(length);
                }
            }
        }

        public Task BeforeSleep(CancellationToken cancellationToken)
        {
            if(createdSerialPort && serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
            return Task.CompletedTask;
        }

        public Task AfterWake(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}