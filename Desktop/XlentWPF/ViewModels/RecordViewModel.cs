using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using XlentApi.Services;

namespace XlentWPF.ViewModels
{
    public class RecordViewModel : ObservableObject
    {
        private readonly ISpeechService _speechService;
        private readonly Action<string> _onTranscriptionCompleted;
        private bool _isRecording;


        public bool IsRecording
        {
            get => _isRecording;
            set => SetProperty(ref _isRecording, value);
        }

        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }

        public RecordViewModel(ISpeechService speechService, Action<string> onTranscriptionCompleted)
        {
            _speechService = speechService;
            _onTranscriptionCompleted = onTranscriptionCompleted;
        }
        public async Task StartRecordingAsync()
        {
            _isRecording = true;
            var result = await _speechService.TranscribeAsync();
            _onTranscriptionCompleted(result);
        }


        public async Task StopRecordingAsync()
        {
            _isRecording = false;

            // Stop Mic.
        }

        public void ToggleRecording()
        {
            if (IsRecording)
            {
                StopRecordingAsync();
            }
            else
            {
                StartRecordingAsync();
            }
        }
    }
}
