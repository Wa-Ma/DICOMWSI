using System.Collections.Generic;
using System.Linq;
using Dicom;
using Dicom.Log;
using Dicom.Network;
using DicomWSI.Model;

namespace DicomWSI
{
    public partial class WSIService : DicomService, IDicomServiceProvider,
        IDicomCStoreProvider, IDicomCEchoProvider, IDicomNServiceProvider,
        IDicomCFindProvider, IDicomCMoveProvider, IDicomCGetProvider
    {
        private IMppsSource _mppsSource;
        private IMppsSource MppsSource
        {
            get
            {
                if (_mppsSource == null) _mppsSource = new MppsHandler(Logger);
                return _mppsSource;
            }
        }

        public DicomNCreateResponse OnNCreateRequest(DicomNCreateRequest request)
        {
            if (request.SOPClassUID == DicomUID.ModalityPerformedProcedureStepSOPClass)
            {
                // on N-Create the UID is stored in AffectedSopInstanceUID, in N-Set the UID is stored in RequestedSopInstanceUID
                var affectedSopInstanceUID = request.Command.GetSingleValue<string>(DicomTag.AffectedSOPInstanceUID);
                Logger.Log(LogLevel.Info, $"reciving N-Create with SOPUID {affectedSopInstanceUID}");
                // get the procedureStepIds from the request
                var procedureStepId = request.Dataset
                    .GetSequence(DicomTag.ScheduledStepAttributesSequence)
                    .First()
                    .GetSingleValue<string>(DicomTag.ScheduledProcedureStepID);
                var ok = MppsSource.SetInProgress(affectedSopInstanceUID, procedureStepId);

                return new DicomNCreateResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);

            }
            else if (request.SOPClassUID == DicomUID.GeneralPurposePerformedProcedureStepSOPClassRETIRED)
            {
                if (request.Dataset.GetSingleValue<string>(DicomTag.PerformedProcedureStepStatus) != "IN PROGRESS")
                    return new DicomNCreateResponse(request, DicomStatus.InvalidAttributeValue);

                // on N-Create the UID is stored in AffectedSopInstanceUID, in N-Set the UID is stored in RequestedSopInstanceUID
                var affectedSopInstanceUID = request.Command.GetSingleValue<string>(DicomTag.AffectedSOPInstanceUID);
                Logger.Log(LogLevel.Info, $"reeiving N-Create with SOPUID {affectedSopInstanceUID}");
                // get the procedureStepIds from the request
                var procedureStepId = request.Dataset
                    .GetSequence(DicomTag.ScheduledStepAttributesSequence)
                    .First()
                    .GetSingleValue<string>(DicomTag.ScheduledProcedureStepID);
                var ok = MppsSource.SetInProgress(affectedSopInstanceUID, procedureStepId);

                return new DicomNCreateResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
            }
            return new DicomNCreateResponse(request, DicomStatus.SOPClassNotSupported);
        }

        public DicomNSetResponse OnNSetRequest(DicomNSetRequest request)
        {
            if (request.SOPClassUID == DicomUID.ModalityPerformedProcedureStepSOPClass)
            {
                // on N-Create the UID is stored in AffectedSopInstanceUID, in N-Set the UID is stored in RequestedSopInstanceUID
                var requestedSopInstanceUID = request.Command.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID);
                Logger.Log(LogLevel.Info, $"receiving N-Set with SOPUID {requestedSopInstanceUID}");

                var status = request.Dataset.GetSingleValue<string>(DicomTag.PerformedProcedureStepStatus);
                if (status == "COMPLETED")
                {
                    // most vendors send some informations with the mpps-completed message. 
                    // this information should be stored into the datbase
                    var doseDescription = request.Dataset.GetSingleValueOrDefault(DicomTag.CommentsOnRadiationDose, string.Empty);
                    var listOfInstanceUIDs = new List<string>();
                    foreach (var seriesDataset in request.Dataset.GetSequence(DicomTag.PerformedSeriesSequence))
                    {
                        // you can read here some information about the series that the modalidy created
                        //seriesDataset.Get(DicomTag.SeriesDescription, string.Empty);
                        //seriesDataset.Get(DicomTag.PerformingPhysicianName, string.Empty);
                        //seriesDataset.Get(DicomTag.ProtocolName, string.Empty);
                        foreach (var instanceDataset in seriesDataset.GetSequence(DicomTag.ReferencedImageSequence))
                        {
                            // here you can read the SOPClassUID and SOPInstanceUID
                            var instanceUID = instanceDataset.GetSingleValueOrDefault(DicomTag.ReferencedSOPInstanceUID, string.Empty);
                            if (!string.IsNullOrEmpty(instanceUID)) listOfInstanceUIDs.Add(instanceUID);
                        }
                    }
                    var ok = MppsSource.SetCompleted(requestedSopInstanceUID, doseDescription, listOfInstanceUIDs);

                    return new DicomNSetResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
                }
                else if (status == "DISCONTINUED")
                {
                    // some vendors send a reason code or description with the mpps-discontinued message
                    // var reason = request.Dataset.Get(DicomTag.PerformedProcedureStepDiscontinuationReasonCodeSequence);
                    var ok = MppsSource.SetDiscontinued(requestedSopInstanceUID, string.Empty);

                    return new DicomNSetResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
                }
                else
                {
                    return new DicomNSetResponse(request, DicomStatus.InvalidAttributeValue);
                }
            }
            else if (request.SOPClassUID == DicomUID.GeneralPurposePerformedProcedureStepSOPClassRETIRED)
            {
                // on N-Create the UID is stored in AffectedSopInstanceUID, in N-Set the UID is stored in RequestedSopInstanceUID
                var requestedSopInstanceUID = request.Command.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID);
                Logger.Log(LogLevel.Info, $"receiving N-Set with SOPUID {requestedSopInstanceUID}");

