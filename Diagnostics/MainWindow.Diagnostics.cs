using System.Windows.Controls.Primitives;
using NAudio.Wave.SampleProviders;
using Concentus;
using Concentus.Oggfile;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.Wave;

namespace TranscriptionMedia;

public partial class MainWindow
{
	private void ValidateCompactUi(string reportPath)
	{
		ShowHome();
		UpdateLayout();
		double width = ActualWidth;
		double height = ActualHeight;
		Capture("-inicio");
		for (int i = 0; i < 2; i++)
		{
			ShowUpload();
			if (uploadView.Visibility != Visibility.Visible)
			{
				throw new Exception("Sección de carga ausente");
			}
			Capture("-carga");
		}
		if (entries.Count == 0)
		{
			throw new Exception("Sin historial de prueba");
		}
		Entry entry = entries[0];
		for (int j = 0; j < 2; j++)
		{
			ShowHistory();
			if (historyView.Visibility != Visibility.Visible)
			{
				throw new Exception("Sección de historial ausente");
			}
			history.UpdateLayout();
			Capture("-historial");
			OpenEntry(entry);
			if (transcript.Text != entry.Text || workspaceView.Visibility != Visibility.Visible)
			{
				throw new Exception("Abrir historial no recuperó el texto");
			}
		}
		Capture("-texto");
		ShowWorkspace(microphone: true, "Micrófono");
		Capture("-grabacion-compacta");
		ShowHome();
		void Capture(string suffix)
		{
			UpdateLayout();
			if (Math.Abs(ActualWidth - width) > 0.5 || Math.Abs(ActualHeight - height) > 0.5)
			{
				throw new Exception($"El tamaño de la ventana cambió en {suffix}: {ActualWidth} × {ActualHeight}");
			}
			RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);
			renderTargetBitmap.Render(this);
			PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder
			{
				Frames = { BitmapFrame.Create(renderTargetBitmap) }
			};
			using FileStream stream = File.Create(System.IO.Path.ChangeExtension(reportPath, null) + suffix + ".png");
			pngBitmapEncoder.Save(stream);
		}
	}

	public void UiSmokeTest(string reportPath)
	{
		long? num = null;
		try
		{
			if (Width != 580.0 || SizeToContent != SizeToContent.Height || homeView.Visibility != Visibility.Visible)
			{
				throw new Exception("Tamaño de inicio incorrecto");
			}
			num = Save("Prueba de interfaz", "Texto de prueba de la interfaz.");
			ValidateCompactUi(reportPath);
			File.WriteAllText(reportPath, "PASS: inicio de 580 de ancho con alto según el contenido; mismo tamaño en todas las secciones; carga, historial, abrir texto y grabación; vuelta al inicio.");
		}
		catch (Exception ex)
		{
			File.WriteAllText(reportPath, "FAIL: " + ex);
		}
		finally
		{
			if (num.HasValue && controller.HistoryAvailable)
			{
				controller.DeleteHistory(false, num.Value);
			}
			controller.Dispose();
			Application.Current.Shutdown();
		}
	}

	public async Task FlowTest(string reportPath)
	{
		_ = 1;
		try
		{
			suppressCompletionAlert = true;
			string audio = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath))!, "prueba-es.wav");
			ShowUpload();
			SelectAudio(audio);
			confirm.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
			if (activeFileTask == null)
			{
				throw new Exception("No se inició la transcripción desde el modal");
			}
			await activeFileTask;
			string expected = transcript.Text;
			if (string.IsNullOrWhiteSpace(expected) || workspaceView.Visibility != Visibility.Visible || progressBar.Value != 100.0)
			{
				throw new Exception("Resultado final ausente");
			}
			await Task.Delay(1500);
			if (transcript.Text != expected)
			{
				throw new Exception("El texto desapareció después de finalizar");
			}
			Entry entry = entries.FirstOrDefault((Entry e) => e.Source == System.IO.Path.GetFileName(audio) && e.Text == expected);
			if (entry == null)
			{
				throw new Exception("El texto no se guardó");
			}
			OpenEntry(entry);
			if (transcript.Text != expected)
			{
				throw new Exception("El historial no recuperó el resultado");
			}
			File.WriteAllText(reportPath, $"PASS: confirmación desde modal; {expected.Length} caracteres permanecen al finalizar, después de 1,5 s y al abrir el historial; progreso 100%.");
		}
		catch (Exception ex)
		{
			File.WriteAllText(reportPath, "FAIL: " + ex);
		}
		finally
		{
			controller.Dispose();
			Application.Current.Shutdown();
		}
	}

	public async Task SelfTest(string reportPath)
	{
		StringBuilder report = new StringBuilder();
		try
		{
			if (!controller.HistoryAvailable)
			{
				throw new Exception("Historial no disponible.");
			}
			{
				string text = controller.ReadCipherVersion();
				if (string.IsNullOrEmpty(text))
				{
					throw new Exception("SQLCipher no está activo.");
				}
				report.AppendLine("SQLCipher: " + text);
			}
			string text2 = "Prueba de historial confidencial áéíóú";
			long id = Save("Prueba", text2);
			Refresh();
			if (!entries.Any((Entry e) => e.Id == id && e.Text == text2))
			{
				throw new Exception("Falló el historial.");
			}
			controller.DeleteHistory(false, id);
			Refresh();
			report.AppendLine("Historial cifrado: guardar/leer/eliminar OK");
			byte[] bytes = RandomNumberGenerator.GetBytes(32);
			byte[] encryptedData = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
			if (!Enumerable.SequenceEqual(bytes, ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser)))
			{
				throw new Exception("DPAPI falló");
			}
			report.AppendLine("DPAPI OK");
			string audio = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(reportPath), "prueba-es.wav");
			if (File.Exists(audio))
			{
				status.Text = "Prueba de transcripción local…";
				string result = await controller.TranscribeFile(audio, null, CancellationToken.None);
				report.AppendLine("Motor: " + controller.Backend);
				using (EngineClient cpu = new EngineClient(forceCpu: true))
				{
					if ((await cpu.Run(new EngineRequest(audio, null), null, CancellationToken.None)).Length == 0)
					{
						throw new Exception("CPU devolvió texto vacío");
					}
					report.AppendLine("CPU alternativa: OK");
				}
				report.AppendLine("Transcripción: " + result);
				if (result.Length == 0)
				{
					throw new Exception("Transcripción vacía");
				}
				transcript.Text = result;
			}
			string testFolder = System.IO.Path.GetDirectoryName(reportPath);
			using (MediaFoundationReader waveProvider = new MediaFoundationReader(audio))
			{
				ISampleProvider sampleProvider = waveProvider.ToSampleProvider();
				if (sampleProvider.WaveFormat.Channels == 2)
				{
					sampleProvider = new StereoToMonoSampleProvider(sampleProvider);
				}
				sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 16000);
				using FileStream outputStream = File.Create(System.IO.Path.Combine(testFolder, "prueba-opus.ogg"));
				OpusOggWriteStream opusOggWriteStream = new OpusOggWriteStream(OpusCodecFactory.CreateEncoder(16000, 1), outputStream, new OpusTags(), 16000);
				float[] array = new float[16000];
				int num;
				while ((num = sampleProvider.Read(array, 0, array.Length)) > 0)
				{
					short[] array2 = new short[num];
					for (int num2 = 0; num2 < num; num2++)
					{
						array2[num2] = (short)Math.Clamp(array[num2] * 32767f, -32768f, 32767f);
					}
					opusOggWriteStream.WriteSamples(array2, 0, num);
				}
				opusOggWriteStream.Finish();
			}
			string[] array3 = new string[2] { "prueba-opus.ogg", "prueba-vorbis.ogg" };
			string[] array4 = array3;
			foreach (string text3 in array4)
			{
				string path = System.IO.Path.Combine(testFolder, text3);
				if (!File.Exists(path))
				{
					continue;
				}
				using (AudioFile audioFile = AudioFile.Open(path))
				{
					if (audioFile.Source.Read(new float[4096], 0, 4096) == 0)
					{
						throw new Exception("OGG sin muestras");
					}
				}
				StringBuilder stringBuilder = report;
				string result = text3;
				stringBuilder.AppendLine(result + ": decodificación y transcripción OK (" + (await controller.TranscribeFile(path, null, CancellationToken.None)).Length + " caracteres)");
			}
			SelectAudio(System.IO.Path.Combine(testFolder, "prueba-opus.ogg"));
			if (busy || !confirm.IsEnabled || progressBar.Value != 0.0 || progressLabel.Text != "Esperando confirmación")
			{
				throw new Exception("Selección inició procesamiento sin confirmación");
			}
			report.AppendLine("Selección sin inicio automático: OK");
			await TranscribeSelected(notify: false);
			if (progressBar.Value != 100.0 || progressLabel.Text != "Completado: 100%" || transcript.Text.Length == 0)
			{
				throw new Exception("Flujo confirmado/progreso/finalización falló");
			}
			report.AppendLine("Confirmación y progreso hasta 100% tras guardar: OK");
			ValidateCompactUi(reportPath);
			report.AppendLine("Inicio compacto, carga e historial reabiertos, Abrir recupera texto: OK");
			string path2 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(reportPath), "invalido.wav");
			File.WriteAllText(path2, "Esto no es audio");
			try
			{
				await controller.TranscribeFile(path2, null, CancellationToken.None);
				throw new Exception("Se aceptó audio inválido");
			}
			catch (Exception ex) when (ex.Message != "Se aceptó audio inválido")
			{
				report.AppendLine("Archivo inválido rechazado OK");
			}
			status.Text = "Pruebas locales completadas";
			UpdateLayout();
			RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);
			renderTargetBitmap.Render(this);
			PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
			pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
			using (FileStream stream = File.Create(System.IO.Path.ChangeExtension(reportPath, "png")))
			{
				pngBitmapEncoder.Save(stream);
			}
			report.AppendLine("PASS");
		}
		catch (Exception ex2)
		{
			report.AppendLine("FAIL: " + ex2);
		}
		File.WriteAllText(reportPath, report.ToString());
		controller.Dispose();
		Application.Current.Shutdown();
	}
}
