using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Threading;
using DicomWSI.Model;
using Dicom.Log;

namespace DicomWSI
{
    class WSIServer
    {
        private static IDicomServer _server;
        private static Timer _itemsLoaderTimer;

        protected WSIServer()
        {
        }

        public static string AETitle { get; set; }

        public static IDicomImageFinderService GetCreateFinderService()
        {
            //string conn = @"Data Source=.\;Initial Catalog=DICOMData;Integrated Security=True";
            string conn = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=DICOMData;Data Source=PC-20170905QAWG\MS11";
            try
            {
                var sqlHelper = new DAL.SqlHelper(conn);
                sqlHelper.ExecuteReader("SELECT * FROM PATIENT");
            }
            catch (Exception)
            {
                return new FileFinderService();
            }
            return new SQLFindService(conn);
        }

        public static IWorklistItemsSource CreateItemsSourceService => new WorklistItemsProvider();

        public static List<WorklistItem> CurrentWorklistItems { get; private set; }

        public static void Start(int port, string aet, Logger log = null)
        {
            AETitle = aet;
            _server = DicomServer.Create<WSIService>(port, null, null, null, log);
            _server.Logger.Info($"Start listening on port {port}...");
            // every 30 seconds the worklist source is queried and the current list of items is cached in _currentWorklistItems
            _itemsLoaderTimer = new Timer((state) =>
            {
                var newWorklistItems = CreateItemsSourceService.GetAllCurrentWorklistItems();
                CurrentWorklistItems = newWorklistItems;
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }


        public static void Stop()
        {
            _server?.Logger?.Info("Stop listening...");
            _server?.Dispose();
        }
    }
}
