using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using XlentApi.Services;

namespace XlentWPF.ViewModels
{
    public class UploadViewModel : ObservableObject
    {
        private readonly ISpeechService _speechService;
        private readonly Action<string> _onCompleted;
        private string _selectedFilePath;
        private bool _isUploading;



        public string SelectedFilePath => System.IO.Path.GetFileName(_selectedFilePath);

        public bool IsFileSelected => !string.IsNullOrEmpty(_selectedFilePath);

        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public ICommand BrowseFilesCommand { get; }
        public ICommand UploadFileCommand { get; }


        public UploadViewModel(ISpeechService speechService,
                             Action<string> onCompleted)
        {
            _speechService = speechService;
            _onCompleted = onCompleted;

            BrowseFilesCommand = new RelayCommand(ExecuteBrowse);
            UploadFileCommand = new RelayCommand(async () =>
                await UploadFile());
        }

        private void ExecuteBrowse()
        {
            throw new NotImplementedException();
        }

        private async Task UploadFile()
        {
            throw new NotImplementedException();
        }
    }
}
