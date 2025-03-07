﻿using Meadow.Hardware;
using Meadow.Peripherals.Motors;
using Meadow.Units;
using System;

namespace Meadow.Foundation.Motors;

/// <summary>
/// Generic h-bridge motor controller.
/// </summary>
public class HBridgeMotor : IDCMotor
{
    private static readonly Frequency DefaultFrequency = new Frequency(1600, Frequency.UnitType.Hertz);

    /// <summary>
    /// PWM port for left motor
    /// </summary>
    protected IPwmPort motorLeftPwm; // H-Bridge 1A pin
    /// <summary>
    /// PWM port for right motor
    /// </summary>
    protected IPwmPort motorRighPwm; // H-Bridge 2A pin
    /// <summary>
    /// Digital output port to enable h-bridge
    /// </summary>
    protected IDigitalOutputPort enablePort; // if enabled, then IsNeutral = false

    /// <summary>
    /// When true, the wheels spin "freely"
    /// </summary>
    public bool IsNeutral
    {
        get => isNeutral;
        set
        {
            isNeutral = value;
            // if neutral, we disable the port
            enablePort.State = !isNeutral;
        }
    }

    private bool isNeutral = true;

    /// <summary>
    /// The power applied to the motor, as a percentage between
    /// `-1.0` and `1.0`.
    /// </summary>
    public float Power
    {
        get => power;
        set
        {
            motorLeftPwm.Stop();
            motorRighPwm.Stop();

            power = value;

            var calibratedSpeed = power * MotorCalibrationMultiplier;
            var absoluteSpeed = Math.Min(Math.Abs(calibratedSpeed), 1);
            var isForward = calibratedSpeed > 0;

            motorLeftPwm.DutyCycle = (isForward) ? absoluteSpeed : 0;
            motorRighPwm.DutyCycle = (isForward) ? 0 : absoluteSpeed;
            IsNeutral = false;

            motorLeftPwm.Start();
            motorRighPwm.Start();
        }
    }

    private float power = 0;

    /// <summary>
    /// The frequency of the PWM used to drive the motors. 
    /// Default value is 1600.
    /// </summary>
    public Frequency PwmFrequency => motorLeftPwm.Frequency;

    /// <summary>
    /// Not all motors are created equally. This number scales the Speed Input so
    /// that you can match motor speeds without changing your logic.
    /// </summary>
    public float MotorCalibrationMultiplier { get; set; } = 1;

    /// <summary>
    /// Create an HBridgeMotor object
    /// </summary>
    /// <param name="a1Pin"></param>
    /// <param name="a2Pin"></param>
    /// <param name="enablePin"></param>
    public HBridgeMotor(IPin a1Pin, IPin a2Pin, IPin enablePin) :
        this(a1Pin.CreatePwmPort(DefaultFrequency), a2Pin.CreatePwmPort(DefaultFrequency), enablePin.CreateDigitalOutputPort(), DefaultFrequency)
    { }

    /// <summary>
    /// Create an HBridgeMotor object
    /// </summary>
    /// <param name="a1Pin"></param>
    /// <param name="a2Pin"></param>
    /// <param name="enablePin"></param>
    /// <param name="pwmFrequency"></param>
    public HBridgeMotor(IPin a1Pin, IPin a2Pin, IPin enablePin, Frequency pwmFrequency) :
        this(a1Pin.CreatePwmPort(DefaultFrequency), a2Pin.CreatePwmPort(DefaultFrequency), enablePin.CreateDigitalOutputPort(), pwmFrequency)
    { }

    /// <summary>
    /// Create an HBridgeMotor object
    /// </summary>
    /// <param name="a1Port"></param>
    /// <param name="a2Port"></param>
    /// <param name="enablePort"></param>
    public HBridgeMotor(IPwmPort a1Port, IPwmPort a2Port, IDigitalOutputPort enablePort)
        : this(a1Port, a2Port, enablePort, DefaultFrequency)
    { }

    /// <summary>
    /// Create an HBridgeMotor object
    /// </summary>
    /// <param name="a1Port"></param>
    /// <param name="a2Port"></param>
    /// <param name="enablePort"></param>
    /// <param name="pwmFrequency"></param>
    public HBridgeMotor(IPwmPort a1Port, IPwmPort a2Port, IDigitalOutputPort enablePort, Frequency pwmFrequency)
    {
        motorLeftPwm = a1Port;
        motorLeftPwm.Frequency = pwmFrequency;
        motorLeftPwm.Start();

        motorRighPwm = a2Port;
        motorRighPwm.Frequency = pwmFrequency;
        motorRighPwm.Start();

        this.enablePort = enablePort;
    }
}