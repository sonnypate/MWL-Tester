﻿using FellowOakDicom.Network.Client;
using FellowOakDicom.Network;
using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Serilog;
using FellowOakDicom.Network.Client.EventArguments;

namespace MWL_Tester.DICOM
{
    internal class WorklistQuery
    {
        private ILogger _logger;

        public ObservableCollection<WorklistResponse> WorklistResponses { get; set; } = new ObservableCollection<WorklistResponse>();

        public WorklistQuery()
        {
            _logger = Log.ForContext<WorklistQuery>();
        }

        internal async Task<List<DicomDataset>> PerformWorklistQuery(IDicomClient client, DicomCFindRequest request)
        {
            var worklistItems = new List<DicomDataset>();

            request.OnResponseReceived = (DicomCFindRequest rq, DicomCFindResponse rp) =>
            {
                if (rp.HasDataset)
                {
                    _logger.Information("Study UID: {SUID}", rp.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
                    worklistItems.Add(rp.Dataset);
                }
                else
                {
                    _logger.Warning(rp.Status.ToString());
                }
            };

            await client.AddRequestAsync(request);
            await client.SendAsync();

            return worklistItems;
        }

        internal void GetWorklistValuesFromDataset(List<DicomDataset> datasets)
        {
            WorklistResponses.Clear();

            foreach (var dataset in datasets)
            {
                var worklist = new WorklistResponse();
                worklist.PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                worklist.PatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                worklist.Accession = dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                worklist.Modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
                worklist.StudyInstanceUID = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                worklist.ScheduledStudyDate = dataset.GetSingleValueOrDefault(DicomTag.StudyDate, string.Empty);

                WorklistResponses.Add(worklist);
            }
        }
    }
}
