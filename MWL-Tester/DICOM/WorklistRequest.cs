using FellowOakDicom;
using FellowOakDicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWL_Tester.DICOM
{
    internal class WorklistRequest
    {
        public string Accession { get; set; } = string.Empty;
        public string PatientID { get; set; } = string.Empty;
        public string PatientName { get; set;} = string.Empty;
        public string StationAET { get; set;} = string.Empty;
        public string StationName { get; set;} = string.Empty;
        public string Modality { get; set;} = string.Empty;
        public DicomDateRange? ScheduledDateTime { get; set; }
    }
}
