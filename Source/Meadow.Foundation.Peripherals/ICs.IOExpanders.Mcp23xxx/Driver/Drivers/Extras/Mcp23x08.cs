﻿using Meadow.Hardware;
using System.Linq;

namespace Meadow.Foundation.ICs.IOExpanders
{
    /// <summary>
    /// Represent an MCP23x08 I2C/SPI port expander
    /// </summary>
    public partial class Mcp23x08 : Mcp23xxx
    {
        /// <summary>
        /// MCP23x08 pin definitions
        /// </summary>
        public PinDefinitions Pins { get; } = new PinDefinitions();

        /// <summary>
        /// Is the pin valid for this device instance
        /// </summary>
        /// <param name="pin">The IPin to validate</param>
        /// <returns>True if pin is valid</returns>
        protected override bool IsValidPin(IPin pin) => Pins.AllPins.Contains(pin);

        /// <summary>
        /// Creates an Mcp23008 object
        /// </summary>
        /// <param name="i2cBus">The I2C bus</param>
        /// <param name="address">The I2C address</param>
        /// <param name="interruptPort">The interrupt port</param>
        public Mcp23x08(II2cBus i2cBus, byte address = 32, IDigitalInputPort interruptPort = null) : base(i2cBus, address, interruptPort)
        {
        }

        /// <summary>
        /// Creates an Mcp23s08 object
        /// </summary>
        /// <param name="spiBus">The SPI bus connected to the Mcp23x08</param>
        /// <param name="chipSelectPort">Chip select port</param>
        /// <param name="interruptPort">optional interupt port, needed for input interrupts</param>
        public Mcp23x08(ISpiBus spiBus, IDigitalOutputPort chipSelectPort, IDigitalInputPort interruptPort = null) :
            base(new SpiMcpDeviceComms(spiBus, chipSelectPort), interruptPort) // use the internal constructor that takes an IMcpDeviceComms
        {
        }

        public override IPin GetPin(string pinName)
        {
            return Pins.AllPins.FirstOrDefault(p => p.Name == pinName || p.Key.ToString() == p.Name);
        }
    }
}