﻿using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using NAudio.CoreAudioApi;
using NAudio.Lame;
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

      Console.Out.WriteLine("Capture format: {0}", capture.WaveFormat);
      if (capture.WaveFormat is WaveFormatExtensible)
      {
        Console.Out.WriteLine("  sub-format: {0}", ((WaveFormatExtensible) capture.WaveFormat).SubFormat);
      }
      Console.Out.WriteLine("");
      

      var pipe = new PipeStream();
      capture.DataAvailable += (_, a) => pipe.PushData(a.Buffer, a.BytesRecorded);

      
      
      new MicroThreadPool().EnqueueTask(() =>
      {
        var nativeStream = WaveFormatConversionStream.CreatePcmStream(new RawSourceWaveStream(pipe, capture.WaveFormat));
        http.NewClientProxy = sw => new LameMP3FileWriter(sw, nativeStream.WaveFormat, LAMEPreset.ABR_128);
        
        var buffer = new byte[1024 * 1024];
        int sz = nativeStream.Read(buffer, 0, buffer.Length);

        http.BroadcastData(capture.WaveFormat, buffer, sz);
      });

      Console.Out.WriteLine("Loopback wave format is: {0}", capture.WaveFormat);
      capture.StartRecording();
      
      Console.Read();
      Console.Out.WriteLine("Terminating capture...");
      capture.StopRecording();
    }
  }
}

