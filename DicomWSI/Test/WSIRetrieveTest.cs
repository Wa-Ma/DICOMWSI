using Dicom;
using Dicom.Network;
using System.IO;

namespace DicomWSI.Test
{
    class WSIRetrieveTest
    {
        public static void Run(string storagePath)
        {
            string QRServerHost = "localhost";
            int QRServerPort = 26104;
            string QRServerAET = "WSIServer";
            string AET = "QetrieveSCU";

            var client = new DicomClient();
            client.NegotiateAsyncOps();

            var cGetRequest = new DicomCGetRequest(
                "1.2.276.0.7230010.3.1.2.296485376.1.1484917366.62818",
                "1.2.276.0.7230010.3.1.3.296485376.1.1484917366.62819",
                "1.2.276.0.7230010.3.1.4.296485376.1.1484917428.62845");
            cGetRequest.Dataset.AddOrUpdate(DicomTag.QueryRetrieveLevel, DicomQueryRetrieveLevel.NotApplicable);
            cGetRequest.Command.Add(DicomTag.SimpleFrameList, new uint[] { 0, 1 });

            client.OnCStoreRequest += (DicomCStoreRequest req) =>
            {
                SaveImage(req.Dataset, storagePath);
                return new DicomCStoreResponse(req, DicomStatus.Success);
            };
            // the client has to accept storage of the images. We know that the requested images are of SOP class Secondary capture, 
            // so we add the Secondary capture to the additional presentation context
            // a more general approach would be to mace a cfind-request on image level and to read a list of distinct SOP classes of all
            // the images. these SOP classes shall be added here.
            var pcs = DicomPresentationContext.GetScpRolePresentationContextsFromStorageUids(
                DicomStorageCategory.Image,
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian,
                DicomTransferSyntax.ImplicitVRBigEndian,
                DicomTransferSyntax.JPEGLSLossless,
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEGProcess14SV1,
                DicomTransferSyntax.JPEGProcess14,
                DicomTransferSyntax.RLELossless,
                DicomTransferSyntax.JPEGLSNearLossless,
                DicomTransferSyntax.JPEG2000Lossy,
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEGProcess2_4);
            client.AdditionalPresentationContexts.AddRange(pcs);

            client.AddRequest(cGetRequest);
            client.Send(QRServerHost, QRServerPort, false, AET, QRServerAET);
        }

        private static void SaveImage(DicomDataset dataset, string storagePath)
        {
            var studyUid = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            var instUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

            var path = Path.GetFullPath(storagePath);
            path = Path.Combine(path, studyUid);

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            path = Path.Combine(path, instUid) + ".dcm";

            new DicomFile(dataset).Save(path);
        }
    }
}
