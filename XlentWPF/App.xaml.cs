using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XlentWPF.Services.Implementations;
using XlentWPF.Services.Interfaces;
using XlentWPF.ViewModels;
using XlentWPF.Views;
using System.Windows;

namespace XlentWPF
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            ServiceProvider = services.BuildServiceProvider();

            ServiceProvider.GetRequiredService<MainWindow>().Show();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);

            // HttpClient for the backend API — carries the shared API key on every request
            services.AddHttpClient<ISummaryService, SummaryService>(client =>
            {
                client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!);
                client.DefaultRequestHeaders.Add("X-Api-Key", configuration["ApiSettings:ApiKey"]);
            });

            services.AddSingleton<IAudioService, AudioService>();
            services.AddSingleton<ITranscriptionService, TranscriptionService>();

            services.AddTransient<RecordingViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<TranscriptViewModel>();
            services.AddTransient<SummaryViewModel>();

            services.AddSingleton<MainWindow>();
        }
    }
}