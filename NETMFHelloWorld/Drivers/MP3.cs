/*
Copyright 2010 GHI Electronics LLC
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. 
*/

using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace GHIElectronics.NETMF.FEZ
{
    static public class MP3
    {
        // Some GPIO pins
        static private OutputPort _reset;
        static private InputPort _DREQ;

        // Define SPI Configuration for MP3 decoder
        static SPI _spi;
        static private SPI.Configuration _dataConfig;
        static private SPI.Configuration _cmdConfig;

        // Values
        const ushort SM_SDINEW = 0x800;
        const ushort SM_RESET = 0x04;

        // Registers
        const int SCI_MODE = 0x00;
        const int SCI_VOL = 0x0B;
        const int SCI_CLOCKF = 0x03;

        static private byte[] block = new byte[32];
        static private byte[] cmdBuffer = new byte[4];

        static public void Initialize()
        {
            SPI.SPI_module spi_module;
            spi_module = SPI.SPI_module.SPI1;

            _dataConfig = new SPI.Configuration((Cpu.Pin)FEZ_Pin.Digital.Di2, false, 0, 0, false, true, 2000, spi_module);
            _cmdConfig = new SPI.Configuration((Cpu.Pin)FEZ_Pin.Digital.Di9, false, 0, 0, false, true, 2000, spi_module);
            // _reset = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.UEXT4, true); //Reset pin is not connected in the this shield
            _DREQ = new InputPort((Cpu.Pin)FEZ_Pin.Digital.Di3, false, Port.ResistorMode.PullUp);

            _spi = new SPI(_dataConfig);

            Reset();

            CommandWrite(SCI_MODE, SM_SDINEW);
            CommandWrite(SCI_CLOCKF, 0x98 << 8);
            CommandWrite(SCI_VOL, 0x2828);  // highest volume -1

            if (CommandRead(SCI_VOL) != (0x2828))
            {
                throw new Exception("Failed to initialize MP3 Decoder.");
            }

        }


        static public void SetVolume(byte left_channel, byte right_channel)
        {
            CommandWrite(SCI_VOL, (ushort)((255 - left_channel) << 8 | (255 - right_channel)));
        }

        private static void Reset()
        {
            while (_DREQ.Read() == false) ;
            CommandWrite(SCI_MODE, (ushort)(CommandRead(SCI_MODE) | SM_RESET));
            Thread.Sleep(1);
            while (_DREQ.Read() == false) ;
            Thread.Sleep(100);
        }

        static private void CommandWrite(byte address, ushort data)
        {
            while (_DREQ.Read() == false)
                Thread.Sleep(1);

            _spi.Config = _cmdConfig;
            cmdBuffer[0] = 0x02;
            cmdBuffer[1] = address;
            cmdBuffer[2] = (byte)(data >> 8);
            cmdBuffer[3] = (byte)data;

            _spi.Write(cmdBuffer);

        }

        static private ushort CommandRead(byte address)
        {
            ushort temp;

            while (_DREQ.Read() == false)
                Thread.Sleep(1);

            _spi.Config = _cmdConfig;
            cmdBuffer[0] = 0x03;
            cmdBuffer[1] = address;
            cmdBuffer[2] = 0;
            cmdBuffer[3] = 0;

            _spi.WriteRead(cmdBuffer, cmdBuffer, 2);

            temp = cmdBuffer[0];
            temp <<= 8;

            temp += cmdBuffer[1];

            return temp;
        }

        static public void SendData(byte[] data)
        {
            int size = data.Length - data.Length % 32;

            _spi.Config = _dataConfig;
            for (int i = 0; i < size; i += 32)
            {

                while (_DREQ.Read() == false)
                    Thread.Sleep(1);  // wait till done

                Array.Copy(data, i, block, 0, 32);

                _spi.Write(block);
            }
        }
    }
}