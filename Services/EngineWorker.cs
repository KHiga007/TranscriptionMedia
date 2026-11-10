using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace TranscriptionMedia;

public static class EngineWorker
{
	private static void Reply(string kind, string text)
	{
		Console.WriteLine(JsonSerializer.Serialize(new EngineReply(kind, text)));
		Console.Out.Flush();
	}

	public static async Task Run(bool cpu, bool integrated = false)
	{
		_ = 2;
		try
		{
			Native.LowIntegrity();
			using (FileStream source = File.OpenRead(AppPaths.VoiceModel))
			{
				if (Convert.ToHexString(SHA256.HashData(source)) != "D75795ECFF3F83B5FAA89D1900604AD8C780ABD5739FAE406DE19F23ECD98AD1")
				{
					throw new IOException("El modelo está dañado o fue modificado.");
				}
			}
			RuntimeOptions.RuntimeLibraryOrder = (cpu ? new List<RuntimeLibrary>(2)
			{
				RuntimeLibrary.Cpu,
				RuntimeLibrary.CpuNoAvx
			} : new List<RuntimeLibrary>(3)
			{
				RuntimeLibrary.Vulkan,
				RuntimeLibrary.Cpu,
				RuntimeLibrary.CpuNoAvx
			});
			int device = (cpu ? (-1) : VulkanDevice.Best(integrated));
			using WhisperFactory factory = WhisperFactory.FromPath(AppPaths.VoiceModel, new WhisperFactoryOptions
			{
				UseGpu = (device >= 0),
				GpuDevice = Math.Max(0, device)
			});
			using WhisperProcessor processor = factory.CreateBuilder().WithLanguage("es").WithTemperature(0f)
				.WithTemperatureInc(0f)
				.WithBeamSearchSamplingStrategy((BeamSearchSamplingStrategyBuilder strategy) =>
				{
					strategy.WithBeamSize(5);
				})
				.WithThreads(Math.Max(1, Math.Min(4, Environment.ProcessorCount - 1)))
				.Build();
			string text;
			while ((text = await Console.In.ReadLineAsync()) != null)
			{
				try
				{
					if (text.Length > 12582912)
					{
						throw new IOException("Solicitud demasiado grande.");
					}
					EngineRequest engineRequest = JsonSerializer.Deserialize<EngineRequest>(text) ?? throw new IOException("Solicitud vacía.");
                    if((engineRequest.File==null)==(engineRequest.Samples==null))throw new IOException("Indicá un archivo o muestras de audio.");
					Reply("backend", (device >= 0 && RuntimeOptions.LoadedLibrary == RuntimeLibrary.Vulkan) ? VulkanDevice.Name : "CPU");
					if (engineRequest.File != null)
					{
						string fullPath = Path.GetFullPath(engineRequest.File);
						if (!Enumerable.Contains(new string[9] { ".wav", ".mp3", ".m4a", ".aac", ".wma", ".flac", ".ogg", ".oga", ".opus" }, Path.GetExtension(fullPath).ToLowerInvariant()))
						{
							throw new IOException("Formato no permitido.");
						}
						if (fullPath.StartsWith("\\\\") || !Path.IsPathFullyQualified(fullPath))
						{
							throw new IOException("Seleccioná un archivo local.");
						}
						if (new FileInfo(fullPath).Length > 2147483648u)
						{
							throw new IOException("El límite es 2 GB.");
						}
						Reply("progress", "0");
						using AudioFile reader = AudioFile.Open(fullPath);
						double seconds = reader.Duration.TotalSeconds;
						if (seconds > 43200.0)
						{
							throw new IOException("El límite es 12 horas.");
						}
						ISampleProvider sampleProvider = reader.Source;
						if (sampleProvider.WaveFormat.Channels == 2)
						{
							sampleProvider = new StereoToMonoSampleProvider(sampleProvider);
						}
						if (sampleProvider.WaveFormat.Channels != 1)
						{
							throw new IOException("Solo audio mono o estéreo.");
						}
						sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 16000);
						long total = 0L;
						foreach (float[] item in AudioChunks(sampleProvider))
						{
							total += item.Length;
							if (total > 691200000)
							{
								throw new IOException("El límite es 12 horas.");
							}
							await Process(processor, item);
							Reply("progress", Math.Min(99.0, (seconds > 0.0) ? ((double)total / (16000.0 * seconds) * 100.0) : ((reader.ReadFraction?.Invoke() ?? 0.0) * 100.0)).ToString(CultureInfo.InvariantCulture));
						}
					}
					else if (engineRequest.Samples != null)
					{
						byte[] array = Convert.FromBase64String(engineRequest.Samples);
						if (array.Length > 7680000 || array.Length % 4 != 0)
						{
							throw new IOException("Audio inválido.");
						}
						float[] array2 = new float[array.Length / 4];
						Buffer.BlockCopy(array, 0, array2, 0, array.Length);
                        if(array2.Any(x=>!float.IsFinite(x)))throw new IOException("El audio contiene muestras inválidas.");
						await Process(processor, array2);
					}
					Reply("done", "");
				}
				catch (Exception ex)
				{
					Reply(ex is IOException or ArgumentException or NotSupportedException or System.Runtime.InteropServices.COMException or JsonException or FormatException ? "request-error" : "error", ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Reply("error", ex2.Message);
		}
	}

	private static IEnumerable<float[]> AudioChunks(ISampleProvider source)
	{
		float[] buffer = new float[464000];
		int filled = 0;
		bool ended = false;
		while (true)
		{
			if (filled < buffer.Length && !ended)
			{
				int num = source.Read(buffer, filled, buffer.Length - filled);
				if (num == 0)
				{
					ended = true;
				}
				else
				{
					filled += num;
				}
				continue;
			}
			if (filled == 0)
			{
				break;
			}
			int cut = filled;
			if (!ended)
			{
				double num2 = double.MaxValue;
				for (int i = 320000; i + 1600 < filled; i += 800)
				{
					double num3 = 0.0;
					for (int j = i; j < i + 1600; j++)
					{
						num3 += (double)(buffer[j] * buffer[j]);
					}
					if (num3 < num2)
					{
						num2 = num3;
						cut = i + 800;
					}
				}
			}
			yield return buffer[..cut];
			int num4 = filled - cut;
			if (num4 > 0)
			{
				Array.Copy(buffer, cut, buffer, 0, num4);
			}
			filled = num4;
		}
	}

	private static async Task Process(WhisperProcessor processor, float[] samples)
	{
		if (samples.Length < 1600 || samples.All((float x) => Math.Abs(x) < 1E-06f))
		{
			return;
		}
		await foreach (SegmentData item in processor.ProcessAsync(samples))
		{
			if (!string.IsNullOrWhiteSpace(item.Text))
			{
				Reply("text", item.Text.Trim());
			}
		}
	}
}
