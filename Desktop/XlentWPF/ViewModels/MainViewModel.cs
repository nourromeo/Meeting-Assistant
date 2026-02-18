using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
}
