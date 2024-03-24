using FellowOakDicom.Network.Client;
using FellowOakDicom.Network;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MWL_Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            var client = DicomClientFactory.Create(ServerIpAddress.Text, int.Parse(ServerPort.Text), false, CallingAET.Text, ServerAET.Text);
            client.NegotiateAsyncOps();
            for (int i = 0; i < 10; i++)
                await client.AddRequestAsync(new DicomCEchoRequest());
            await client.SendAsync();
            
        }

    }
}