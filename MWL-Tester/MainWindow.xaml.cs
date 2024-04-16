using CommunityToolkit.HighPerformance.Helpers;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using MWL_Tester.DICOM;
using Serilog;
using System.Collections.ObjectModel;
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
        private ILogger _logger;
        private string _logDir;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private int _resultCounter = 0;
        WorklistQuery _worklistQuery;

        //ObservableCollection<WorklistResponse> WorklistResponses { get; set; } = new ObservableCollection<WorklistResponse>();

        public MainWindow()
        {
            _logger = Log.ForContext<MainWindow>();
            InitializeComponent();
            _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MWL-Tester\Logs\");
            _worklistQuery = new WorklistQuery();
            QueryResultsGrid.ItemsSource = _worklistQuery.WorklistResponses;
            _worklistQuery.WorklistResponses.CollectionChanged += WorklistResponses_CollectionChanged;
        }

        private void WorklistResponses_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _resultCounter++;
            UpdateStatusBar($"Found {_resultCounter.ToString()} results.");
        }

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            if (TestButton.Content.Equals("Cancel"))
            {
                _cts.Cancel();
                TestButton.Content = "Test";
            }
            // Reset the cancellation token to be used again if needed.
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
            // Only allow numeric values to be typed
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
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = _logDir,
                UseShellExecute = true
            };

            // Open log directory.
            try
            {
                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                _logger.Error("Error while opening '{dir}': {exception}", _logDir, ex);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            _resultCounter = 0;

            UpdateStatusBar("Starting worklist query");
            
            WorklistRequest request = new WorklistRequest()
            {
                Accession = AccessionText.Text,
                PatientID = PatientIdText.Text,
                PatientName = PatientNameText.Text,
                StationAET = string.Empty,
                StationName = string.Empty,
                Modality = ModalityText.Text,
                ScheduledDateTime = null
            };

            Connection connection = new Connection()
            {
                CalledAET = CalledAET.Text,
                CalledHost = CalledHost.Text,
                CallingAET = CallingAET.Text,
                Port = Int32.Parse(CalledPort.Text),
                UseTLS = UseTlsCheckbox.IsChecked.Value
            };


            var client = DicomClientFactory.Create(connection.CalledHost, connection.Port, connection.UseTLS, connection.CallingAET, connection.CalledAET);
            client.AssociationAccepted += Client_AssociationAccepted;
            client.AssociationRequestTimedOut += Client_AssociationRequestTimedOut;
            client.AssociationRejected += Client_AssociationRejected;
            client.AssociationReleased += Client_AssociationReleased;


            var dataset = await _worklistQuery.PerformWorklistQuery(client, CreateWorklistRequest(request));

            _worklistQuery.GetWorklistValuesFromDataset(dataset);
        }

        private DicomCFindRequest CreateWorklistRequest(WorklistRequest requestParams)
        {
            var worklistQuery = DicomCFindRequest.CreateWorklistQuery(
                patientId: requestParams.PatientID,
                patientName: requestParams.PatientName,
                stationAE: requestParams.StationAET,
                stationName: requestParams.StationName,
                modality: requestParams.Modality,
                scheduledDateTime: requestParams.ScheduledDateTime
                );

            return worklistQuery;
        }
    }
}
