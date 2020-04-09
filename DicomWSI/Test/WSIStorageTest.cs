using Dicom.Network;
using System.IO;

namespace DicomWSI.Test
{
    class WSIStorageTest
    {
        public static void Run(string rootPath)
        {
            var client = new DicomClient();
            client.NegotiateAsyncOps();
            DirectoryInfo TheFolder = new DirectoryInfo(Path.GetFullPath(rootPath));
            foreach (FileInfo file in TheFolder.GetFiles())
            {
                client.AddRequest(new DicomCStoreRequest(file.FullName));
            }
            client.Send("127.0.0.1", 26104, false, "TestSCU", "WSIServer");
            //System.Windows.Forms.MessageBox.Show("测试完毕");
        }
    }
}
