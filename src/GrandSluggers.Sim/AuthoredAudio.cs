namespace GrandSluggers.Sim;

/// <summary>
/// Original wav drops in data/art/audio-clips/{id}.wav. Missing file keeps generated tones.
/// Never Nintendo samples. Never licensed music.
/// </summary>
public static class AuthoredAudio
{
    public static string Dir(string dataRoot) => Path.Combine(dataRoot, "art", "audio-clips");

    public static HashSet<string> Ids(string dataRoot)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dir = Dir(dataRoot);
        if (!Directory.Exists(dir)) return set;
        foreach (var file in Directory.GetFiles(dir, "*.wav"))
            set.Add(Path.GetFileNameWithoutExtension(file));
        return set;
    }

    public static bool TryLoad(string dataRoot, string id, out float[] samples, out int sampleRate)
    {
        samples = Array.Empty<float>();
        sampleRate = 0;
        if (string.IsNullOrWhiteSpace(id)) return false;
        return WavPcm.TryRead(Path.Combine(Dir(dataRoot), id + ".wav"), out samples, out sampleRate);
    }
}

static class WavPcm
{
    public static bool TryRead(string path, out float[] samples, out int sampleRate)
    {
        samples = Array.Empty<float>();
        sampleRate = 0;
        if (!File.Exists(path)) return false;
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 44) return false;
        if (Ascii(bytes, 0, 4) != "RIFF" || Ascii(bytes, 8, 4) != "WAVE") return false;

        var channels = 1;
        var bits = 16;
        sampleRate = 44100;
        var dataOff = -1;
        var dataLen = 0;
        var i = 12;
        while (i + 8 <= bytes.Length)
        {
            var id = Ascii(bytes, i, 4);
            var size = BitConverter.ToInt32(bytes, i + 4);
            if (size < 0) return false;
            var body = i + 8;
            if (id == "fmt " && size >= 16)
            {
                var format = BitConverter.ToInt16(bytes, body);
                channels = BitConverter.ToInt16(bytes, body + 2);
                sampleRate = BitConverter.ToInt32(bytes, body + 4);
                bits = BitConverter.ToInt16(bytes, body + 14);
                if (format != 1 || channels < 1 || sampleRate < 8000 || (bits != 8 && bits != 16))
                    return false;
            }
            else if (id == "data")
            {
                dataOff = body;
                dataLen = size;
                break;
            }
            i = body + size + (size & 1);
        }
        if (dataOff < 0 || dataOff + dataLen > bytes.Length || sampleRate <= 0) return false;

        var step = (bits / 8) * channels;
        if (step <= 0) return false;
        var frames = dataLen / step;
        if (frames < 8) return false;
        samples = new float[frames];
        for (var f = 0; f < frames; f++)
        {
            var at = dataOff + f * step;
            float mix = 0;
            for (var c = 0; c < channels; c++)
            {
                if (bits == 16)
                    mix += BitConverter.ToInt16(bytes, at + c * 2) / 32768f;
                else
                    mix += (bytes[at + c] - 128) / 128f;
            }
            samples[f] = mix / channels;
        }
        return true;
    }

    static string Ascii(byte[] b, int at, int n) =>
        System.Text.Encoding.ASCII.GetString(b, at, n);
}
