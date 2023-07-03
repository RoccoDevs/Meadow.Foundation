﻿
using Meadow.Hardware;
using System;
using System.Threading;

namespace Meadow.Foundation.ICs.IOExpanders
{

    public partial class Sc16is7x2
    {
        public class Sc16is7x2Channel : ISerialPort
        {
            /// <inheritdoc/>
            public event SerialDataReceivedEventHandler DataReceived = delegate { };
            /// <inheritdoc/>
            public event EventHandler BufferOverrun = delegate { };

            private int _baudRate;
            private int _dataBits;
            private Parity _parity;
            private StopBits _stopBits;

            /// <inheritdoc/>
            public int BytesToRead => _controller.GetReadFifoCount(_channel);
            /// <inheritdoc/>
            public bool IsOpen { get; private set; }

            /// <inheritdoc/>
            public string PortName { get; }

            /// <summary>
            /// Size of the receive buffer (FIFO).
            /// </summary>
            /// <remarks>This is fixed at 64-bytes for this hardware</remarks>
            public int ReceiveBufferSize => 64;

            /// <inheritdoc/>
            public TimeSpan ReadTimeout { get; set; } = TimeSpan.Zero;

            /// <inheritdoc/>
            public TimeSpan WriteTimeout { get; set; } = TimeSpan.Zero;

            private readonly Sc16is7x2 _controller;
            private readonly Channels _channel;
            private readonly IDigitalInterruptPort? _irq;

            internal Sc16is7x2Channel(Sc16is7x2 controller, string portName, Channels channel, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, bool isRS485 = false, bool invertDE = false, IDigitalInterruptPort? irq = null)
            {
                PortName = portName;
                _controller = controller;
                _channel = channel;

                Initialize(baudRate, dataBits, parity, stopBits);
                if (isRS485)
                {
                    InitializeRS485(invertDE);
                }

                if (irq != null)
                {
                    _irq = irq;
                    _controller.EnableReceiveInterrupts(_channel);
                    _irq.Changed += OnInterruptLineChanged;
                }
            }

            private void OnInterruptLineChanged(object sender, DigitalPortResult e)
            {
                if (_controller.ReceiveInterruptPending(_channel))
                {
                    this.DataReceived?.Invoke(this, new SerialDataReceivedEventArgs(SerialDataType.Chars));
                }
            }

            /// <inheritdoc/>
            public int BaudRate
            {
                get => _baudRate;
                set => _baudRate = _controller.SetBaudRate(_channel, value);
            }

            /// <inheritdoc/>
            public int DataBits
            {
                get => _dataBits;
                set
                {
                    _controller.SetLineSettings(_channel, value, Parity, StopBits);
                    _dataBits = value;
                }
            }

            /// <inheritdoc/>
            public Parity Parity
            {
                get => _parity;
                set
                {
                    _controller.SetLineSettings(_channel, DataBits, value, StopBits);
                    _parity = value;
                }
            }

            /// <inheritdoc/>
            public StopBits StopBits
            {
                get => _stopBits;
                set
                {
                    _controller.SetLineSettings(_channel, DataBits, Parity, value);
                    _stopBits = value;
                }
            }

            private void Initialize(int baudRate, int dataBits, Parity parity, StopBits stopBits)
            {
                _controller.Reset(_channel);
                _controller.EnableFifo(_channel);
                _baudRate = _controller.SetBaudRate(_channel, baudRate);
                _controller.SetLineSettings(_channel, dataBits, parity, stopBits);
            }

            private void InitializeRS485(bool invertDE)
            {
                _controller.EnableRS485(_channel, invertDE);
            }

            /// <inheritdoc/>
            public int ReadByte()
            {
                // check if data is available
                if (!_controller.IsFifoDataAvailable(_channel))
                {
                    return -1;
                }

                // read the data
                return _controller.ReadByte(_channel);
            }

            /// <inheritdoc/>
            public int Read(byte[] buffer, int offset, int count)
            {
                var timeout = -1;

                if (ReadTimeout.TotalMilliseconds > 0)
                {
                    timeout = Environment.TickCount + (int)ReadTimeout.TotalMilliseconds;
                }

                var toRead = _controller.GetReadFifoCount(_channel);

                for (var i = 0; i < count; i++)
                {
                    while (toRead == 0)
                    {
                        Thread.Sleep(1);
                        toRead = _controller.GetReadFifoCount(_channel);

                        if (timeout > 0)
                        {
                            if (Environment.TickCount > timeout)
                            {
                                throw new TimeoutException();
                            }
                        }
                    }

                    buffer[i + offset] = _controller.ReadByte(_channel);

                    toRead--;

                    if (toRead == 0)
                    {
                        toRead = _controller.GetReadFifoCount(_channel);
                    }
                }

                return toRead;
            }

            /// <inheritdoc/>
            public int ReadAll(byte[] buffer)
            {
                var available = _controller.GetReadFifoCount(_channel);
                return Read(buffer, 0, available);
            }

            /// <inheritdoc/>
            public void ClearReceiveBuffer()
            {
                _controller.ResetReadFifo(_channel);
            }

            /// <inheritdoc/>
            public void Close()
            {
                IsOpen = false;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                // nop
            }

            /// <inheritdoc/>
            public void Open()
            {
                IsOpen = true;
            }

            /// <inheritdoc/>
            public int Peek()
            {
                // TODO: read and buffer the 1 byte for the next actual read
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public int Write(byte[] buffer)
            {
                return Write(buffer, 0, buffer.Length);
            }

            /// <inheritdoc/>
            public int Write(byte[] buffer, int offset, int count)
            {
                var timeout = -1;
                var index = offset;
                var remaining = count;

                if (ReadTimeout.TotalMilliseconds > 0)
                {
                    timeout = Environment.TickCount + (int)ReadTimeout.TotalMilliseconds;
                }

                // write until we're either written all of the THR is full
                var available = _controller.GetReadFifoCount(_channel);

                while (remaining > 0)
                {
                    while (available <= 0)
                    {
                        Thread.Sleep(1);
                        available = _controller.GetReadFifoCount(_channel);

                        if (timeout > 0)
                        {
                            if (Environment.TickCount > timeout)
                            {
                                throw new TimeoutException();
                            }
                        }
                    }

                    _controller.WriteByte(_channel, buffer[index]);
                    index++;
                    available--;
                    remaining--;

                    if (available == 0)
                    {
                        available = _controller.GetReadFifoCount(_channel);
                    }
                }

                return count;
            }
        }
    }
}