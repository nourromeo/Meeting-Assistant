using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using XlentWPF.Services.Mock;
using XlentWPF.Services.Interfaces;   
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

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            RegisterServices(services, configuration);
            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);

            // Services (بدون أي Summary Service)
            services.AddSingleton<ITranscriptionService, MockTranscriptionService>();
            services.AddSingleton<IAudioService, MockAudioService>();

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