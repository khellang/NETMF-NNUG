using System.Threading;
using GHIElectronics.NETMF.FEZ;
using Microsoft.SPOT.Hardware;

namespace NETMFHelloWorld.Drivers
{
    public class LED
    {
        private OutputPort led;
        private bool ledState;
        private bool shouldBlink;

        public LED(FEZ_Pin.Digital pin, bool initState)
        {
            led = new OutputPort((Cpu.Pin) pin, initState);
            ledState = initState;
            shouldBlink = false;
        }

        private void Blink()
        {
            while (shouldBlink)
            {
                Thread.Sleep(500);

                ledState = !ledState;
                led.Write(ledState);
            }
            ledState = false;
            led.Write(ledState);
        }

        public void StartBlink()
        {
            shouldBlink = true;
            new Thread(Blink).Start();
        }

        public void StopBlink()
        {
            shouldBlink = false;
        }
    }
}