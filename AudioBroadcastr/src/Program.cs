using System;
using System.IO;
using System.Net;
using System.Text;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.SoundFont;
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
      StartStreaming(http);
      

      Console.Read();
    }

    private static byte[] GenerateWavHeader(WaveFormat format)
    {
      var ms = new MemoryStream();
      var writer = new BinaryWriter(ms);

      writer.Write(Encoding.UTF8.GetBytes("RIFF"));
      writer.Write(0);
      writer.Write(Encoding.UTF8.GetBytes("WAVE"));
      writer.Write(Encoding.UTF8.GetBytes("fmt "));
      format.Serialize(writer);

      writer.Write(Encoding.UTF8.GetBytes("fact"));
      writer.Write(4);
      writer.Flush();
      writer.Write(0);

      writer.Write(Encoding.UTF8.GetBytes("data"));
      writer.Close();
      ms.Close();
      return ms.GetBuffer();
    }

    private static void StartStreaming(Http http)
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

      http.OnNewClient = result => result.Write(GenerateWavHeader(capture.WaveFormat));
      capture.DataAvailable += (sender, eventArgs) => http.BroadcastData(capture.WaveFormat, eventArgs.Buffer, eventArgs.BytesRecorded);

      Console.Out.WriteLine("Loopback wave format is: {0}", capture.WaveFormat);
      capture.StartRecording();

      
      Console.Read();
      Console.Out.WriteLine("Terminating capture...");
      capture.StopRecording();
    }
  }
}

