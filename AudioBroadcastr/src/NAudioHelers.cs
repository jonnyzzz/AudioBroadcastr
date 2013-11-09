using System;
using NAudio.Wave;

namespace EugenePetrenko.AudioBroadcastr
{
  public static class NAudioHelers
  {
    private static readonly Guid MEDIASUBTYPE_IEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");
    private static readonly Guid MEDIASUBTYPE_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71");

    public static WaveFormat Resolve(this WaveFormat format)
    {
      var extensible = format as WaveFormatExtensible;
      if (extensible != null)
      {
        Guid subFormat = extensible.SubFormat;
        if (subFormat == MEDIASUBTYPE_IEEE_FLOAT)
          return WaveFormat.CreateIeeeFloatWaveFormat(extensible.SampleRate, extensible.Channels);

        if (subFormat == MEDIASUBTYPE_PCM)
          return new WaveFormat(extensible.SampleRate, extensible.Channels);
      }
      return format;
    }
  }
}
