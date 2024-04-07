using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MWL_Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ILogger _logger;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public MainWindow()
        {
            _logger = Log.ForContext<MainWindow>();
            InitializeComponent();
        }

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add cancellation
            await TestDicomConnection();
        }

        private async Task TestDicomConnection()
        {
            var client = DicomClientFactory.Create(ServerIpAddress.Text, int.Parse(ServerPort.Text), false, CallingAET.Text, ServerAET.Text);
            client.AssociationAccepted += Client_AssociationAccepted;
            client.AssociationRequestTimedOut += Client_AssociationRequestTimedOut;

            try
            {
                client.NegotiateAsyncOps();
                for (int i = 0; i < 10; i++)
                    await client.AddRequestAsync(new DicomCEchoRequest());
                await client.SendAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                _logger.Error("Error: {exception}", ex);
            }
        }

        private void Client_AssociationRequestTimedOut(object? sender, FellowOakDicom.Network.Client.EventArguments.AssociationRequestTimedOutEventArgs e)
        {
            if (StatusText.Dispatcher.CheckAccess())
            {
                StatusText.Content = $"Connection timed out";
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    StatusText.Content = $"Connection timed out";
                });
            }
        }

        private void Client_AssociationAccepted(object? sender, FellowOakDicom.Network.Client.EventArguments.AssociationAcceptedEventArgs e)
        {            
            if (StatusText.Dispatcher.CheckAccess())
            {
                StatusText.Content = "Association accepted";
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    StatusText.Content = "Association accepted";
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.CloseAndFlush();
        }
    }
}