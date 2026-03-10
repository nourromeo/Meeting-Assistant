using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Windows;
using XlentWPF.Services;
using XlentWPF.Services.Interfaces;
using XlentWPF.Services.Mock;
using XlentWPF.ViewModels;
using XlentWPF.Views;

namespace XlentWPF
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Build configuration (reads appsettings.json from output folder)
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Build DI container
            var services = new ServiceCollection();
            RegisterServices(services, configuration);

            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.AddSingleton(configuration);

            // Services (Mock)
            services.AddSingleton<ITranscriptionService, MockTranscriptionService>();
            services.AddSingleton<IAudioService, MockAudioService>();

            // Services
            services.AddSingleton<ISummaryApiService>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var baseUrl = config["ApiSettings:BaseUrl"]
                              ?? throw new InvalidOperationException("Api BaseUrl not configured.");

                var http = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl)
                };

                return new SummaryApiService(http);
            });

            // ViewModels
            services.AddSingleton<RecordingViewModel>();
            services.AddSingleton<TranscriptViewModel>();
            services.AddSingleton<SummaryViewModel>();
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }
    }
}