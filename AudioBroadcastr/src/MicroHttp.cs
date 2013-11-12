using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace EugenePetrenko.AudioBroadcastr
{
  public static class MicroHttp
  {
    public static string ExecuteToString(this HttpWebRequest request)
    {
      HttpWebResponse response;
      try
      {
        response = (HttpWebResponse)request.GetResponse();
      }
      catch (WebException we)
      {
        response = (HttpWebResponse)we.Response;
      }

      Console.Out.WriteLine(">>> {0} {1}", response.StatusCode, response.StatusDescription);
      foreach (var header in response.Headers.AllKeys)
      {
        Console.Out.WriteLine(">>>> {0}: {1}", header, response.Headers[header]);
      }

      using (var responseStream = response.GetResponseStream())
      {
        if (responseStream == null) return null;
        using (var sw = new StreamReader(responseStream))
        {
          var text = sw.ReadToEnd();
          foreach (var line in Regex.Split(text, @"[\r\n]+"))
          {
            Console.Out.WriteLine(">>>>  " + line);
          }
          Console.Out.WriteLine("");
          return text;
        }
      }
    }

  }
}