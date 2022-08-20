﻿using Meadow.Hardware;

namespace Meadow.Foundation.ICs.IOExpanders
{
    /// <summary>
    /// Represent an MCP23017 I2C port expander
    /// </summary>
    public class Mcp23017 : Mcp23x17
    {
        /// <summary>
        /// Creates an Mcp23017 object
        /// </summary>
        /// <param name="i2cBus">The I2C bus</param>
        /// <param name="address">The I2C address</param>
        /// <param name="interruptPort">The interrupt port</param>
        public Mcp23017(II2cBus i2cBus, byte address = 32, IDigitalInputPort interruptPort = null) :
            base(i2cBus, address, interruptPort)
        { }
    }
}