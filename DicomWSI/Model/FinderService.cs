using Dicom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DicomWSI.DAL;
using System.Data.SqlClient;
using System.Data;

namespace DicomWSI.Model
{
    public class SQLFindService : IDicomImageFinderService
    {
        public readonly string conn;
        public SQLFindService(string s)
        {
            conn = s;
        }
        public List<DicomFile> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID)
        {
            SqlHelper sqlHelper = new SqlHelper(conn);
            string sql = "SELECT StoragePath from Instance where 1=1";
            SqlParameter[] para = new SqlParameter[]
            {
                new SqlParameter("@PatientID", SqlDbType.VarChar, 100) { Value = PatientId },
                new SqlParameter("@StudyInstanceUID", SqlDbType.VarChar, 100) { Value = StudyUID },
                new SqlParameter("@SeriesInstanceUID", SqlDbType.VarChar, 100) { Value = SeriesUID },
            };
            if (PatientId != string.Empty)
                sql += " and PatientID = @PatientID";
            if (StudyUID != string.Empty)
                sql += " and StudyInstanceUID = @StudyInstanceUID";
            if (SeriesUID != string.Empty)
                sql += " and SeriesInstanceUID = @SeriesInstanceUID";
            var data = sqlHelper.ExecuteReader(sql, para);
            List<string> StoragePaths = new List<string>();
            for (int i = 0; i < data.Rows.Count; ++i)
            {
                StoragePaths.Add(data.Rows[i][0].ToString());
            }
            return SearchInFilesystem(StoragePaths);
        }

        public List<DicomFile> FindImageByUID(string InstanceUID)
        {
            SqlHelper sqlHelper = new SqlHelper(conn);
            string sql = "SELECT StoragePath from Image where SOPInstanceUID = @SOPInstanceUID";
            SqlParameter para = new SqlParameter("@SOPInstanceUID", SqlDbType.VarChar, 100) { Value = InstanceUID };
            var data = sqlHelper.ExecuteReader(sql, para);
            var StoragePath = data.Rows?[0]?[0].ToString();
            return SearchInFilesystem(new List<string>(new string[] { StoragePath }));
        }

        public List<DicomFile> FindPatientFiles(string PatientName, string PatientId)
        {
            SqlHelper sqlHelper = new SqlHelper(conn);
            string sql = "SELECT StoragePath from Instance where 1=1";
            SqlParameter[] para = new SqlParameter[]
            {
                new SqlParameter("@PatientName", SqlDbType.VarChar, 50) { Value = PatientName },
                new SqlParameter("@PatientID", SqlDbType.VarChar, 100) { Value = PatientId },
            };
            if (PatientName != string.Empty)
                sql += " and PatientName = @PatientName";
            if (PatientId != string.Empty)
                sql += " and PatientID = @PatientID";
            var data = sqlHelper.ExecuteReader(sql, para);
            List<string> StoragePaths = new List<string>();
            for (int i = 0; i < data.Rows.Count; ++i)
            {
                StoragePaths.Add(data.Rows[i][0].ToString());
            }
            return SearchInFilesystem(StoragePaths);
        }

