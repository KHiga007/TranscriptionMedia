namespace TranscriptionMedia;

internal static class RecordingChunks
{
    private const int SampleRate = 16_000;
    private const int MaxChunkSamples = SampleRate * 29;
    private const int FirstCutSample = SampleRate * 20;
    private const int EnergyWindow = 1_600;
    private const int EnergyStep = 800;

    // Keep only one float window instead of duplicating the complete recording.
    public static IEnumerable<float[]> Split(IEnumerable<short[]> packets)
    {
        var buffer = new float[MaxChunkSamples];
        int filled = 0;
        foreach (short[] packet in packets)
        {
            int position = 0;
            while (position < packet.Length)
            {
                int count = Math.Min(buffer.Length - filled, packet.Length - position);
                for (int i = 0; i < count; i++)
                {
                    buffer[filled + i] = packet[position + i] / 32768f;
                }

                position += count;
                filled += count;
                if (filled < buffer.Length)
                {
                    continue;
                }

                int cut = FindQuietCut(buffer, filled);
                yield return buffer[..cut];
                int remaining = filled - cut;
                Array.Copy(buffer, cut, buffer, 0, remaining);
                filled = remaining;
            }
        }

        if (filled > 0)
        {
            yield return buffer[..filled];
        }
    }

    private static int FindQuietCut(float[] buffer, int filled)
    {
        int cut = filled;
        double bestEnergy = double.MaxValue;
        for (int start = FirstCutSample; start + EnergyWindow < filled; start += EnergyStep)
        {
            double energy = 0;
            for (int i = start; i < start + EnergyWindow; i++)
            {
                energy += buffer[i] * buffer[i];
            }

            if (energy < bestEnergy)
            {
                bestEnergy = energy;
                cut = start + EnergyStep;
            }
        }
        return cut;
    }
}
