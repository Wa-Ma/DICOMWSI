using System;
using Dicom.Log;
using Dicom.Network;
using Dicom;
using System.IO;
using DicomWSI.DAL;
using System.Data.SqlClient;
using System.Data;

namespace DicomWSI
{
    public partial class WSIService : DicomService, IDicomServiceProvider,
        IDicomCStoreProvider, IDicomCEchoProvider, IDicomNServiceProvider,
        IDicomCFindProvider, IDicomCMoveProvider, IDicomCGetProvider
    {
        public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
        {
            var studyUid = request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            var instUid = request.SOPInstanceUID.UID;
            var dicomFile = request.File;

            var path = Path.GetFullPath(StoragePath);
            path = Path.Combine(path, studyUid);

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            path = Path.Combine(path, instUid) + ".dcm";

            dicomFile.Save(path);
            Update(dicomFile, path);

            Logger.Info($"Success storaged from {Association.CallingAE}");
            return new DicomCStoreResponse(request, DicomStatus.Success);
        }

        private void Update(DicomFile dicomFile, string path)
        {
            try
            {
                //string conn = @"Data Source=.\;Initial Catalog=DICOMData;Integrated Security=True";
                string conn = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=DICOMData;Data Source=PC-20170905QAWG\MS11";
                var sqlHelper = new SqlHelper(conn);
                sqlHelper.ExecuteReader("SELECT * FROM Patient");
                var PatientID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientID);
                var PatientName = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName);
                var PatientBirth = dicomFile.Dataset.GetSingleValue<DateTime>(DicomTag.PatientBirthDate);
                var PatientSex = dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientSex);

                var StudyID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyID);
                var AccessionNumber = dicomFile.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber);
                var StudyDate = dicomFile.Dataset.GetSingleValue<DateTime>(DicomTag.StudyDate);
                var Modality = dicomFile.Dataset.GetSingleValue<string>(DicomTag.Modality);
                var StudyInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);

                var SeriesInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                var SeriesNumber = dicomFile.Dataset.GetSingleValue<int>(DicomTag.SeriesNumber);

                var SOPInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
                var SOPClassUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID);
                var NumberOfFrames = dicomFile.Dataset.GetSingleValue<int>(DicomTag.NumberOfFrames);
                var StoragePath = path;

                SqlParameter[][] para = new SqlParameter[4][]
                {
                    new SqlParameter[]
                    {
                        new SqlParameter("@PatientID", SqlDbType.VarChar, 100) { Value = PatientID },
                        new SqlParameter("@PatientName", SqlDbType.VarChar, 50) { Value = PatientName },
                        new SqlParameter("@PatientBirth", SqlDbType.DateTime) { Value = PatientBirth },
                        new SqlParameter("@PatientSex", SqlDbType.VarChar, 50) { Value = PatientSex },
                    },
                    new SqlParameter[]
                    {
                        new SqlParameter("@StudyID", SqlDbType.VarChar, 50) { Value = StudyID },
                        new SqlParameter("@AccessionNumber", SqlDbType.VarChar, 50) { Value = AccessionNumber },
                        new SqlParameter("@StudyDate", SqlDbType.DateTime) { Value = StudyDate },
                        new SqlParameter("@Modality", SqlDbType.VarChar, 50) { Value = Modality },
                        new SqlParameter("@StudyInstanceUID", SqlDbType.VarChar, 100) { Value = StudyInstanceUID },
                        new SqlParameter("@PatientID", SqlDbType.VarChar, 100) { Value = PatientID },
                    },
                    new SqlParameter[]
                    {
                        new SqlParameter("@SeriesInstanceUID", SqlDbType.VarChar, 100) { Value = SeriesInstanceUID },
                        new SqlParameter("@SeriesNumber", SqlDbType.Int) { Value = SeriesNumber },
                        new SqlParameter("@StudyInstanceUID", SqlDbType.VarChar, 100) { Value = StudyInstanceUID },
                    },
                    new SqlParameter[]
                    {
                        new SqlParameter("@SOPInstanceUID", SqlDbType.VarChar, 100) { Value = SOPInstanceUID },
                        new SqlParameter("@SOPClassUID", SqlDbType.VarChar, 100) { Value = SOPClassUID },
                        new SqlParameter("@NumberOfFrames", SqlDbType.Int) { Value = NumberOfFrames },
                        new SqlParameter("@StoragePath", SqlDbType.VarChar, 300) { Value = StoragePath },
                        new SqlParameter("@SeriesInstanceUID", SqlDbType.VarChar, 100) { Value = SeriesInstanceUID },
                    },
                };

                string[] sql =
                {
                    "INSERT INTO Patient VALUES (@PatientID, @PatientName, @PatientBirth, @PatientSex)",
                    "INSERT INTO Study VALUES (@StudyInstanceUID, @StudyID, @AccessionNumber, @StudyDate, @Modality, @PatientID)",
                    "INSERT INTO Series VALUES (@SeriesInstanceUID, @SeriesNumber, @StudyInstanceUID)",
                    "INSERT INTO Image VALUES (@SOPInstanceUID, @SOPClassUID, @NumberOfFrames, @StoragePath, @SeriesInstanceUID)"
                };

                for (int i = 0; i < 4; ++i)
                {
                    try
                    {
                        sqlHelper.ExecuteNonQuery(sql[i], para[i]);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }

        public void OnCStoreRequestException(string tempFileName, Exception e)
        {
            throw new NotImplementedException();
        }
    }
}