        public List<DicomFile> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality)
        {
            SqlHelper sqlHelper = new SqlHelper(conn);
            string sql = "SELECT StoragePath from Instance where 1=1";
            SqlParameter[] para = new SqlParameter[]
            {
                new SqlParameter("@PatientName", SqlDbType.VarChar, 50) { Value = PatientName },
                new SqlParameter("@PatientID", SqlDbType.VarChar, 100) { Value = PatientId },
                new SqlParameter("@AccessionNumber", SqlDbType.VarChar, 50) { Value = AccessionNbr },
                new SqlParameter("@StudyInstanceUID", SqlDbType.VarChar, 100) { Value = StudyUID },
                new SqlParameter("@SeriesInstanceUID", SqlDbType.VarChar, 100) { Value = SeriesUID },
                new SqlParameter("@Modality", SqlDbType.VarChar, 50) { Value = Modality },
            };
            if (PatientName != string.Empty)
                sql += " and PatientName = @PatientName";
            if (PatientId != string.Empty)
                sql += " and PatientID = @PatientID";
            if (AccessionNbr != string.Empty)
                sql += " and AccessionNumber = @AccessionNumber";
            if (StudyUID != string.Empty)
                sql += " and StudyInstanceUID = @StudyInstanceUID";
            if (SeriesUID != string.Empty)
                sql += " and SeriesInstanceUID = @SeriesInstanceUID";
            if (Modality != string.Empty)
                sql += " and Modality = @Modality";
            var data = sqlHelper.ExecuteReader(sql, para);
            List<string> StoragePaths = new List<string>();
            for (int i = 0; i < data.Rows.Count; ++i)
            {
                StoragePaths.Add(data.Rows[i][0].ToString());
            }
            return SearchInFilesystem(StoragePaths);
        }

        public List<DicomFile> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID)
        {
            SqlHelper sqlHelper = new SqlHelper(conn);
            string sql = "SELECT StoragePath from Instance where 1=1";
            SqlParameter[] para = new SqlParameter[]
            {
                new SqlParameter("@PatientName", SqlDbType.VarChar, 50) { Value = PatientName },
                new SqlParameter("@PatientID", SqlDbType.VarChar, 100) { Value = PatientId },
                new SqlParameter("@AccessionNumber", SqlDbType.VarChar, 50) { Value = AccessionNbr },
                new SqlParameter("@StudyInstanceUID", SqlDbType.VarChar, 100) { Value = StudyUID },
            };
            if (PatientName != string.Empty)
                sql += " and PatientName = @PatientName";
            if (PatientId != string.Empty)
                sql += " and PatientID = @PatientID";
            if (AccessionNbr != string.Empty)
                sql += " and AccessionNumber = @AccessionNumber";
            if (StudyUID != string.Empty)
                sql += " and StudyInstanceUID = @StudyInstanceUID";
            var data = sqlHelper.ExecuteReader(sql, para);
            List<string> StoragePaths = new List<string>();
            for (int i = 0; i < data.Rows.Count; ++i)
            {
                StoragePaths.Add(data.Rows[i][0].ToString());
            }
            return SearchInFilesystem(StoragePaths);
        }

        private List<DicomFile> SearchInFilesystem(IEnumerable<string> fileNames)
        {
            var matchingFiles = new List<DicomFile>();
            foreach (string fileNameToTest in fileNames)
            {
                var dcmFile = DicomFile.Open(fileNameToTest);
                matchingFiles.Add(dcmFile);
            }
            return matchingFiles;
        }
    }

    public class FileFinderService : IDicomImageFinderService
    {
        private static string StoragePath = @".\DicomWSIStorage";


        public List<DicomFile> FindPatientFiles(string PatientName, string PatientId)
        {
            // usually here a SQL statement is built to query a Patient-table
            return SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    return matches;
                });
        }


        public List<DicomFile> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID)
        {
            // usually here a SQL statement is built to query a Study-table
            return SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    return matches;
                });
        }


        public List<DicomFile> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality)
        {
            // usually here a SQL statement is built to query a Series-table
            return SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));
                    matches &= MatchFilter(Modality, dcmFile.GetSingleValueOrDefault(DicomTag.Modality, string.Empty));
                    return matches;
                });
        }

        public List<DicomFile> FindImageByUID(string InstanceUID)
        {
            string dicomRootDirectory = StoragePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<DicomFile>(); // holds the file matching the criteria. one representative file per key
            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);
                    if (dcmFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) == InstanceUID)
                    {
                        matchingFiles.Add(dcmFile);
                        return matchingFiles;
                        //return new List<DicomFile>(new DicomFile[] { dcmFile });
                    }
                }
                catch (Exception)
                {
                    // invalid file, ignore here
                }
            }
            return matchingFiles;
        }

        private List<DicomFile> SearchInFilesystem(Func<DicomDataset, string> level, Func<DicomDataset, bool> matches)
        {
            string dicomRootDirectory = StoragePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<DicomFile>(); // holds the file matching the criteria. one representative file per key
            var foundKeys = new List<string>(); // holds the list of keys that have already been found so that only one file per key is returned

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    var key = level(dcmFile.Dataset);
                    if (!string.IsNullOrEmpty(key))/* && !foundKeys.Contains(key))*/
                    {
                        if (matches(dcmFile.Dataset))
                        {
                            matchingFiles.Add(dcmFile);
                            foundKeys.Add(key);
                        }
                    }
                }
                catch (Exception)
                {
                    // invalid file, ignore here
                }
            }
            return matchingFiles;
        }


        public List<DicomFile> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID)
        {
            // normally here a SQL query is constructed. But this implementation searches in file system
            string dicomRootDirectory = StoragePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<DicomFile>();

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    bool matches = true;
                    matches &= MatchFilter(PatientId, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));

                    if (matches)
                    {
                        matchingFiles.Add(dcmFile);
                    }
                }
                catch (Exception)
                {
                    // invalid file, ignore here
                }
            }
            return matchingFiles;
        }


        private bool MatchFilter(string filterValue, string valueToTest)
        {
            if (string.IsNullOrEmpty(filterValue))
            {
                // if the QR SCU sends an empty tag, then no filtering should happen
                return true;
            }
            // take into account, that strings may contain a *-wildcard
            var filterRegex = "^" + Regex.Escape(filterValue).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(valueToTest, filterRegex, RegexOptions.IgnoreCase);
        }
    }
}
