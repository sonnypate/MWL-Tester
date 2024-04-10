using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers;
using Serilog.Events;
using System.IO;
using System.Windows;
using ILogger = Serilog.ILogger;

namespace MWL_Tester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var serilogLogger = ConfigureLogging();

            new DicomSetupBuilder() // Requires Serilog.Extensions.Logging for the AddSerilog function.
                .RegisterServices(services => services.AddLogging(logging => logging.AddSerilog(serilogLogger)))
                .Build();

            base.OnStartup(e);
        }

        public ILogger ConfigureLogging()
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MWL-Tester\Logs\Trace.log");

            var loggerConfig = new LoggerConfiguration()
                .Enrich.With<MachineNameEnricher>()
                .MinimumLevel.Verbose()
                .WriteTo.File(logPath, 
                    global::Serilog.Events.LogEventLevel.Verbose, 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

            var logger = loggerConfig.CreateLogger();

            //Stash the logger in the global Log instance for convenience
            global::Serilog.Log.Logger = logger;

            return logger;
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            // Clean up logging before shutting down.
            Log.CloseAndFlush();

            // Save any user settings automatically.
            MWL_Tester.Properties.Settings.Default.Save();
        }

    }
}
