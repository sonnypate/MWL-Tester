using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Targets;
using System.Configuration;
using System.Data;
using System.Windows;

namespace MWL_Tester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialize log manager.
            new DicomSetupBuilder()
                .RegisterServices(services => services.AddLogging(logging => logging.AddProvider(new NLog.))
               ).Build();

                base.OnStartup(e);
        }

        
    }

}
