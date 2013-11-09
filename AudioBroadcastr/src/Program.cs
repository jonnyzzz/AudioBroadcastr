using System;
using System.IO;
using System.Net;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using ServiceDiscovery;

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

      var http = new Http();
      http.Start();

/*
      var x = new SDServiceDiscovery();
      x.ServiceFound += (service, ownService) => Console.Out.WriteLine("Discovered service: {0}", service);
      x.ServiceDiscoveryError += _ => Console.Out.WriteLine("Discovery error");

      const string SERVICE_NAME = "_raop._tcp";
      if (!x.SearchForServices(SERVICE_NAME))
      {
        Console.Out.WriteLine("ERRROR");
      }

*/

      StartStreaming(http.BroadcastData);

      Console.In.ReadLine();
    }

    private static void StartStreaming(Action<byte[], int> onData)
    {
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

      capture.DataAvailable += (sender, eventArgs) => onData(eventArgs.Buffer, eventArgs.BytesRecorded);

      Console.Out.WriteLine("Loopback wave format is: {0}", capture.WaveFormat);
      capture.StartRecording();
    }
  }
}

