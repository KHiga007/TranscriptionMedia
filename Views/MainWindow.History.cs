using System.Windows;
using System.Windows.Controls;

namespace TranscriptionMedia;

public partial class MainWindow
{
	private void UpdateHistoryState()
	{
		historyCount.Text = ((entries.Count == 1) ? "1 transcripción · cifrado local" : $"{entries.Count} transcripciones · cifrado local");
		historyEmpty.Text = ((entries.Count == 0) ? "Todavía no hay transcripciones guardadas." : "No hay transcripciones que coincidan con la búsqueda.");
		historyEmpty.Visibility = ((history.Items.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		searchPlaceholder.Visibility = ((search.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
		clearHistory.IsEnabled = entries.Count > 0 && !busy;
	}

	private void ShowHistory()
	{
		if (!busy && controller.HistoryAvailable)
		{
			Refresh();
			ShowSection(historyView, historyNav);
		}
	}

	private void DeleteHistoryRow(object sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: Entry tag })
		{
			Delete(all: false, tag);
			e.Handled = true;
		}
	}

	private void OpenHistoryRow(object sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: Entry tag })
		{
			OpenEntry(tag);
			e.Handled = true;
		}
	}

	private void OpenEntry(Entry entry)
	{
		if (!busy)
		{
			ShowWorkspace(microphone: false, entry.Source, showProgress: false);
			transcript.Text = entry.Text;
            displayedHistoryId=entry.Id;
			progressBar.Value = 0.0;
			progressBar.IsIndeterminate = false;
			progressLabel.Text = "Transcripción abierta desde el historial";
			status.Text = "Abierta desde el historial · " + entry.Date;
		}
	}

	private void Refresh()
	{
		if (controller.HistoryAvailable)
		{
			entries = controller.ReadHistory();
			Filter();
		}
	}

	private void Filter()
	{
		history.ItemsSource = controller.SearchHistory(entries, search.Text);
		UpdateHistoryState();
	}

	private long Save(string source, string text, long? id = null)
	{
		long result = controller.SaveHistory(source, text, id);
		Refresh();
		return result;
	}

	private void Delete(bool all, Entry? target = null)
	{
		if ((object)target == null)
		{
			target = history.SelectedItem as Entry;
		}
		if (busy || !controller.HistoryAvailable || (!all && target == null) || MessageBox.Show(this, all ? "¿Borrar todo el historial? No se puede deshacer." : "¿Eliminar esta transcripción?", "Historial", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
		{
			return;
		}
		try
		{
			controller.DeleteHistory(all, target?.Id);
            if(all||displayedHistoryId==target?.Id){transcript.Clear();displayedHistoryId=null;}
			Refresh();
		}
		catch (Exception ex)
		{
			status.Text = ex.Message;
		}
	}
}
