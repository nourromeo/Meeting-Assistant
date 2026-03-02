using System;
using System.Collections.Generic;
using System.Text;

namespace XlentWPF.Services.Interfaces
{
    public interface IAudioService : IDisposable
    {
        //True when the microphone is actively capturing.
        bool IsCapturing { get; }

        /// <summary>
        /// Fired continuously while capturing.
        /// Value is 0.0–1.0 normalised audio level for waveform display.
        /// </summary>
        event Action<float> AudioLevelChanged;

        //Start capturing from the default microphone.
        Task StartAsync();

        //Pause capturing (keeps device open).
        void Pause();

        //Resume after pause
        void Resume();

        //Stop capturing and release the device.
        void Stop();
    }
}
