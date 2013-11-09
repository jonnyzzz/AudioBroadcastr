using System;
using System.IO;
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
      while (myIsRunning)
      {
        var newConn = myServer.AcceptTcpClient();

        myPool.EnqueueTask(() => ProcessRequest(newConn));
      }
    }

    private void ProcessRequest(TcpClient newConn)
    {
      var iep = (IPEndPoint) newConn.Client.RemoteEndPoint;
      Console.WriteLine("Connected with a client: {0}: {1} ", iep.Address, iep.Port);

      using (var stream = newConn.GetStream())
      using (var sr = new StreamReader(stream))
      using (var sw = new StreamWriter(stream))
      {
        var request = sr.ReadLine();
        if (request == null) return;
        
        Console.Out.WriteLine("Request: {0}", request);
        var path = request.Split(' ')[1];

        if (path.StartsWith("/mp3"))
        {
          sw.WriteLine("HTTP/1.1 200 OK");
          sw.WriteLine("Content-Type: audio/mpeg");
          sw.WriteLine("Transfer-Encoding: identity");
          sw.WriteLine("Connection: close");
          sw.WriteLine();
          sw.Flush();

          HandleStreamingClient(sw.BaseStream);      
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


        //read all data from request
        String line;
        while ((line = sr.ReadLine()) != null)
        {
          if (line.Length == 0) break;
        }
      }
    }


    private readonly ManualResetEvent myExitEvent = new ManualResetEvent(false);

    private void HandleStreamingClient(Stream output)
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
        catch(Exception e)
        {
          Console.Out.WriteLine(e);
          OnData -= handler;
          exitEvent.Set();
        }
      };

      OnData += handler;
      
      WaitHandle.WaitAny(new WaitHandle[]{myExitEvent, exitEvent});

      OnData -= handler;
    }
  }

}
