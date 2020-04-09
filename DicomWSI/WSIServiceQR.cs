using System;
using System.Collections.Generic;
using System.Threading;
using Dicom;
using Dicom.Imaging;
using Dicom.Log;
using Dicom.Network;
using DicomWSI.Model;


namespace DicomWSI
{
    public partial class WSIService : DicomService, IDicomServiceProvider,
        IDicomCStoreProvider, IDicomCEchoProvider, IDicomNServiceProvider,
        IDicomCFindProvider, IDicomCMoveProvider, IDicomCGetProvider
    {
        public IEnumerable<DicomCFindResponse> OnCFindRequest(DicomCFindRequest request)
        {
            var queryLevel = request.Level;

            var matchingFiles = new List<DicomFile>();
            IDicomImageFinderService finderService = WSIServer.GetCreateFinderService();
            switch (queryLevel)
            {
                case DicomQueryRetrieveLevel.Patient:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);

                        matchingFiles = finderService.FindPatientFiles(patname, patid);
                    }
                    break;
                case DicomQueryRetrieveLevel.Study:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                        var accNr = request.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                        var studyUID = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);

                        matchingFiles = finderService.FindStudyFiles(patname, patid, accNr, studyUID);
                    }
                    break;
                case DicomQueryRetrieveLevel.Series:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                        var accNr = request.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                        var studyUID = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
                        var seriesUID = request.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
                        var modality = request.Dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);

                        matchingFiles = finderService.FindSeriesFiles(patname, patid, accNr, studyUID, seriesUID, modality);
                    }
                    break;
                case DicomQueryRetrieveLevel.Image:
                    {
                        //matchingFiles = finderService.FindImageByUID(request.Command.GetSingleValue<string>(DicomTag.SOPInstanceUID));

                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                        var accNr = request.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                        var studyUID = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
                        var seriesUID = request.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
                        var modality = request.Dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);

                        matchingFiles = finderService.FindSeriesFiles(patname, patid, accNr, studyUID, seriesUID, modality);

                    }
                    break;
                default:
                    break;
            }
            // now read the required dicomtags from the matching files and return as results
            if (queryLevel != DicomQueryRetrieveLevel.NotApplicable)
            {
                foreach (var matchingFile in matchingFiles)
                {
                    var dicomFile = matchingFile;
                    var result = new DicomDataset();
                    foreach (var requestedTag in request.Dataset)
                    {
                        // most of the requested DICOM tags are stored in the DICOM files and therefore saved into a database.
                        // you can fill the responses by selecting the values from the database.
                        // also be aware that there are some requested DicomTags like "ModalitiesInStudy" or "NumberOfStudyRelatedInstances" 
                        // or "NumberOfPatientRelatedInstances" and so on which have to be calculated and cannot be read from a DICOM file.
                        if (dicomFile.Dataset.Contains(requestedTag.Tag))
                        {
                            dicomFile.Dataset.CopyTo(result, requestedTag.Tag);
                        }
                        // else if (requestedTag == DicomTag.NumberOfStudyRelatedInstances)
                        // {
                        //    ... somehow calculate how many instances are stored within the study
                        //    result.Add(DicomTag.NumberOfStudyRelatedInstances, number);
                        // } ....

                        else
                        {
                            result.Add(requestedTag);
                        }
                    }
                    yield return new DicomCFindResponse(request, DicomStatus.Pending) { Dataset = result };
                }
            }
            else
            {
                foreach (DicomDataset result in WorklistHandler.FilterWorklistItems(request.Dataset, WSIServer.CurrentWorklistItems))
                {
                    yield return new DicomCFindResponse(request, DicomStatus.Pending) { Dataset = result };
                }
            }
            yield return new DicomCFindResponse(request, DicomStatus.Success);
        }

        public IEnumerable<DicomCGetResponse> OnCGetRequest(DicomCGetRequest request)
        {
            IDicomImageFinderService finderService = WSIServer.GetCreateFinderService();
            List<DicomFile> matchingFiles = new List<DicomFile>();
            switch (request.Level)
            {
                case DicomQueryRetrieveLevel.Patient:
                    matchingFiles = finderService.FindFilesByUID(request.Dataset.GetSingleValue<string>(DicomTag.PatientID), string.Empty, string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Study:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID), string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Series:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.Image:
                    matchingFiles = finderService.FindImageByUID(request.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.NotApplicable:
                    {
                        var sopInstanceUid = request.Dataset.GetValue<string>(DicomTag.SOPInstanceUID, 0);
                        var sFrameList = request.Command.GetValues<uint>(DicomTag.SimpleFrameList);
                        var istance = finderService.FindImageByUID(sopInstanceUid)[0];
                        
                        var dicoms = istance.Dataset;
                        var pd = DicomPixelData.Create(dicoms, true);

                        istance = finderService.FindImageByUID(sopInstanceUid)[0];

                        DicomSequence perFrameFunctionalGroupsSequence = new DicomSequence(DicomTag.PerFrameFunctionalGroupsSequence);
                        foreach (int idx in sFrameList)
                        {
                            DicomPixelData dicomPixelData = DicomPixelData.Create(istance.Dataset);
                            pd.AddFrame(dicomPixelData.GetFrame(idx));
                            perFrameFunctionalGroupsSequence.Items.Add(
                                istance.Dataset.GetSequence(DicomTag.PerFrameFunctionalGroupsSequence).Items[idx]);
                        }

                        var fes = new DicomSequence(DicomTag.FrameExtractionSequence);
                        fes.Items.Add(new DicomDataset(
                            request.Dataset.GetDicomItem<DicomItem>(DicomTag.SOPInstanceUID),
                            request.Command.GetDicomItem<DicomItem>(DicomTag.SimpleFrameList)));
                        dicoms.AddOrUpdate(fes);

                        dicoms.AddOrUpdate(perFrameFunctionalGroupsSequence);
                        dicoms.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUID.Generate());

                        DicomFile dicomFile = new DicomFile(dicoms);
                        istance.FileMetaInfo.CopyTo(dicomFile.FileMetaInfo);
                        matchingFiles.Add(dicomFile);
                        //var simpleFrameList = request.Command.GetDicomItem<DicomItem>(DicomTag.SimpleFrameList);
                        //var instance = finderService.FindImageByUID(request.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID))[0];
                        //var df = GetWSIModel(instance, simpleFrameList);
                        //matchingFiles.Add(df);
                    }
                    break;
            }

            foreach (var matchingFile in matchingFiles)
            {
                var storeRequest = new DicomCStoreRequest(matchingFile);

                SendRequestAsync(storeRequest).Wait();
            }

            yield return new DicomCGetResponse(request, DicomStatus.Success);
        }

        public IEnumerable<DicomCMoveResponse> OnCMoveRequest(DicomCMoveRequest request)
        {
            // the c-move request contains the DestinationAE. the data of this AE should be configured somewhere.
            if (request.DestinationAE != "STORESCP")
            {
                yield return new DicomCMoveResponse(request, DicomStatus.QueryRetrieveMoveDestinationUnknown);
                yield return new DicomCMoveResponse(request, DicomStatus.ProcessingFailure);
                yield break;
            }

            // this data should come from some data storage!
            var destinationPort = 11112;
            var destinationIP = "localhost";

            IDicomImageFinderService finderService = WSIServer.GetCreateFinderService();
            List<DicomFile> matchingFiles = null;

            switch (request.Level)
            {
                case DicomQueryRetrieveLevel.Patient:
                    matchingFiles = finderService.FindFilesByUID(request.Dataset.GetSingleValue<string>(DicomTag.PatientID), string.Empty, string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Study:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID), string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Series:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.Image:
                    matchingFiles = finderService.FindImageByUID(request.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.NotApplicable:
                    var simpleFrameList = request.Dataset.GetDicomItem<DicomItem>(DicomTag.SimpleFrameList);
                    var instance = finderService.FindImageByUID(request.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID))[0];
                    var SOPInstanceUID = instance.Dataset.GetDicomItem<DicomItem>(DicomTag.SOPInstanceUID);
                    var df = GetWSIModel(SOPInstanceUID, simpleFrameList);
                    matchingFiles.Add(df);
                    break;
            }

            DicomClient client = new DicomClient();
            client.NegotiateAsyncOps();
            int storeTotal = matchingFiles.Count;
            int storeDone = 0; // this variable stores the number of instances that have already been sent
            int storeFailure = 0; // this variable stores the number of faulues returned in a OnResponseReceived
            foreach (var file in matchingFiles)
            {
                var storeRequest = new DicomCStoreRequest(file);

                storeRequest.OnResponseReceived += (req, resp) =>
                {
                    if (resp.Status == DicomStatus.Success)
                    {
                        Logger.Info("Storage of image successfull");
                        storeDone++;
                    }
                    else
                    {
                        Logger.Error("Storage of image failed");
                        storeFailure++;
                    }
                    // SendResponse(new DicomCMoveResponse(request, DicomStatus.Pending) { Remaining = storeTotal - storeDone - storeFailure, Completed = storeDone });
                };
                client.AddRequest(storeRequest);
            }

            // client.Send(destinationIP, destinationPort, false, QRServer.AETitle, request.DestinationAE);

            var sendTask = client.SendAsync(destinationIP, destinationPort, false, WSIServer.AETitle, request.DestinationAE);

            while (!sendTask.IsCompleted)
            {
                // while the send-task is runnin we inform the QR SCU every 2 seconds about the status and how many instances are remaining to send. 
                yield return new DicomCMoveResponse(request, DicomStatus.Pending) { Remaining = storeTotal - storeDone - storeFailure, Completed = storeDone };
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            Logger.Info("..fertig");
            yield return new DicomCMoveResponse(request, DicomStatus.Success);
        }

        private DicomFile GetWSIModel(DicomItem sopInstanceUID, DicomItem simpleFrameList)
        {
            IDicomImageFinderService finderService = WSIServer.GetCreateFinderService();
            var sopInstanceUid = (sopInstanceUID as DicomElement).Get<string>();
            var sFrameList = (simpleFrameList as DicomElement).Get<uint[]>();
            var istance = finderService.FindImageByUID(sopInstanceUid)[0];

            var dicoms = istance.Dataset;
            var pd = DicomPixelData.Create(dicoms, true);

            istance = finderService.FindImageByUID(sopInstanceUid)[0];

            DicomSequence perFrameFunctionalGroupsSequence = new DicomSequence(DicomTag.PerFrameFunctionalGroupsSequence);
            foreach (int idx in sFrameList)
            {
                DicomPixelData dicomPixelData = DicomPixelData.Create(istance.Dataset);
                pd.AddFrame(dicomPixelData.GetFrame(idx));
                perFrameFunctionalGroupsSequence.Items.Add(
                    istance.Dataset.GetSequence(DicomTag.PerFrameFunctionalGroupsSequence).Items[idx]);
            }

            var fes = new DicomSequence(DicomTag.FrameExtractionSequence);
            fes.Items.Add(new DicomDataset(
                sopInstanceUID,
                simpleFrameList));
            dicoms.AddOrUpdate(fes);

            dicoms.AddOrUpdate(perFrameFunctionalGroupsSequence);
            dicoms.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUID.Generate());

            DicomFile dicomFile = new DicomFile(dicoms);
            istance.FileMetaInfo.CopyTo(dicomFile.FileMetaInfo);
            return dicomFile;
        }
    }
}
