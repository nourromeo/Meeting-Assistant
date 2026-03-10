using XlentWPF.Services.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace XlentWPF.Services.Implementations
{
   public class AudioService : IAudioService
    {
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private readonly string _tempPath = "recording.wav";

        public bool IsCapturing { get; private set; }
        public event Action<float>? AudioLevelChanged;


        public Task StartAsync()
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1) // 16kHz Mono: Ideal for AI STT
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _writer = new WaveFileWriter(_tempPath, _waveIn.WaveFormat);

            _waveIn.StartRecording();
            IsCapturing = true;
            return Task.CompletedTask;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);

            float max = 0;
            var buffer = new WaveBuffer(e.Buffer);
            int sampleCount = e.BytesRecorded / 2; // 16-bit = 2 bytes per sample
            for (int i = 0; i < sampleCount; i++)
            {
                var sample = Math.Abs(buffer.ShortBuffer[i] / 32768f); // normalise to 0.0–1.0
                if (sample > max) max = sample;
            }

            AudioLevelChanged?.Invoke(max);
        }


        public void Pause() => _waveIn?.StopRecording();

        public void Resume() => _waveIn?.StartRecording();

        public void Stop()
        {
            IsCapturing = false;
            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _writer?.Dispose();
        }

        public void Dispose() => Stop();
    }
}
