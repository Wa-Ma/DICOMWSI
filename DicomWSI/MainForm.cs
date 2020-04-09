using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dicom.Log;
using DicomWSI.Model;
using DicomWSI.Test;

namespace DicomWSI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LogManager.SetImplementation(new TextWriterLogManager(new LogBoxWriter(textLog)));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WSIServer.Start(int.Parse(textBox3.Text), textBox2.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            WSIServer.Stop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            WSIServer.Stop();
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textLog.Clear();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Task.Run(() => WSIStorageTest.Run(textBox5.Text));
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Task.Run(() => WSIRetrieveTest.Run(textBox6.Text));
        }
    }
}
