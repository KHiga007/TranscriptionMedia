using System.IO;

namespace TranscriptionMedia;

internal static class AppPaths
{
	public static string VoiceModel
	{
		get
		{
			const string fileName = "ggml-large-v3-q5_0.bin";
			string currentPath = Path.Combine(Root, "Voice Model", fileName);
			string previousPath = Path.Combine(Root, "models", fileName);
			return File.Exists(currentPath) || !File.Exists(previousPath) ? currentPath : previousPath;
		}
	}

	public static string Root
	{
		get
		{
			if (!string.Equals(new DirectoryInfo(AppContext.BaseDirectory).Name, "componentes", StringComparison.OrdinalIgnoreCase))
			{
				return AppContext.BaseDirectory;
			}
			return Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName;
		}
	}
}
