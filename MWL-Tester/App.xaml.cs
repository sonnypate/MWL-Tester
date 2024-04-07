using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers;
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
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MWL-Tester\Logs\Trace.log");

            var serilogLogger = ConfigureLogging();

            new DicomSetupBuilder()
                .RegisterServices(services => services.AddLogging(logging => logging.AddSerilog(serilogLogger)))
                .Build();

            Log.Information("Hello, world!");
            base.OnStartup(e);
        }

        public ILogger ConfigureLogging()
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MWL-Tester\Logs\Trace.log");

            var loggerConfig = new LoggerConfiguration()
                //Enrich each log message with the machine name
                .Enrich.With<MachineNameEnricher>()
                //Accept verbose output  (there is effectively no filter)
                .MinimumLevel.Verbose()
                //Also write out to a file based on the date and restrict these writes to warnings or worse (warning, error, fatal)
                .WriteTo.File(logPath, 
                    global::Serilog.Events.LogEventLevel.Verbose, 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} | [{Level:u3}] | {Message:lj}{NewLine}{Exception}");

            var logger = loggerConfig
                //Take all of that configuration and make a logger
                .CreateLogger();

            //Stash the logger in the global Log instance for convenience
            global::Serilog.Log.Logger = logger;

            return logger;
        }
    }
}
