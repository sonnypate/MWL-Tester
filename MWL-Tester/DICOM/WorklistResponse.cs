﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWL_Tester.DICOM
{
    internal class WorklistResponse
    {
        public string PatientName { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string Accession { get; set;} = string.Empty;
        public string Modality { get; set;} = string.Empty;
        public string ScheduledStudyDate { get; set; } = string.Empty;
        public string StudyInstanceUID { get; set; } = string.Empty;
    }
}