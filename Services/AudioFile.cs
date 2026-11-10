using System.Diagnostics;
using System.IO;
using Concentus;
using Concentus.Oggfile;
using NAudio.Vorbis;
using NAudio.Wave;

namespace TranscriptionMedia;

internal sealed class AudioFile : IDisposable
{
	private sealed class OpusSamples : ISampleProvider, IDisposable
	{
		private readonly FileStream stream;

		private readonly OpusOggReadStream reader;

		private short[] buffer = Array.Empty<short>();

		private int offset;

		public WaveFormat WaveFormat { get; }

		public TimeSpan Duration => reader.TotalTime;

		public double Fraction
		{
			get
			{
				if (stream.Length <= 0)
				{
					return 0.0;
				}
				return Math.Clamp((double)stream.Position / (double)stream.Length, 0.0, 1.0);
			}
		}

		public OpusSamples(string path, int channels)
		{
			if ((uint)(channels - 1) > 1u)
			{
				throw new IOException("Solo se admite Opus mono o estéreo.");
			}
			WaveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(48000, channels);
			stream = File.OpenRead(path);
			try
			{
				reader = new OpusOggReadStream(OpusCodecFactory.CreateDecoder(48000, channels), stream);
			}
			catch
			{
				stream.Dispose();
				throw;
			}
		}

		public int Read(float[] output, int start, int count)
		{
			int num = 0;
			int num2 = 0;
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (num < count)
			{
				if (stopwatch.Elapsed > TimeSpan.FromSeconds(20.0))
				{
					throw new IOException("El decodificador Opus no responde. El archivo podría estar dañado.");
				}
				if (offset >= buffer.Length)
				{
					if (!reader.HasNextPacket)
					{
						break;
					}
					buffer = reader.DecodeNextPacket() ?? Array.Empty<short>();
					if (buffer.Length == 0 && !string.IsNullOrEmpty(reader.LastError))
					{
						throw new IOException(reader.LastError);
					}
					offset = 0;
					if (buffer.Length == 0)
					{
						if (++num2 >= 32)
						{
							throw new IOException("El archivo Opus contiene paquetes inválidos o no permite avanzar.");
						}
						continue;
					}
					num2 = 0;
				}
				int num3 = Math.Min(count - num, buffer.Length - offset);
				for (int i = 0; i < num3; i++)
				{
					output[start + num + i] = (float)buffer[offset + i] / 32768f;
				}
				offset += num3;
				num += num3;
			}
			return num;
		}

		public void Dispose()
		{
			stream.Dispose();
		}
	}

	private readonly IDisposable resource;

	public ISampleProvider Source { get; }

	public TimeSpan Duration { get; }

	public Func<double>? ReadFraction { get; private set; }

	private AudioFile(ISampleProvider source, IDisposable resource, TimeSpan duration)
	{
		Source = source;
		this.resource = resource;
		Duration = duration;
	}

	public static AudioFile Open(string path)
	{
		bool flag;
		switch (Path.GetExtension(path).ToLowerInvariant())
		{
		case ".ogg":
		case ".oga":
		case ".opus":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			using (FileStream fileStream = File.OpenRead(path))
			{
				byte[] array = new byte[512];
				int num = fileStream.Read(array);
				if (num < 28 || !array.AsSpan(0, 4).SequenceEqual("OggS"u8) || array[4] != 0)
				{
					throw new IOException("El archivo no es un contenedor OGG válido.");
				}
				int num2 = 27 + array[26];
				if (num >= num2 + 8 && array.AsSpan(num2, 8).SequenceEqual("OpusHead"u8))
				{
					OpusSamples reader = new OpusSamples(path, (num > num2 + 9) ? array[num2 + 9] : 0);
					return new AudioFile(reader, reader, reader.Duration)
					{
						ReadFraction = () => reader.Fraction
					};
				}
				VorbisWaveReader vorbisWaveReader = new VorbisWaveReader(path);
				return new AudioFile(vorbisWaveReader.ToSampleProvider(), vorbisWaveReader, vorbisWaveReader.TotalTime);
			}
		}
		MediaFoundationReader mediaFoundationReader = new MediaFoundationReader(path);
		return new AudioFile(mediaFoundationReader.ToSampleProvider(), mediaFoundationReader, mediaFoundationReader.TotalTime);
	}

	public void Dispose()
	{
		resource.Dispose();
	}
}
