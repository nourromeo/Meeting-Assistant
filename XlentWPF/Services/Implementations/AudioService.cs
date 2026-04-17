using XlentWPF.Services.Interfaces;
using NAudio.Wave;
using System;

namespace XlentWPF.Services.Implementations
{
    public class AudioService : IAudioService
    {
        private WaveInEvent? _waveIn;
        private bool _isCapturing;
        private bool _disposed;

        private static readonly WaveFormat CaptureFormat = new(16_000, 16, 1);

        public bool IsCapturing => _isCapturing;

        public event Action<byte[]>? AudioDataAvailable;
        public event Action<float>? AudioLevelChanged;

        public Task StartAsync()
        {
            if (_isCapturing) return Task.CompletedTask;

            _waveIn = new WaveInEvent
            {
                WaveFormat = CaptureFormat,
                BufferMilliseconds = 250   // 250 ms chunks ≈ 8 000 bytes — more acoustic context per push
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
            _isCapturing = true;

            return Task.CompletedTask;
        }

        public void Pause()
        {
            if (!_isCapturing || _waveIn is null) return;
            _waveIn.StopRecording();
            _isCapturing = false;
        }

        public void Resume()
        {
            if (_isCapturing || _waveIn is null) return;
            _waveIn.StartRecording();
            _isCapturing = true;
        }

        public void Stop()
        {
            if (_waveIn is null) return;
            _waveIn.StopRecording();
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.Dispose();
            _waveIn = null;
            _isCapturing = false;
        }

        // ------------------------------------------------------------------ //

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0) return;

            var buffer = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, buffer, e.BytesRecorded);
            AudioDataAvailable?.Invoke(buffer);
            AudioLevelChanged?.Invoke(CalculateRmsLevel(e.Buffer, e.BytesRecorded));
        }

        private static float CalculateRmsLevel(byte[] buffer, int bytesRecorded)
        {
            int sampleCount = bytesRecorded / 2;   // 16-bit = 2 bytes per sample
            if (sampleCount == 0) return 0f;

            long sumSquares = 0;
            for (int i = 0; i < bytesRecorded - 1; i += 2)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                sumSquares += (long)sample * sample;
            }

            double rms = Math.Sqrt((double)sumSquares / sampleCount);
            return (float)Math.Min(rms / 32_768.0, 1.0);   // max Int16 = 32 768
        }

        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}