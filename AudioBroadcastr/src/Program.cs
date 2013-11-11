using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Sockets;
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
      Console.Out.WriteLine("This is an AudioBroadcastr to capture 'whatUhear' audio");
      Console.Out.WriteLine("(C) Eugene Petrenko 2013");
      Console.Out.WriteLine("");

/*
      var http = new Http();
      var sound = new SoundCapture(http);

      http.Start();
      sound.Start();

      Console.Out.WriteLine();
      Console.Out.WriteLine("Press any key to exit");
      Console.Read();


      sound.Stop();
      http.Stop();

*/
      


      /*var x = new SDServiceDiscovery();
      x.ServiceFound += (service, ownService) => Console.Out.WriteLine("Discovered service: {0}", service);
      x.ServiceDiscoveryError += _ => Console.Out.WriteLine("Discovery error");

      const string SERVICE_NAME = "MediaRenderer:1";
      if (!x.SearchForServices(SERVICE_NAME))
      {
        Console.Out.WriteLine("ERRROR");
      }

       */

      //dummycast();
      GetInfo();
      SetStream();
      PlayStream();
      Console.Out.WriteLine("!!!");
      
      
      Console.Read();
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


      ProcessResponse(request);
    }

    private static void ProcessResponse(HttpWebRequest request)
    {
      Console.Out.WriteLine("Content sent");

      HttpWebResponse response;
      try
      {
        response = (HttpWebResponse) request.GetResponse();
      }
      catch (WebException we)
      {
        response = (HttpWebResponse) we.Response;
      }

      foreach (var header in response.Headers.AllKeys)
      {
        Console.Out.WriteLine("{0} = {1}", header, response.Headers[header]);
      }

      Console.Out.WriteLine("" + response.StatusCode);
      using (var sw = new StreamReader(response.GetResponseStream()))
      {
        Console.Out.WriteLine(sw.ReadToEnd());
      }
    }

    private static void SetStream()
    {
      var request = (HttpWebRequest) WebRequest.Create("http://192.168.2.105:8080/AVTransport/ctrl");
      request.Method = "POST";
      request.ContentType = "text/xml; charset=\"utf-8\"";
      request.Headers.Add("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI\"");

      var envelope = @"<?xml version=""1.0""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
             s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <s:Body>
    <u:SetAVTransportURI xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
      <InstanceID>0</InstanceID>
      <CurrentURI>http://192.168.2.103:9765/mp3.mp3</CurrentURI>
      <CurrentURIMetaData></CurrentURIMetaData>
    </u:SetAVTransportURI>
  </s:Body> 
</s:Envelope>";

      byte[] buffer = Encoding.UTF8.GetBytes(envelope);
      var requestStream = request.GetRequestStream();
      requestStream.Write(buffer, 0, buffer.Length);
      requestStream.Close();


      ProcessResponse(request);
    }

    private static void PlayStream()
    {
      var request = (HttpWebRequest) WebRequest.Create("http://192.168.2.105:8080/AVTransport/ctrl");
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


      ProcessResponse(request);
    }

    private static void dummycast()
    {
      IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Any, 31900);
      IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

      Socket UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

      UdpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      UdpSocket.Bind(LocalEndPoint);
      UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
        new MulticastOption(MulticastEndPoint.Address, IPAddress.Any));
      UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
      UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);

      Console.WriteLine("UDP-Socket setup done...\r\n");

      string SearchString =
        "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nST:ssdp:all\r\nMX:3\r\n\r\n";

      UdpSocket.SendTo(Encoding.UTF8.GetBytes(SearchString), SocketFlags.None, MulticastEndPoint);

      Console.WriteLine("M-Search sent...\r\n");

      byte[] ReceiveBuffer = new byte[64000];

      int ReceivedBytes = 0;

      while (true)
      {
        if (UdpSocket.Available > 0)
        {
          ReceivedBytes = UdpSocket.Receive(ReceiveBuffer, SocketFlags.None);

          if (ReceivedBytes > 0)
          {
            Console.WriteLine(Encoding.UTF8.GetString(ReceiveBuffer, 0, ReceivedBytes));
          }
        }
      }
    }
  }
}

