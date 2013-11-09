using System;
using NAudio.Wave;

namespace EugenePetrenko.AudioBroadcastr
{
  public static class Utils
  {
    public static Action<WaveFormat, byte[], int> CatchAll(params Action<WaveFormat, byte[], int>[] handlers)
    {
      return (a, b, c) =>
      {
        foreach (var _onData in handlers)
        {
          try
          {
            _onData(a, b, c);
          }
          catch (Exception e)
          {
            Console.Out.WriteLine(e);
          }
        }
      };
    }
  }
}