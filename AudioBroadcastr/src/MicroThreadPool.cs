using System;
using System.Threading;

namespace EugenePetrenko.AudioBroadcastr
{
  public class MicroThreadPool
  {
    public void EnqueueTask(Action action)
    {
      var th = new Thread(() => action());
      th.Name = "Micto pooled thread";
      th.IsBackground = true;
      th.Start();
    }
  }
}