using XlentWPF.Services.Interfaces;

namespace XlentWPF.Services.Mock;


// Replace with NAudioMicrophoneService when ready.

public class MockAudioService : IAudioService
{
    private readonly System.Timers.Timer _timer = new(80);
    private readonly Random _rng = new();

    public bool IsCapturing { get; private set; }

    public event Action<float>? AudioLevelChanged;
    public event Action<byte[]> AudioDataAvailable;

    public MockAudioService()
    {
        _timer.Elapsed += (_, _) =>
        {
            if (IsCapturing)
                AudioLevelChanged?.Invoke((float)(_rng.NextDouble() * 0.9 + 0.05));
        };
    }

    public Task StartAsync()
    {
        IsCapturing = true;
        _timer.Start();
        return Task.CompletedTask;
    }

    public void Pause()
    {
        IsCapturing = false;
        _timer.Stop();
        AudioLevelChanged?.Invoke(0f);
    }

    public void Resume()
    {
        IsCapturing = true;
        _timer.Start();
    }

    public void Stop()
    {
        IsCapturing = false;
        _timer.Stop();
        AudioLevelChanged?.Invoke(0f);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
