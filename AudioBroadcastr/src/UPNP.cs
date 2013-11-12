using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using NAudio.SoundFont;

namespace EugenePetrenko.AudioBroadcastr
{
  public class UPNP
  {
    private readonly MicroThreadPool myPool = new MicroThreadPool();
    private readonly HashSet<string> myResolvedRenderers = new HashSet<string>();
    private readonly Http myMP3;
    private int myMulticastListenPort = 32223;

    public UPNP(Http mp3)
    {
      myMP3 = mp3;
    }

    public void DetectAndStart()
    {
      var multicastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
      var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
        new MulticastOption(multicastEndPoint.Address, IPAddress.Any));
      socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 80);
      socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
      socket.Bind(new IPEndPoint(IPAddress.Any, myMulticastListenPort++));

      Console.WriteLine("UDP-Socket setup done...\r\n");

      const string broadcastString =
        "M-SEARCH * HTTP/1.1\r\n" +
        "Host:239.255.255.250:1900\r\n" +
        "ST:urn:schemas-upnp-org:device:MediaRenderer:1\r\n" +
        "Man:\"ssdp:discover\"\r\n" +
        "MX:3\r\n" +
        "USER-AGENT: jonnyzzz\r\n" +
        "\r\n";

      myPool.EnqueueTask(() =>
      {
        bool gotResponse = false;
        var endTime = DateTime.Now + TimeSpan.FromSeconds(5);
        while (endTime > DateTime.Now)
        {
          if (socket.Available < 10)
          {
            Thread.Sleep(TimeSpan.FromMilliseconds(5));
            continue;
          }

          var buff = new byte[65536];
          var sz = socket.Receive(buff, SocketFlags.None);
          if (sz > 0)
          {
            myPool.EnqueueTask(() => ProcessResponse(buff, sz));
            gotResponse = true;
          }
        }

        if (!gotResponse)
        {
          Console.Out.WriteLine("No serivces were detected via UPNP. Retrying...");
          myPool.EnqueueTask(DetectAndStart);
        }

        Console.Out.WriteLine("M-Search completed");
      });

