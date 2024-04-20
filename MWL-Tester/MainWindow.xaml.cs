using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using MWL_Tester.DICOM;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

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
        private WorklistQuery _worklistQuery;

        public MainWindow()
        {
            // Logging
            _logger = Log.ForContext<MainWindow>();
            _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MWL-Tester\Logs\");

            InitializeComponent();

            // Worklist query and binding
            _worklistQuery = new WorklistQuery();
            QueryResultsGrid.ItemsSource = _worklistQuery.WorklistResponses;
            _worklistQuery.WorklistResponses.CollectionChanged += WorklistResponses_CollectionChanged;
        }

        #region Menu Bar
        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog settings = new SettingsDialog();
            settings.ShowDialog();
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
        #endregion

        #region Main Form
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

        private async void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (QueryButton.Content.Equals("Cancel"))
            {
                _logger.Warning("Cancelled worklist query");
                _cts.Cancel();
                QueryButton.Content = "Submit";
            }
            // Reset the cancellation token to be used again if needed.
            else
            {
                _cts = new CancellationTokenSource();

                QueryButton.Content = "Cancel";
                await PerformWorklistQuery(_cts.Token);
                QueryButton.Content = "Submit";
            }
        }
        #endregion

        private async Task PerformWorklistQuery(CancellationToken cancellationToken)
        {
            var port = 0;
            var numeric = int.TryParse(CalledPort.Text, out port);

            if (numeric)
            {
                UpdateStatusBar("Starting worklist query");

                var request = DicomCFindRequest.CreateWorklistQuery(
                    patientId: PatientIdText.Text,
                    patientName: PatientNameText.Text,
                    stationAE: StationAetText.Text,
                    stationName: StationNameText.Text,
                    modality: ModalityText.Text
                );

                // Get the ScheduledProcedureStepSequence from the dataset created by the CreateWorklistQuery function.
                // Using this instead of the built-in scheduledDateTime parameter from the CreateWorklistQuery function because it requires
                // a range. This allows me to optionally select either a start date, both, or none:
                foreach (var item in request.Dataset.GetSequence(DicomTag.ScheduledProcedureStepSequence))
                {
                    // If a start date is selected, use this.
                    if (StartDatePicker.SelectedDate != null)
                    {
                        item?.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, StartDatePicker.SelectedDate.GetValueOrDefault());
                    }

                    // if the end date is selected, but there's no start date, then copy the end date to the start date.
                    if (StartDatePicker.SelectedDate == null && EndDatePicker.SelectedDate != null)
                    {
                        StartDatePicker.SelectedDate = EndDatePicker.SelectedDate.GetValueOrDefault();
                    }

                    // If both start date and end date are selected then create a range with date and time.
                    if (StartDatePicker.SelectedDate != null && EndDatePicker.SelectedDate != null)
                    {
                        var dr = new DicomDateRange(StartDatePicker.SelectedDate.GetValueOrDefault(), EndDatePicker.SelectedDate.GetValueOrDefault().AddDays(1).AddTicks(-1));
                        item?.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, dr);
                        item?.AddOrUpdate(DicomTag.ScheduledProcedureStepStartTime, dr);
                    }
                }

                try
                {
                    var client = GetDicomClient();

                    var dataset = await _worklistQuery.PerformWorklistQuery(client, request, cancellationToken);
                    _worklistQuery.GetWorklistValuesFromDataset(dataset);
                }
                catch (AggregateException ex)
                {
                    _logger.Error("Error: {exception}", ex);
                    MessageBox.Show($"{ex.Message}", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusBar("Error");
                    return;
                }
            }
            else
            {
                MessageBox.Show($"The port can only contain numeric values. Current port value is '{CalledPort.Text}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PerformConnectionTest(CancellationToken cancellationToken)
        {
            var port = 0;
            var numeric = int.TryParse(CalledPort.Text, out port);

            if (numeric)
            {
                UpdateStatusBar("Starting C-Echo");
                _logger.Information("Starting C-Echo");

                var client = GetDicomClient();

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
                    UpdateStatusBar("Connection failed");
                    return;
                }

                // If the cancel button was clicked, then update status, otherwise if it made it this far then it was successful.
                if (cancellationToken.IsCancellationRequested)
                {
                    UpdateStatusBar("Test cancelled");
                    return;
                }
                else
                {
                    MessageBox.Show($"Connection to {CalledHost.Text} was successful.", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatusBar("Connection successful");
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

        private IDicomClient GetDicomClient()
        {
            Connection connection = new Connection()
            {
                CalledAET = CalledAET.Text,
                CalledHost = CalledHost.Text,
                CallingAET = CallingAET.Text,
                Port = Int32.Parse(CalledPort.Text),
                UseTLS = UseTlsCheckbox.IsChecked.GetValueOrDefault()
            };

            var client = DicomClientFactory.Create(connection.CalledHost, connection.Port, connection.UseTLS, connection.CallingAET, connection.CalledAET);

            // Additional logging for debug:
            client.ServiceOptions.LogDimseDatasets = Properties.Settings.Default.Client_LogDimseDatasets;
            client.ServiceOptions.LogDataPDUs = Properties.Settings.Default.Client_LogDataPDUs;

            // Client events to update the status bar.
            client.AssociationAccepted += Client_AssociationAccepted;
            client.AssociationRequestTimedOut += Client_AssociationRequestTimedOut;
            client.AssociationRejected += Client_AssociationRejected;
            client.AssociationReleased += Client_AssociationReleased;

            return client;
        }

        private void CalledPort_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numeric values to be typed
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        #region Status Bar
        private void WorklistResponses_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateStatusBar($"Found {_worklistQuery.WorklistResponses.Count} results.");
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
        #endregion
    }
}
