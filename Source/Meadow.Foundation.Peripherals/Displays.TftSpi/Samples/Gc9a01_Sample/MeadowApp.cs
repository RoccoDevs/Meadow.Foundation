﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Graphics;
using System.Threading.Tasks;

namespace Displays.Tft.Gc9a01_Sample
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        MicroGraphics graphics;

        public override Task Initialize()
        {
            Resolver.Log.Info("Initializing ...");

            var spiBus = Device.CreateSpiBus();

            Resolver.Log.Info("Create display driver instance");

            var display = new Gc9a01
            (
                spiBus: spiBus,
                chipSelectPin: Device.Pins.D02,
                dcPin: Device.Pins.D01,
                resetPin: Device.Pins.D00
            );

            graphics = new MicroGraphics(display)
            {
                IgnoreOutOfBoundsPixels = true,
                CurrentFont = new Font12x20()
            };

            return base.Initialize();
        }

        public override Task Run()
        {
            graphics.Clear();
            graphics.DrawTriangle(10, 10, 50, 50, 10, 50, Meadow.Foundation.Color.Red);
            graphics.DrawRectangle(20, 15, 40, 20, Meadow.Foundation.Color.Yellow, false);
            graphics.DrawCircle(50, 50, 40, Meadow.Foundation.Color.Blue, false);
            graphics.DrawText(5, 5, "Meadow F7");
            graphics.Show();

            return base.Run();
        }

        //<!=SNOP=>
    }
}