                var status = request.Dataset.GetSingleValue<string>(DicomTag.PerformedProcedureStepStatus);
                if (status != "IN PROGRESS")
                {
                    return new DicomNSetResponse(request, DicomStatus.ProcessingFailure);
                }
                if (status == "COMPLETED")
                {
                    // most vendors send some informations with the mpps-completed message. 
                    // this information should be stored into the datbase
                    var doseDescription = request.Dataset.GetSingleValueOrDefault(DicomTag.CommentsOnRadiationDose, string.Empty);
                    var listOfInstanceUIDs = new List<string>();
                    foreach (var seriesDataset in request.Dataset.GetSequence(DicomTag.PerformedSeriesSequence))
                    {
                        // you can read here some information about the series that the modalidy created
                        //seriesDataset.Get(DicomTag.SeriesDescription, string.Empty);
                        //seriesDataset.Get(DicomTag.PerformingPhysicianName, string.Empty);
                        //seriesDataset.Get(DicomTag.ProtocolName, string.Empty);
                        foreach (var instanceDataset in seriesDataset.GetSequence(DicomTag.ReferencedImageSequence))
                        {
                            // here you can read the SOPClassUID and SOPInstanceUID
                            var instanceUID = instanceDataset.GetSingleValueOrDefault(DicomTag.ReferencedSOPInstanceUID, string.Empty);
                            if (!string.IsNullOrEmpty(instanceUID)) listOfInstanceUIDs.Add(instanceUID);
                        }
                    }
                    var ok = MppsSource.SetCompleted(requestedSopInstanceUID, doseDescription, listOfInstanceUIDs);

                    return new DicomNSetResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
                }
                else if (status == "DISCONTINUED")
                {
                    // some vendors send a reason code or description with the mpps-discontinued message
                    // var reason = request.Dataset.Get(DicomTag.PerformedProcedureStepDiscontinuationReasonCodeSequence);
                    var ok = MppsSource.SetDiscontinued(requestedSopInstanceUID, string.Empty);

                    return new DicomNSetResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
                }
                else
                {
                    return new DicomNSetResponse(request, DicomStatus.InvalidAttributeValue);
                }
            }
            return new DicomNSetResponse(request, DicomStatus.SOPClassNotSupported);
        }

        public DicomNActionResponse OnNActionRequest(DicomNActionRequest request)
        {
            if (request.SOPClassUID != DicomUID.GeneralPurposeScheduledProcedureStepSOPClassRETIRED)
            {
                return new DicomNActionResponse(request, DicomStatus.SOPClassNotSupported);
            }
            var requestedSopInstanceUID = request.Command.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID);
            Logger.Log(LogLevel.Info, $"receiving N-Action with SOPUID {requestedSopInstanceUID}");

            var status = request.Dataset.GetSingleValue<string>(DicomTag.GeneralPurposeScheduledProcedureStepStatusRETIRED);
            if (status == "IN PROGRESS")
            {
                var ok = MppsSource.SetInProgress(requestedSopInstanceUID, string.Empty);

                return new DicomNActionResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
            }
            else if (status == "SCHEDULED")
            {
                // some vendors send a reason code or description with the mpps-discontinued message
                // var reason = request.Dataset.Get(DicomTag.PerformedProcedureStepDiscontinuationReasonCodeSequence);
                var ok = MppsSource.SetDiscontinued(requestedSopInstanceUID, string.Empty);

                return new DicomNActionResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
            }
            else if (status == "SUSPENDED")
            {
                // some vendors send a reason code or description with the mpps-discontinued message
                // var reason = request.Dataset.Get(DicomTag.PerformedProcedureStepDiscontinuationReasonCodeSequence);
                var ok = MppsSource.SetDiscontinued(requestedSopInstanceUID, string.Empty);

                return new DicomNActionResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
            }
            else
            {
                return new DicomNActionResponse(request, DicomStatus.ProcessingFailure);
            }
            //Logger.Log(LogLevel.Info, "receiving N-Action, not supported");
            //return new DicomNActionResponse(request, DicomStatus.UnrecognizedOperation);
        }

        public DicomNGetResponse OnNGetRequest(DicomNGetRequest request)
        {
            if (request.SOPClassUID != DicomUID.GeneralPurposePerformedProcedureStepSOPClassRETIRED)
            {
                return new DicomNGetResponse(request, DicomStatus.SOPClassNotSupported);
            }
            return new DicomNGetResponse(request, DicomStatus.UnrecognizedOperation);
            //return new DicomNGetResponse(request, DicomStatus.UnrecognizedOperation);
        }

        #region not supported methods but that are required because of the interface

        public DicomNDeleteResponse OnNDeleteRequest(DicomNDeleteRequest request)
        {
            Logger.Log(LogLevel.Info, "receiving N-Delete, not supported");
            return new DicomNDeleteResponse(request, DicomStatus.UnrecognizedOperation);
        }

        public DicomNEventReportResponse OnNEventReportRequest(DicomNEventReportRequest request)
        {
            Logger.Log(LogLevel.Info, "receiving N-Event, not supported");
            return new DicomNEventReportResponse(request, DicomStatus.UnrecognizedOperation);
        }

        #endregion
    }
}
