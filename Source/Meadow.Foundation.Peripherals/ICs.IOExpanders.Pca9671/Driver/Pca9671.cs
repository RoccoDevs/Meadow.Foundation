﻿using Meadow.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Meadow.Foundation.ICs.IOExpanders
{
    /// <summary>
    /// Represents a PCA9671 I2C IO Expander
    /// </summary>
    public partial class Pca9671 : I2cCommunications, IDigitalOutputController, IDigitalInputController, II2cPeripheral, IDisposable
    {
        private ushort _outputs;
        private ushort _directionMask; // inputs must be set to logic 1 (data sheet section 8.1)
        private readonly List<IPin> _pinsInUse = new List<IPin>();
        private bool _isDisposed;
        private IDigitalOutputPort? _resetPort;

        /// <inheritdoc/>
        public byte DefaultI2cAddress => 0x20;

        /// <inheritdoc/>
        public PinDefinitions Pins { get; private set; }

        /// <summary>
        /// Creates a new Pca9671 instance
        /// </summary>
        /// <param name="bus">The I2C buss the peripheral is connected to</param>
        /// <param name="peripheralAddress">The bus address of the peripheral</param>
        /// <param name="resetPin">The optional pin connected tothe peripheral's reset</param>
        public Pca9671(II2cBus bus, byte peripheralAddress, IPin? resetPin = default)
            : base(bus, peripheralAddress, 8, 8)
        {
            Pins = new PinDefinitions(this);
            Init(resetPin);
        }

        /// <inheritdoc/>
        public IDigitalOutputPort CreateDigitalOutputPort(IPin pin, bool initialState = false, OutputType initialOutputType = OutputType.PushPull)
        {
            lock (_pinsInUse)
            {
                if (_pinsInUse.Contains(pin))
                {
                    throw new PortInUseException($"{this.GetType().Name} pin {pin.Name} is already in use.");
                }
                var port = new DigitalOutputPort(this, pin, initialState);
                _pinsInUse.Add(pin);

                port.Disposed += (s, e) =>
                {
                    lock (_pinsInUse)
                    {
                        _pinsInUse.Add(pin);
                    }
                };

                return port;
            }
        }

        /// <inheritdoc/>
        public IDigitalInputPort CreateDigitalInputPort(IPin pin, ResistorMode resistorMode)
        {
            switch (resistorMode)
            {
                case ResistorMode.InternalPullUp:
                case ResistorMode.InternalPullDown:
                    throw new ArgumentException("Internal resistors are not supported");
            }

            lock (_pinsInUse)
            {
                if (_pinsInUse.Contains(pin))
                {
                    throw new PortInUseException($"{this.GetType().Name} pin {pin.Name} is already in use.");
                }
                var port = new DigitalInputPort(this, pin);
                _pinsInUse.Add(pin);

                _directionMask |= (ushort)(1 << (byte)pin.Key);

                port.Disposed += (s, e) =>
                {
                    lock (_pinsInUse)
                    {
                        _pinsInUse.Add(pin);
                    }
                };

                return port;
            }
        }

        private void Init(IPin? resetPin = default)
        {
            if (resetPin != null)
            {
                _resetPort = resetPin.CreateDigitalOutputPort(true);
            }

            Reset();
            AllOff();
        }

        /// <inheritdoc/>
        public IPin GetPin(string pinName)
            => Pins.AllPins.FirstOrDefault(p => p.Name == pinName || p.Key.ToString() == p.Name);

        /// <inheritdoc/>
        protected bool IsValidPin(IPin pin)
            => Pins.AllPins.Contains(pin);

        /// <summary>
        /// Resets the peripheral
        /// </summary>
        public void Reset()
        {
            if (_resetPort is null)
            {
                return;
            }

            _resetPort.State = false;
            Thread.Sleep(1);
            _resetPort.State = true;
        }

        /// <summary>
        /// Convenience method to turn all outputs off
        /// </summary>
        public void AllOff()
        {
            WriteState(0x0000);
        }

        /// <summary>
        /// Convenience method to turn all outputs on
        /// </summary>
        public void AllOn()
        {
            WriteState(0xffff);
        }

        /// <summary>
        /// Retrieves the state of a pin
        /// </summary>
        /// <param name="pin">The pin to query</param>
        public bool GetState(IPin pin)
        {
            // if it's an input, read it, otherwise reflect what we wrote

            var pinMask = 1 << ((byte)pin.Key);
            if ((pinMask & _directionMask) != 0)
            {
                // this is an actual input, so read
                var state = ReadState();
                return (state & pinMask) != 0;
            }

            // this is an output, just reflect what we've been told to write
            return (_outputs & pinMask) != 0;
        }

        /// <summary>
        /// Sets the state of a pin
        /// </summary>
        /// <param name="pin">The pin to affect</param>
        /// <param name="state"><b>True</b> to set the pin state high, <b>False</b> to set it low</param>
        public void SetState(IPin pin, bool state)
        {
            var offset = (byte)pin.Key;
            if (state)
            {
                _outputs |= (ushort)(1 << offset);
            }
            else
            {
                _outputs &= (ushort)~(1 << offset);
            }

            WriteState(_outputs);
        }

        private enum PinDirection
        {
            Output = 0,
            Input = 1
        }

        /// <summary>
        /// Reads the peripheral state register
        /// </summary>
        protected ushort ReadState()
        {
            Span<byte> buffer = stackalloc byte[2];
            Bus.Read(Address, buffer);
            return (ushort)((buffer[0] << 8) | buffer[1]);
        }

        /// <summary>
        /// Writes the peripheral state register
        /// </summary>
        protected void WriteState(ushort state)
        {
            state |= _directionMask;
            Span<byte> buffer = stackalloc byte[] { (byte)(state & 0xff), (byte)(state >> 8) };
            Bus.Write(Address, buffer);
        }

        /// <summary>
        /// Disposes the instances resources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_resetPort != null)
                    {
                        _resetPort.Dispose();
                        _resetPort = null;
                    }
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposes the instances resources
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}