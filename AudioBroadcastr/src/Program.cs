using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace EugenePetrenko.AudioBroadcastr
{
  public class Program
  {
    [STAThread]
    static void Main(string[] args)
    {
      Console.Out.WriteLine("This is an AudioBroadcastr to capture 'whatUhead' audio");
      Console.Out.WriteLine("(C) Eugene Petrenko 2013");
      Console.Out.WriteLine("");

      new Http().Start();

      Console.Out.WriteLine("Looking for audio capture devices:");
      for (int n = 0; n < WaveIn.DeviceCount; n++)
      {
        var dev = WaveIn.GetCapabilities(n);
        Console.Out.WriteLine("  Device: {0}, channels={1}", dev.ProductName, dev.Channels);
      }

      Console.Out.WriteLine("Looking for audio MMD:");
      var mmd = new MMDeviceEnumerator();
      var mddDev = mmd.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
      Console.Out.WriteLine("  [default] {0} {1}", mddDev.ID, mddDev.FriendlyName);

      Console.Out.WriteLine("Starting from default device");
      var capture = new WasapiLoopbackCapture();

      int sz = 0;
      capture.DataAvailable += (sender, eventArgs) =>
        {
          sz += eventArgs.BytesRecorded;
          if (sz%1024 == 0) Console.Out.WriteLine(".");
        };

      Console.Out.WriteLine("Loopback wave format is: {0}", capture.WaveFormat);
      capture.StartRecording();


    }
  }
}

