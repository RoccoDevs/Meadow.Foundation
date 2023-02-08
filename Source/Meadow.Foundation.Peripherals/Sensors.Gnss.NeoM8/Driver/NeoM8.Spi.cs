﻿using Meadow.Hardware;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Foundation.Sensors.Gnss
{
    public partial class NeoM8
    {
        readonly ISpiPeripheral spiPeripheral;

        const byte NULL_VALUE = 0xFF;

        /// <summary>
        /// Create a new NEOM8 object using SPI
        /// </summary>
        public NeoM8(ISpiBus spiBus, 
            IDigitalOutputPort chipSelectPort, 
            IDigitalOutputPort resetPort = null, 
            IDigitalInputPort ppsPort = null)
        {
            ResetPort = resetPort;
            PulsePerSecondPort = ppsPort;

            spiPeripheral = new SpiPeripheral(spiBus, chipSelectPort);

            _ = InitializeSpi();
        }

        /// <summary>
        /// Create a new NeoM8 object using SPI
        /// </summary>
        public NeoM8(IMeadowDevice device, ISpiBus spiBus, IPin chipSelectPin = null, IPin resetPin = null, IPin ppsPin = null)
        {
            var chipSelectPort = device.CreateDigitalOutputPort(chipSelectPin);

            spiPeripheral = new SpiPeripheral(spiBus, chipSelectPort);

            if (resetPin != null)
            {
                device.CreateDigitalOutputPort(resetPin, true);
            }

            if (ppsPin != null)
            {
                device.CreateDigitalInputPort(ppsPin, InterruptMode.EdgeRising, ResistorMode.InternalPullDown);
            }

            _ = InitializeSpi();
        }

        //ToDo cancellation for sleep aware 
        async Task InitializeSpi()
        {
            messageProcessor = new SerialMessageProcessor(suffixDelimiter: Encoding.ASCII.GetBytes("\r\n"),
                                                    preserveDelimiter: true,
                                                    readBufferSize: 512);

            communicationMode = CommunicationMode.SPI;
            messageProcessor.MessageReceived += MessageReceived;

            InitDecoders();

            await Reset();

            Resolver.Log.Debug("Finish NeoM8 SPI initialization");
        }

        async Task StartUpdatingSpi()
        { 
            byte[] data = new byte[BUFFER_SIZE];

            static bool HasMoreData(byte[] data)
            {
                bool hasNullValue = false;
                for(int i = 1; i < data.Length; i++)
                {
                    if (data[i] == NULL_VALUE) { hasNullValue = true; }
                    if (data[i - 1] == NULL_VALUE && data[i] != NULL_VALUE)
                    {
                        return true;
                    }
                }
                return !hasNullValue;
            }

            await Task.Run(() =>
            {
                while (true)
                {
                    spiPeripheral.Read(data);
                    messageProcessor.Process(data);

                    if(HasMoreData(data) == false)
                    {
                        Thread.Sleep(COMMS_SLEEP_MS);
                    }
                }
            });
        }
    }
}