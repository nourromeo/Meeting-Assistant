using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using XlentApi.Services;

namespace XlentWPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string outputText;

    [RelayCommand]
    private async Task CallApi()
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync("https://localhost:7200/api/Test");

        var json = JsonDocument.Parse(response);
        OutputText = json.RootElement.GetProperty("message").GetString();
    }

    private object _currentView;
    private object _currentInputViewModel;
    private string _transcriptionResult;


    public object CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public object CurrentInputViewModel
    {
        get => _currentInputViewModel;
        set => SetProperty(ref _currentInputViewModel, value);
    }

    public string TranscriptionResult
    {
        get => _transcriptionResult;
        set => SetProperty(ref _transcriptionResult, value);

    }

    public RecordViewModel RecordVM { get; }

    public UploadViewModel UploadVM { get; }

    public ICommand ShowRecordCommand { get; }
    public ICommand ShowUploadCommand { get; }

    public MainViewModel(ISpeechService speechService)
    {
        RecordVM = new RecordViewModel(speechService, OnTranscriptionCompleted);
        UploadVM = new UploadViewModel(speechService, OnTranscriptionCompleted);

        ShowRecordCommand = new RelayCommand(() =>
            CurrentInputViewModel = RecordVM);

        ShowUploadCommand = new RelayCommand(() =>
            CurrentInputViewModel = UploadVM);

        CurrentInputViewModel = RecordVM;
    }

    private void OnTranscriptionCompleted(string result)
    {
        TranscriptionResult = result;
        OnPropertyChanged(nameof(TranscriptionResult));
    }

}



