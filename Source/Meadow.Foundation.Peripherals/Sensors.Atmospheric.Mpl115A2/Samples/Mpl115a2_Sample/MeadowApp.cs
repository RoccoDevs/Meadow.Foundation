﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Atmospheric;
using System;
using System.Threading.Tasks;

namespace MeadowApp
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        Mpl115a2 sensor;

        public override Task Initialize()
        {
            Console.WriteLine("Initializing...");

            sensor = new Mpl115a2(Device.CreateI2cBus());

            var consumer = Mpl115a2.CreateObserver(
                handler: result =>
                {
                    Console.WriteLine($"Observer: Temp changed by threshold; new temp: {result.New.Temperature?.Celsius:N2}C, old: {result.Old?.Temperature?.Celsius:N2}C");
                },
                filter: result =>
                {
                    //c# 8 pattern match syntax. checks for !null and assigns var.
                    if (result.Old?.Temperature is { } oldTemp &&
                        result.New.Temperature is { } newTemp)
                    {
                        return (newTemp - oldTemp).Abs().Celsius > 0.5; // returns true if > 0.5°C change.
                    }
                    return false;
                }
            );
            sensor.Subscribe(consumer);

            sensor.Updated += (sender, result) => {
                Console.WriteLine($"  Temperature: {result.New.Temperature?.Celsius:N2}C");
                Console.WriteLine($"  Pressure: {result.New.Pressure?.Bar:N2}Bar");
            };

            return Task.CompletedTask;
        }

        public override async Task Run()
        {
            var conditions = await sensor.Read();
            Console.WriteLine($"Temperature: {conditions.Temperature?.Celsius}°C, Pressure: {conditions.Pressure?.Pascal}Pa");

            sensor.StartUpdating(TimeSpan.FromSeconds(1));
        }

        //<!=SNOP=>
    }
}