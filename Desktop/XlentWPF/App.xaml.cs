using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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

            // ViewModels
            services.AddSingleton<RecordingViewModel>();
            services.AddSingleton<TranscriptViewModel>();
            services.AddSingleton<SummaryViewModel>(); // must have ctor (IConfiguration config)
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }
    }
}