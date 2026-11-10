namespace TranscriptionMedia;

public record Entry(long Id, string Date, string Source, string Text)
{
	public string Label => Date + "\n" + Source;

	public string Preview => Text.ReplaceLineEndings(" ");
}
