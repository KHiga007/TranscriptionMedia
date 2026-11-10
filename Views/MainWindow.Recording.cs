using System.Windows.Automation;
using System.Media;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TranscriptionMedia;

public partial class MainWindow
{
	private sealed record MicOption(int Device, string Name)
	{
		public override string ToString()
		{
			return Name;
		}
	}

	private void UpdateRecordingButtons()
	{
		record.Visibility = (recording ? Visibility.Collapsed : Visibility.Visible);
		pause.Visibility = ((!recording) ? Visibility.Collapsed : Visibility.Visible);
		stop.Visibility = ((!recording) ? Visibility.Collapsed : Visibility.Visible);
		pause.Content = (paused ? "\ue768" : "\ue769");
		pause.Tag = (paused ? "Reanudar" : "Pausar");
		AutomationProperties.SetName(pause, (string)pause.Tag);
		micPicker.IsEnabled = !recording;
	}

	private void RefreshMicrophones()
	{
		string previous = (micPicker.SelectedItem as MicOption)?.Name;
		List<MicOption> list = new List<MicOption>
		{
			new MicOption(-1, "Predeterminado de Windows")
		};
		try
		{
			for (int i = 0; i < WaveIn.DeviceCount; i++)
			{
				list.Add(new MicOption(i, WaveIn.GetCapabilities(i).ProductName));
			}
		}
		catch
		{
		}
		micPicker.ItemsSource = list;
		micPicker.SelectedItem = list.FirstOrDefault((MicOption o) => o.Name == previous) ?? list[0];
	}

	private void UpdateMeter()
	{
		bool flag = recording && !paused;
		if (flag && !recordWatch.IsRunning)
		{
			recordWatch.Start();
		}
		else if (!flag && recordWatch.IsRunning)
		{
			recordWatch.Stop();
		}
		TimeSpan elapsed = recordWatch.Elapsed;
		timerText.Text = ((elapsed.TotalHours >= 1.0) ? elapsed.ToString("h\\:mm\\:ss") : elapsed.ToString("mm\\:ss"));
		Array.Copy(meterLevels, 1, meterLevels, 0, meterLevels.Length - 1);
		meterLevels[^1] = (flag ? Math.Min(1.0, Math.Sqrt(micLevel) * 1.4) : 0.0);
		micLevel = 0f;
		for (int i = 0; i < meterBars.Length; i++)
		{
			meterBars[i].Height = 4.0 + meterLevels[i] * 26.0;
			meterBars[i].Fill = (flag ? Palette.Rec : Palette.BorderStrong);
		}
	}

	private async Task<string> Transcribe(float[] samples, CancellationToken token)
	{
		string result = await controller.TranscribeSamples(samples, token);
		backend = controller.Backend;
		return result;
	}

	private void Record(object sender, RoutedEventArgs e)
	{
		try
		{
			if (WaveIn.DeviceCount == 0)
			{
				throw new InvalidOperationException("No se encontró un micrófono.");
			}
			cancellation = new CancellationTokenSource();
			paused = false;
			transcript.Clear();displayedHistoryId=null;
			lock (audioLock)
			{
				recorded.Clear();
				recordedSamples = 0L;
			}
			mic = new WaveInEvent
			{
				DeviceNumber = ((micPicker.SelectedItem as MicOption)?.Device ?? (-1)),
				WaveFormat = new WaveFormat(16000, 16, 1),
				BufferMilliseconds = 100
			};
			mic.DataAvailable += (object? _, WaveInEventArgs args) =>
			{
				if (!paused)
				{
					short[] array = new short[args.BytesRecorded / 2];
					Buffer.BlockCopy(args.Buffer, 0, array, 0, array.Length * 2);
					float num = 0f;
					short[] array2 = array;
					foreach (short num2 in array2)
					{
						num = Math.Max(num, Math.Abs((float)num2 / 32768f));
					}
					micLevel = Math.Max(micLevel, num);
					lock (audioLock)
					{
						if (recordedSamples < 115200000)
						{
							recorded.Add(array);
							recordedSamples += array.Length;
							return;
						}
					}
					Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!paused)
						{
							paused = true;
							UpdateRecordingButtons();
							status.Text = "Se alcanzó el límite de 2 horas. Pulsá Detener para transcribir.";
						}
					}));
				}
			};
			mic.RecordingStopped += (object? _, StoppedEventArgs args) =>
			{
				if (args.Exception != null)
				{
					Dispatcher.BeginInvoke((Action)(() =>
					{
						paused=true;Stop(this,new RoutedEventArgs());status.Text = "El micrófono se detuvo: " + args.Exception.Message;
					}));
				}
			};
			mic.StartRecording();
			SetBusy(value: true);
			pause.IsEnabled = true;
			stop.IsEnabled = true;
			recording = true;
			recordWatch.Reset();
			Array.Clear(meterLevels);
			meterTimer.Start();
			UpdateRecordingButtons();
			status.Text = "Grabando · La transcripción empieza al pulsar Detener.";
		}
		catch (Exception ex)
		{
			mic?.Dispose();
			mic = null;
			cancellation?.Dispose();
			cancellation = null;
			SetBusy(value: false);
			status.Text = ex.Message;
		}
	}

	private async void Stop(object sender, RoutedEventArgs e)
	{
        if(!recording)return;
		stop.IsEnabled = false;
		pause.IsEnabled = false;
		recording = false;
		meterTimer.Stop();
		UpdateMeter();
		try{mic?.StopRecording();}catch(Exception ex){status.Text="Error al detener el micrófono: "+ex.Message;}
        try{mic?.Dispose();}catch{}
		mic = null;
		paused = false;
		UpdateRecordingButtons();
        List<short[]> packets;
        lock(audioLock){packets=recorded.ToList();recorded.Clear();recordedSamples=0;}
        long totalSamples=packets.Sum(p=>(long)p.Length);
		CancellationToken token = cancellation?.Token ?? CancellationToken.None;
		statusProgress.Value = 0.0;
		statusProgress.Visibility = Visibility.Visible;
		cancelRecording.Visibility = Visibility.Visible;
		status.Text = "Transcribiendo la grabación…";
		transcribeWatch.Restart();
		try
		{
			long processed=0;
			StringBuilder text = new StringBuilder();
			foreach(var chunk in RecordingChunks.Split(packets))
			{
				string text2 = await Transcribe(chunk, token);
				if (text2.Length > 0)
				{
					text.Append((text.Length > 0) ? "\n" : "").Append(text2);
					transcript.Text = text.ToString();
					transcript.ScrollToEnd();
				}
				processed+=chunk.Length;double value=totalSamples>0?(double)processed/totalSamples*100:100;
				statusProgress.Value = value;
				status.Text = $"Transcribiendo la grabación · {value:0}%";
			}
			if (text.Length > 0)
			{
				displayedHistoryId=Save("Micrófono", text.ToString());
				transcript.ScrollToHome();
				status.Text = TranscribedIn();
				SystemSounds.Asterisk.Play();
			}
			else
			{
				status.Text = "Grabación finalizada · No se detectó voz transcribible.";
			}
		}
		catch (OperationCanceledException)
		{
			status.Text = "Cancelado. La grabación no se guardó.";
		}
		catch (Exception ex2)
		{
			status.Text = "No se pudo transcribir: " + ex2.Message;
		}
		finally
		{
			packets.Clear();
            statusProgress.Visibility = Visibility.Collapsed;
			cancelRecording.Visibility = Visibility.Collapsed;
			cancellation?.Dispose();
			cancellation = null;
			SetBusy(value: false);
		}
	}
}
