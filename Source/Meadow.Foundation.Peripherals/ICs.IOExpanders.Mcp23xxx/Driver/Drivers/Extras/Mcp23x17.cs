﻿using Meadow.Hardware;
using System.Linq;

namespace Meadow.Foundation.ICs.IOExpanders
{
    /// <summary>
    /// Represent an MCP23x17 I2C port expander
    /// </summary>
    public partial class Mcp23x17 : Mcp23xxx
    {
        /// <summary>
        /// MCP23x17 pin definitions
        /// </summary>
        public PinDefinitions Pins { get; } = new PinDefinitions();

        /// <summary>
        /// Is the pin valid for this device instance
        /// </summary>
        /// <param name="pin">The IPin to validate</param>
        /// <returns>True if pin is valid</returns>
        protected override bool IsValidPin(IPin pin) => Pins.AllPins.Contains(pin);

        /// <summary>
        /// Creates an Mcp23017 object
        /// </summary>
        /// <param name="i2cBus">The I2C bus</param>
        /// <param name="address">The I2C address</param>
        /// <param name="interruptPort">The interrupt port</param>
        public Mcp23x17(II2cBus i2cBus, byte address = 32, IDigitalInputPort interruptPort = null) 
            : base(i2cBus, address, interruptPort)
        {
        }

        /// <summary>
        /// Creates an Mcp23s17 object
        /// </summary>
        /// <param name="spiBus">The SPI bus</param>
        /// <param name="chipSelectPort">The chip select port</param>
        /// <param name="interruptPort">The interrupt port</param>
        public Mcp23x17(ISpiBus spiBus, IDigitalOutputPort chipSelectPort, IDigitalInputPort interruptPort = null) : base(spiBus, chipSelectPort, interruptPort)
        {
        }

        /// <summary>
        /// Get the pin from the name
        /// </summary>
        /// <param name="pinName">The pin name to look up</param>
        /// <returns>IPin reference if found</returns>
        public override IPin GetPin(string pinName)
        {
            return Pins.AllPins.FirstOrDefault(p => p.Name == pinName || p.Key.ToString() == p.Name);
        }
    }
}