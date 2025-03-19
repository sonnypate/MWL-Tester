namespace MWL_Tester.DICOM
{
    internal class Connection
    {
        public string CalledAET { get; set; } = string.Empty;
        public string CalledHost { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool UseTLS { get; set; } = false;
        public string CallingAET { get; set; } = string.Empty;
    }
}
