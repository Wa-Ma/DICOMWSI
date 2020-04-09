using Dicom;
using System.Collections.Generic;

namespace DicomWSI.Model
{

    public interface IDicomImageFinderService
    {

        /// <summary>
        /// Searches in a DICOM store for patient information. Returns a representative DICOM file per found patient
        /// </summary>
        List<DicomFile> FindPatientFiles(string PatientName, string PatientId);

        /// <summary>
        /// Searches in a DICOM store for study information. Returns a representative DICOM file per found study
        /// </summary>
        List<DicomFile> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID);

        /// <summary>
        /// Searches in a DICOM store for series information. Returns a representative DICOM file per found serie
        /// </summary>
        List<DicomFile> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality);

        /// <summary>
        /// Searches in a DICOM store for all files matching the given UIDs
        /// </summary>
        List<DicomFile> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID);


        List<DicomFile> FindImageByUID(string InstanceUID);
    }
}
