using System;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using Dicom.Log;
using Dicom.Network;

namespace DicomWSI
{
    public partial class WSIService : DicomService, IDicomServiceProvider,
    IDicomCStoreProvider, IDicomCEchoProvider, IDicomNServiceProvider,
    IDicomCFindProvider, IDicomCMoveProvider, IDicomCGetProvider
    {
        string StoragePath = @".\DicomWSIStorage";

        private static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
            {
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian,
            };

        private static readonly DicomTransferSyntax[] AcceptedImageTransferSyntaxes = new DicomTransferSyntax[]
            {
                // Lossless
                DicomTransferSyntax.JPEGLSLossless,
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEGProcess14SV1,
                DicomTransferSyntax.JPEGProcess14,
                DicomTransferSyntax.RLELossless,

                // Lossy
                DicomTransferSyntax.JPEGLSNearLossless,
                DicomTransferSyntax.JPEG2000Lossy,
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEGProcess2_4,

                // Uncompressed
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian
            };

        public WSIService(INetworkStream stream, Encoding fallbackEncoding, Logger log)
        : base(stream, fallbackEncoding, log)
        {
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            Logger.Info($"Received verification request from AE {Association.CallingAE} with IP: {Association.RemoteHost}");
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        public void OnConnectionClosed(Exception exception)
        {
            Clean();
        }

        private void Clean()
        {

        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            //log the abort reason
            Logger.Error($"Received abort from {source}, reason is {reason}");
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            Clean();
            return SendAssociationReleaseResponseAsync();
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            Logger.Info($"Received association request from AE: {association.CallingAE} with IP: {association.RemoteHost} ");

            if (WSIServer.AETitle != association.CalledAE)
            {
                Logger.Error($"Association with {association.CallingAE} rejected since called aet {association.CalledAE} is unknown");
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.ModalityWorklistInformationModelFIND
                    || pc.AbstractSyntax == DicomUID.ModalityPerformedProcedureStepSOPClass
                    || pc.AbstractSyntax == DicomUID.ModalityPerformedProcedureStepNotificationSOPClass
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelFIND
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelMOVE
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelFIND
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelMOVE
                    || pc.AbstractSyntax == DicomUID.CompositeInstanceRootRetrieveMOVE)
                {
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelGET
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelGET
                    || pc.AbstractSyntax == DicomUID.CompositeInstanceRootRetrieveGET
                    || pc.AbstractSyntax == DicomUID.CompositeInstanceRetrieveWithoutBulkDataGET)
                {
                    pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
                }
                else
                {
                    Logger.Warn($"Requested abstract syntax {pc.AbstractSyntax} from {association.CallingAE} not supported");
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Logger.Info($"Accepted association request from {association.CallingAE}");
            return SendAssociationAcceptAsync(association);
        }
    }
}
