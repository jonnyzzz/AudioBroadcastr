using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace EugenePetrenko.AudioBroadcastr
{
  /// <summary>
  /// Readable stream
  /// </summary>
  public class PipeStream : Stream
  {
    private readonly AutoResetEvent myEvent = new AutoResetEvent(false);
    private readonly List<byte> myData = new List<byte>();

    public void PushData(byte[] data, int sz)
    {
      if (sz == 0) return;

      lock (myData)
      {
        myData.AddRange(data.Take(sz));
      }
      myEvent.Set();
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      int i = 0;
      lock (myData)
      {
        foreach (var b in myData)
        {
          buffer[offset + i++] = b;
          if (i >= count) break;
        }
        myData.RemoveRange(0, i);
      }

      if (i != 0) return i;
      
      //wait for some more data
      myEvent.WaitOne();
      return Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotImplementedException();
    }

    public override bool CanRead
    {
      get { return true; }
    }

    public override bool CanSeek
    {
      get { return false; }
    }

    public override bool CanWrite
    {
      get { return false; }
    }

    public override long Length
    {
      get { throw new NotImplementedException(); }
    }

    public override long Position { get; set; }
  }
}