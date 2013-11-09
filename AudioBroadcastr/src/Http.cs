using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EugenePetrenko.AudioBroadcastr
{
  public class Http
  {
    private readonly MicroThreadPool myPool = new MicroThreadPool();
    private volatile bool myIsRunning;
    private TcpListener myServer;
    private event Action<byte[], int> OnData;
    public Func<Stream, Stream> NewClientProxy = x=>x;
    
    public void BroadcastData(byte[] data, int sz)
    {
      if (OnData != null)
        OnData(data, sz);
    }

    public void Start()
    {
      myServer = new TcpListener(IPAddress.Any, 9765);
      myServer.Start();
      Console.WriteLine("Listening for connections at {0}", myServer.LocalEndpoint);

      myIsRunning = true;
      myPool.EnqueueTask(ProcessRequest);
    }

    public void Stop()
    {
      myIsRunning = false;
      myExitEvent.Set();
      myServer.Stop();
    }

    private void ProcessRequest()
    {
      int id = 10000;
      while (myIsRunning)
      {
        var newConn = myServer.AcceptTcpClient();
        myPool.EnqueueTask(() => ProcessRequest(id++, newConn));
      }
    }

    private void ProcessRequest(int id, TcpClient newConn)
    {
      var iep = (IPEndPoint) newConn.Client.RemoteEndPoint;
      Console.WriteLine("Connected with a client: {0}: {1} ", iep.Address, iep.Port);

      using (var stream = newConn.GetStream())
      using (var sr = new StreamReader(stream))
      using (var sw = new StreamWriter(stream))
      {
        var request = sr.ReadLine();
        if (request == null) return;
        
        Console.Out.WriteLine();
        Console.Out.WriteLine("{1} Request: {0}", request, id);
        while (true)
        {
          var headers = sr.ReadLine();
          if (headers == null || headers.Trim() == "") break;
          Console.Out.WriteLine("{1}  {0}", headers, id);
        }
        var path = request.Split(' ')[1];

        if (path.StartsWith("/mp3"))
        {
          sw.WriteLine("HTTP/1.1 200 OK");
          sw.WriteLine("Content-Type: audio/mpeg");
          sw.WriteLine("Transfer-Encoding: identity");
          sw.WriteLine("Connection: close");
          sw.WriteLine();
          sw.Flush();

          HandleStreamingClient(id, sw.BaseStream);      
        }
        else
        {
          sw.Write("HTTP/1.1 404 Not Found\r\n");
          sw.Write("Content-Type: text/plain; charset=utf-8\r\n");
          sw.Write("\r\n");
          sw.Write("\r\n");
          sw.Write("\r\n");
          sw.Write("No Data For you now\r\n");
          sw.Flush();
        }
      }
    }


    private readonly ManualResetEvent myExitEvent = new ManualResetEvent(false);

    private void HandleStreamingClient(int id, Stream output)
    {
      try
      {
        var exitEvent = new ManualResetEvent(false);
        var sw = NewClientProxy(output);

        Action<byte[], int> handler = null;
        handler = (bytes, i) =>
        {
          try
          {
            sw.Write(bytes, 0, i);
          }
          catch (Exception e)
          {
            LogException(id, e);
            OnData -= handler;
            exitEvent.Set();
          }
        };

        OnData += handler;

        WaitHandle.WaitAny(new WaitHandle[] {myExitEvent, exitEvent});

        OnData -= handler;
      }
      catch (Exception e)
      {
        LogException(id, e);
      }
    }

    private static void LogException(int id, Exception e)
    {
      if (e is IOException && e.InnerException != null)
      {
        LogException(id, e.InnerException);
        return;
      }

      var innerSocket = e as SocketException;
      if (innerSocket != null)
      {
        Console.Out.WriteLine(id + "  " + e.Message);
        return;
      }

      Console.Out.WriteLine(id + " " + e.Message);
      var lines = e.ToString()
        .Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.TrimEnd())
        .Where(x => x.Length > 0);

      foreach (var line in lines)
      {
        Console.Out.WriteLine(id + "    " + line);
      }
    }
  }

}
