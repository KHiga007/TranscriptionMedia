using System.IO;

namespace TranscriptionMedia;

internal sealed class TranscriptionController : IDisposable
{
	private readonly HistoryStore history = new();
	private readonly EngineClient engine = new();

	public bool HistoryAvailable => history.IsOpen;
	public string Backend => engine.Backend;
	public void OpenHistory() => history.Open();
	public List<Entry> ReadHistory() => history.Read();
	public long SaveHistory(string source, string text, long? id = null) => history.Save(source, text, id);
	public void DeleteHistory(bool all, long? id) => history.Delete(all, id);
	public string? ReadCipherVersion() => history.ReadCipherVersion();

	public List<Entry> SearchHistory(IEnumerable<Entry> entries, string query)
	{
		return entries.Where(entry => (entry.Text + entry.Source + entry.Date)
			.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
	}

	public void ValidateAudio(string path)
	{
		if (!File.Exists(path))
		{
			throw new IOException("El archivo seleccionado ya no existe.");
		}
		if (new FileInfo(path).Length > 2147483648L)
		{
			throw new IOException("El límite por archivo es 2 GB.");
		}
	}

	public Task<string> TranscribeFile(string path, Action<string>? text, CancellationToken token, Action<double>? progress = null)
	{
		return engine.Run(new EngineRequest(path, null), text, token, progress);
	}

	public Task<string> TranscribeSamples(float[] samples, CancellationToken token)
	{
		if (samples.Length < 1600 || samples.All(sample => Math.Abs(sample) < 0.000001f))
		{
			return Task.FromResult("");
		}

		byte[] buffer = new byte[samples.Length * sizeof(float)];
		Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);
		return engine.Run(new EngineRequest(null, Convert.ToBase64String(buffer)), null, token);
	}

	public void Dispose()
	{
		engine.Dispose();
		history.Dispose();
	}
}