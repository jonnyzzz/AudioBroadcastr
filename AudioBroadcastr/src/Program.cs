using System;

namespace EugenePetrenko.AudioBroadcastr
{
  public static class Program
  {
    [STAThread]
    private static void Main(string[] args)
    {
      Console.Out.WriteLine("This is an AudioBroadcastr to capture 'whatUhear' audio");
      Console.Out.WriteLine("(C) Eugene Petrenko 2013");
      Console.Out.WriteLine("");

      var http = new Http();
      var sound = new SoundCapture(http);

      http.Start();
      sound.Start();

      new UPNP(http).DetectAndStart();

      Console.Out.WriteLine();
      Console.Out.WriteLine("Press any key to exit");
      Console.Read();


      sound.Stop();
      http.Stop();
    }
  }
}