      Thread.Sleep(TimeSpan.FromMilliseconds(500));
      var message = Encoding.UTF8.GetBytes(broadcastString);
      socket.SendTo(message, SocketFlags.None, multicastEndPoint);
      Console.WriteLine("M-Search multicast sent...");
    }

    private void ProcessResponse(byte[] data, int sz)
    {
      Console.Out.WriteLine(">>>");

      //HTTP/1.1 200 OK
      //CACHE-CONTROL: max-age=1800
      //EXT:
      //LOCATION: http://192.168.2.105:8080/description.xml
      //SERVER: KnOS/3.2 UPnP/1.0 DMP/3.5
      //ST: urn:schemas-upnp-org:device:MediaRenderer:1
      //USN: uuid:5F9EC1B3-ED59-79BB-4530-00E036EE9B18::urn:schemas-upnp-org:device:MediaRenderer:1

      string url = null;
      var text = Encoding.UTF8.GetString(data, 0, sz);
      foreach (var line in text.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries))
      {
        Console.WriteLine(">>> {0}", line);

        var x = line.Trim();
        if (x.StartsWith("LOCATION:", StringComparison.InvariantCultureIgnoreCase))
        {
          x = x.Substring("LOCATION:".Length).Trim();
          if (x.Length > 0) url = x;
        }
      }

      if (url != null)
      {
        OnMediaRendererDetected(url);
      }
    }

    private void OnMediaRendererDetected(string url)
    {
      lock (myResolvedRenderers)
      {
        if (!myResolvedRenderers.Add(url)) return;
      }
      Console.Out.WriteLine("   Detected media rendered @ " + url);
      Console.Out.WriteLine("   Loading service metadata...");

      var request = (HttpWebRequest) WebRequest.Create(url);
      request.UserAgent = "jonnyzzz";
      request.Accept = "text/xml";

      var doc = new XmlDocument();
      doc.LoadXml(request.ExecuteToString());

      var nm = new XmlNamespaceManager(doc.NameTable);
      nm.AddNamespace("x", "urn:schemas-upnp-org:device-1-0");

      var node =
        doc.SelectSingleNode(
          "/x:root" +
          "/x:device" +
          "/x:serviceList" +
          "/x:service[x:serviceType/text() = \"urn:schemas-upnp-org:service:AVTransport:1\"]" +
          "/x:controlURL" +
          "/text()",
          nm);

      if (node == null) return;

      string AVTransport = node.Value;
      Console.Out.WriteLine("Control URL: " + AVTransport);

      var baseUrl = new Uri(url);
      var avUrl = new Uri(baseUrl, AVTransport);


      StartThePlay(avUrl.ToString());
    }

    private void StartThePlay(string av)
    {
      Console.Out.WriteLine("Starting music... @ " + new Uri(av).Host);
      SetStream(av);
      PlayStream(av);
    }

    private void SetStream(string av)
    {
      var request = (HttpWebRequest) WebRequest.Create(av);
      request.UserAgent = "jonnyzzz";
      request.Method = "POST";
      request.ContentType = "text/xml; charset=\"utf-8\"";
      request.Headers.Add("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI\"");

      string resolveMp3StreamUrl = myMP3.ResolveMp3StreamUrl(av);

      Console.Out.WriteLine("Opening: {0}", resolveMp3StreamUrl);

      var envelope = @"<?xml version=""1.0""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
             s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <s:Body>
    <u:SetAVTransportURI xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
      <InstanceID>0</InstanceID>
      <CurrentURI>" + resolveMp3StreamUrl + @"</CurrentURI>
      <CurrentURIMetaData></CurrentURIMetaData>
    </u:SetAVTransportURI>
  </s:Body> 
</s:Envelope>";

      byte[] buffer = Encoding.UTF8.GetBytes(envelope);
      var requestStream = request.GetRequestStream();
      requestStream.Write(buffer, 0, buffer.Length);
      requestStream.Close();

      request.ExecuteToString();
    }

    private static void PlayStream(string av)
    {
      var request = (HttpWebRequest) WebRequest.Create(av);
      request.UserAgent = "jonnyzzz";
      request.Method = "POST";
      request.ContentType = "text/xml; charset=\"utf-8\"";
      request.Headers.Add("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");

      var envelope = @"<?xml version=""1.0""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
             s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <s:Body>
    <u:Play xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
      <InstanceID>0</InstanceID>
      <Speed>1</Speed>
    </u:Play>
  </s:Body> 
</s:Envelope>";

      byte[] buffer = Encoding.UTF8.GetBytes(envelope);
      var requestStream = request.GetRequestStream();
      requestStream.Write(buffer, 0, buffer.Length);
      requestStream.Close();

      request.ExecuteToString();
    }

    private static void GetInfo()
    {
      var request = (HttpWebRequest) WebRequest.Create("http://192.168.2.105:8080/ConnectionManager/ctrl");
      request.Method = "POST";
      request.ContentType = "text/xml; charset=\"utf-8\"";
      request.SendChunked = false;
      request.Headers.Add("SOAPAction", "\"urn:schemas-upnp-org:service:ConnectionManager:1#GetProtocolInfo\"");

      var envelope = @"<?xml version=""1.0""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
             s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <s:Body>
    <u:GetProtocolInfo xmlns:u=""urn:schemas-upnp-org:service:ConnectionManager:1"">
    </u:GetProtocolInfo>
  </s:Body> 
</s:Envelope>";

      byte[] buffer = Encoding.UTF8.GetBytes(envelope);
      var requestStream = request.GetRequestStream();
      requestStream.Write(buffer, 0, buffer.Length);
      requestStream.Close();


      request.ExecuteToString();
    }

  }
}
