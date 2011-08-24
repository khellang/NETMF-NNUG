using NETMFHelloWorld.Drivers;
using GHIElectronics.NETMF.FEZ;
using System.Threading;
using GHIElectronics.NETMF.USBHost;
using GHIElectronics.NETMF.IO;
using Microsoft.SPOT.IO;
using Microsoft.SPOT;
using System.IO;
namespace NETMFHelloWorld
{
    public class Program
    {
        public static void Main()
        {
            #region Part1 - Hello LED!

            var led = new LED(FEZ_Pin.Digital.Di5, false);

            #endregion

            #region Part2 - Hello Mp3!

            MP3.Initialize();

            string volumeRoot = null;
            var volumeReady = new AutoResetEvent(false);

            USBHostController.DeviceConnectedEvent += (device) =>
                {
                    if (device.TYPE != USBH_DeviceType.MassStorage)
                    {
                        return;
                    }
                    var storage = new PersistentStorage(device);
                    storage.MountFileSystem();
                };

            RemovableMedia.Insert += (s, e) =>
                {
                    volumeRoot = e.Volume.RootDirectory;
                    volumeReady.Set();
                };

            Debug.Print("Wiating for MassStorage..");
            volumeReady.WaitOne();

            var files = Directory.GetFiles(volumeRoot);
            int bufferSize = 2048;
            var buffer = new byte[bufferSize];

            Debug.Print("MassStorage detected, getting files...");
            foreach (var file in files)
            {
                if (!FileIsMp3(file))
                {
                    continue;
                }
                led.StartBlink();

                Debug.Print("Playing " + file);

                using (var stream = File.OpenRead(file))
                {
                    int bytesRead;
                    do
                    {
                        bytesRead = stream.Read(buffer, 0, bufferSize);
                        MP3.SendData(buffer);
                    } while (bytesRead > 0);
                    led.StopBlink();
                    Thread.Sleep(500);
                }
            }

            #endregion
        }

        private static bool FileIsMp3(string fileName)
        {
            var temp = fileName.Split('.');
            var ext = temp[temp.Length - 1];
            return ext.ToUpper() == "MP3";
        }
    }
}
