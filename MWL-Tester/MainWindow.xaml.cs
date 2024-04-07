using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Serilog;
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
            var port = 0;
            var numeric = int.TryParse(CalledPort.Text, out port);
            
            if (numeric)
            {
                StatusText.Content = "Starting C-Echo";

                var client = DicomClientFactory.Create(CalledHost.Text, port, Properties.Settings.Default.UseTLS, CallingAET.Text, CalledAET.Text);
                client.AssociationAccepted += Client_AssociationAccepted;
                client.AssociationRequestTimedOut += Client_AssociationRequestTimedOut;

                try
                {
                    client.NegotiateAsyncOps();
                    for (int i = 0; i < 10; i++)
                        await client.AddRequestAsync(new DicomCEchoRequest());
                    await client.SendAsync(_cts.Token);
                }
                catch (AggregateException ex)
                {
                    _logger.Error("Error: {exception}", ex);
                    MessageBox.Show($"Error while connecting to '{CalledHost.Text}': {Environment.NewLine}{ex.Message}", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Content = "Connection failed";
                    return;
                }

                MessageBox.Show($"Connection to {CalledHost.Text} was successful.", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Content = "Connection successful";

            }
            else
            {
                MessageBox.Show("Called port must be a number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void CalledPort_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}