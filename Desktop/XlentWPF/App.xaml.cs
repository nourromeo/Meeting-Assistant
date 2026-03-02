using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
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

            // --- Build Configration -----------------------------------------

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();


            // --- Register services -----------------------------------------

            var services = new ServiceCollection();
            RegisterServices(services, configuration);
            Services = services.BuildServiceProvider();

            // --- Launch main window -----------------------------------------
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }


        private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.AddSingleton<IConfiguration>(configuration);

            // --- Services ------------------------------------------------

            // TO SWITCH TO REAL APIs: swap these three lines:
            //   services.AddSingleton<ITranscriptionService, AzureTranscriptionService>();
            //   services.AddSingleton<ISummaryService,       OpenAiSummaryService>();
            //   services.AddSingleton<IAudioService,         NAudioMicrophoneService>();
            services.AddSingleton<ITranscriptionService, MockTranscriptionService>();
            services.AddSingleton<ISummaryService, MockSummaryService>();
            services.AddSingleton<IAudioService, MockAudioService>();

            // -- ViewModels ------------------------------------------------
            services.AddSingleton<RecordingViewModel>();
            services.AddSingleton<TranscriptViewModel>();
            services.AddSingleton<SummaryViewModel>();
            services.AddSingleton<MainViewModel>();

            // -- Views ----------------------------------------------------
            services.AddSingleton<MainWindow>();


        }

    }
}
