using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using NAudio.MediaFoundation;
using NAudio.SoundFont;
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
      var sound = new SoundCapture(http);

      http.Start();
      sound.Start();

      Console.Out.WriteLine();
      Console.Out.WriteLine("Press any key to exit");
      Console.Read();


      sound.Stop();
      http.Stop();

      


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
    }
  }
}

