using System;
using NAudio.CoreAudioApi;
using NAudio.Lame;
using NAudio.Wave;

namespace EugenePetrenko.AudioBroadcastr
{
  public class SoundCapture
  {
    private readonly Http myHttp;
    private WasapiLoopbackCapture myCapture;

    public SoundCapture(Http http)
    {
      myHttp = http;
      myHttp.NewClientProxy = sw => new LameMP3FileWriter(sw, myCapture.WaveFormat.Resolve(), LAMEPreset.EXTREME_FAST);
    }

    public void Start()
    {
      LogAudioDevices();

      Console.Out.WriteLine("Starting from default device...");
      myCapture = new WasapiLoopbackCapture();

      Console.Out.WriteLine("Capture format: {0}", myCapture.WaveFormat);
      Console.Out.WriteLine("");
        
      myCapture.DataAvailable += capture_DataAvailable;

      Console.Out.WriteLine("Loopback wave format is: {0}", myCapture.WaveFormat);
      myCapture.StartRecording();
    }

    private static void LogAudioDevices()
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
    }

    void capture_DataAvailable(object sender, WaveInEventArgs a)
    {
      myHttp.BroadcastData(a.Buffer, a.BytesRecorded);
    }

    public void Stop()
    {
      myCapture.StopRecording(); 
      myCapture.DataAvailable -= capture_DataAvailable;
      Console.Out.WriteLine("Terminating capture...");
    }
  }
}
