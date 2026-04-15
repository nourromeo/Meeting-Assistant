using System;
using System.Collections.Generic;
using System.Text;

namespace XlentWPF.Services.Interfaces
{
    public interface IAudioService : IDisposable
    {
        event Action<byte[]> AudioDataAvailable;
        bool IsCapturing { get; }
        event Action<float> AudioLevelChanged;

        Task StartAsync();
        void Pause();
        void Resume();
        void Stop();

    }
}
