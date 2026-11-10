using System.IO;
using System.Media;
using System.Text;
using System.Windows;

namespace TranscriptionMedia;

public partial class MainWindow
{
	private void UpdateUploadLabels()
	{
		selectedLabel.Text = ((selectedAudio == null) ? "Elegir archivo de audio" : System.IO.Path.GetFileName(selectedAudio));
		dropHint.Text = ((selectedAudio == null) ? "Buscar en tu computadora" : "Hacé clic para cambiar el archivo");
	}

	private void SelectAudio(string path)
	{
		selectedAudio = System.IO.Path.GetFullPath(path);
		selectedLabel.Text = "Audio seleccionado: " + System.IO.Path.GetFileName(path);
		selectedLabel.ToolTip = selectedAudio;
		confirm.IsEnabled = !busy;
		progressBar.IsIndeterminate = false;
		progressBar.Value = 0.0;
		progressLabel.Text = "Esperando confirmación";
		status.Text = "Revisá el archivo y pulsá «Usar este audio y transcribir».";
	}

	private void OpenFile(object sender, RoutedEventArgs e)
	{
		ShowUpload();
	}

	private async void ConfirmFile(object sender, RoutedEventArgs e)
	{
		if (selectedAudio != null && !busy)
		{
			ShowWorkspace(microphone: false, System.IO.Path.GetFileName(selectedAudio));
			activeFileTask = TranscribeSelected(!suppressCompletionAlert);
			await activeFileTask;
		}
	}

	private async Task TranscribeSelected(bool notify)
	{
		if (busy || selectedAudio == null)
		{
			return;
		}
		string path = selectedAudio;
		try
		{
			controller.ValidateAudio(path);
		}
		catch (Exception ex)
		{
			status.Text = ex.Message;
			return;
		}
		SetBusy(value: true);
		transcribeWatch.Restart();
		cancellation = new CancellationTokenSource();
		transcript.Clear();displayedHistoryId=null;
		progressBar.Value = 0.0;
		progressBar.IsIndeterminate = true;
		progressLabel.Text = "Preparando el motor…";
		status.Text = "Transcribiendo " + System.IO.Path.GetFileName(path);
		try
		{

			StringBuilder attemptText = new StringBuilder();
			bool retryPending = false;
			string text = await controller.TranscribeFile(path, (string part) =>
			{
				if (part == "\0RESET")
				{
					attemptText.Clear();
					retryPending = true;
					progressBar.Value = 0.0;
					progressLabel.Text = "Reintentando con otro dispositivo · Texto anterior conservado";
				}
				else
				{
					if (retryPending)
					{
						transcript.Clear();
						retryPending = false;
					}
					attemptText.AppendLine(part);
					transcript.AppendText(part + "\n");
					transcript.ScrollToEnd();
					status.Text = "Transcribiendo · " + controller.Backend;
				}
			}, cancellation.Token, (double value) =>
			{
				progressBar.IsIndeterminate = value < 0.0;
				if (value >= 0.0)
				{
					progressBar.Value = Math.Min(99.0, value);
					progressLabel.Text = $"Transcribiendo: {Math.Min(99.0, value):0}%";
				}
				else
				{
					progressLabel.Text = "Transcribiendo audio…";
				}
			});
						if (string.IsNullOrWhiteSpace(text) && attemptText.Length > 0)
			{
				text = attemptText.ToString().Trim();
			}
			if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(transcript.Text))
			{
				throw new IOException("El motor no completó el reintento. Se conserva el texto parcial en pantalla.");
			}
			if (text.Length > 0)
			{
				displayedHistoryId=Save(System.IO.Path.GetFileName(path), text);
				ShowWorkspace(microphone: false, System.IO.Path.GetFileName(path));
				transcript.Text = text;
				transcript.ScrollToHome();
			}
			backend = controller.Backend;
			progressBar.IsIndeterminate = false;
			progressBar.Value = 100.0;
			progressLabel.Text = "Completado: 100%";
			status.Text = ((text.Length > 0) ? TranscribedIn() : "Terminado · No se detectó voz transcribible.");
			if (notify)
			{
				SystemSounds.Asterisk.Play();
				MessageBox.Show(this, (text.Length > 0) ? ("Terminó la transcripción de «" + System.IO.Path.GetFileName(path) + "».\nEl texto se guardó en el historial.") : ("Terminó el procesamiento de «" + System.IO.Path.GetFileName(path) + "».\nNo se detectó voz transcribible."), "Transcripción finalizada", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
		}
		catch (OperationCanceledException)
		{
			progressLabel.Text = "Cancelado";
			status.Text = "Cancelado. El texto parcial no se guardó.";
		}
		catch (Exception ex3)
		{
			progressLabel.Text = "No se pudo completar";
			status.Text = "No se pudo transcribir: " + ex3.Message;
		}
		finally
		{
			progressBar.IsIndeterminate = false;
			cancellation.Dispose();
			cancellation = null;
			SetBusy(value: false);
			newAudio.Visibility = Visibility.Visible;
		}
	}
}
