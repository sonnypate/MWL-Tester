using CommunityToolkit.HighPerformance.Helpers;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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
        string _logDir;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public MainWindow()
        {
            _logger = Log.ForContext<MainWindow>();
            InitializeComponent();
            _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MWL-Tester\Logs\");
        }

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            await TestDicomConnection();
        }

        private async Task TestDicomConnection()
        {
            if (TestButton.Content.Equals("Cancel"))
            {
                _cts.Cancel();
                TestButton.Content = "Test";
            }
            // Try to reset the cancellation token to be used again if needed.
            else
            {
                _cts = new CancellationTokenSource();

                TestButton.Content = "Cancel";
                await PerformConnectionTest(_cts.Token);
                TestButton.Content = "Test";
            }
        }

        private void CalledPort_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private async Task PerformConnectionTest(CancellationToken cancellationToken)
        {
            var port = 0;
            var numeric = int.TryParse(CalledPort.Text, out port);

            if (numeric)
            {
                StatusText.Content = "Starting C-Echo";
                _logger.Information("Starting C-Echo");

                var client = DicomClientFactory.Create(CalledHost.Text, port, Properties.Settings.Default.UseTLS, CallingAET.Text, CalledAET.Text);
                client.AssociationAccepted += Client_AssociationAccepted;
                client.AssociationRequestTimedOut += Client_AssociationRequestTimedOut;
                client.AssociationRejected += Client_AssociationRejected;
                client.AssociationReleased += Client_AssociationReleased;

                try
                {
                    client.NegotiateAsyncOps();
                    
                    for (int i = 0; i < 10; i++)
                    {
                        await client.AddRequestAsync(new DicomCEchoRequest());
                    }

                    await client.SendAsync(cancellationToken);
                }
                catch (AggregateException ex)
                {
                    _logger.Error("Error: {exception}", ex);
                    MessageBox.Show($"Error while connecting to '{CalledHost.Text}': {Environment.NewLine}{ex.Message}", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Content = "Connection failed";
                    return;
                }

                // If the cancel button was clicked, then update status, otherwise if it made it this far then it was successful.
                if (cancellationToken.IsCancellationRequested)
                {
                    StatusText.Content = "Test cancelled";
                    return;
                }
                else
                {
                    MessageBox.Show($"Connection to {CalledHost.Text} was successful.", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Content = "Connection successful";
                }
            }
            else
            {
                MessageBox.Show("Called port must be a number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusBar(string message)
        {
            if (StatusText.Dispatcher.CheckAccess())
            {
                StatusText.Content = message;
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    StatusText.Content = message;
                });
            }
        }

        private void Client_AssociationRequestTimedOut(object? sender, FellowOakDicom.Network.Client.EventArguments.AssociationRequestTimedOutEventArgs e)
        {
            UpdateStatusBar("Connection timed out.");
        }

        private void Client_AssociationAccepted(object? sender, FellowOakDicom.Network.Client.EventArguments.AssociationAcceptedEventArgs e)
        {
            UpdateStatusBar("Association accepted");
        }

        private void Client_AssociationReleased(object? sender, EventArgs e)
        {
            UpdateStatusBar("Association released");
        }

        private void Client_AssociationRejected(object? sender, FellowOakDicom.Network.Client.EventArguments.AssociationRejectedEventArgs e)
        {
            UpdateStatusBar("Association rejected");
        }

        private void LogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Open log directory.
        }
    }
}