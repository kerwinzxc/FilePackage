namespace WindowsFormsApp1
{
    using LDT.JudgetDoc.Infrastructure.Common.Tools;
    using PMPagkage;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //var yu = File.ReadAllBytes(@"D:\MyDownloads\Download\QCExplorer.rar");
            //File.WriteAllBytes(@"D:\QCExplorer.rar", yu);

            //File.Copy(@"D:\MyDownloads\Download\QCExplorer.rar", @"D:\QCExplorer.rar", true);

            //new FileTools().LocalDirectoryDelete("D:\\yugeV5");
            //new FileXMLPackage().Package("D:\\MyDownloads\\", "D:\\yugeV5\\");
            //MessageBox.Show("package right.");

            FolderBrowserDialog sourcepath = new FolderBrowserDialog();
            sourcepath.Description = "请选择要打包的文件夹.";
            if (sourcepath.ShowDialog() == DialogResult.OK)
            {
                FolderBrowserDialog targetpath = new FolderBrowserDialog();
                targetpath.Description = "请选择要打包后存储的文件夹.";
                if (targetpath.ShowDialog() == DialogResult.OK)
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    new FileXMLPackage().Package(sourcepath.SelectedPath, targetpath.SelectedPath + "\\");
                    watch.Stop();
                    System.Diagnostics.Debug.WriteLine(watch.ElapsedMilliseconds);
                    MessageBox.Show("package right.");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //new FileTools().LocalDirectoryDelete("D:\\penggev5");
            //new FileXMLPackage().UnPackage("D:\\yugeV5\\", "D:\\penggev5\\");
            //MessageBox.Show("unpackage right.");

            FolderBrowserDialog sourcepath = new FolderBrowserDialog();
            sourcepath.Description = "请选择要解包的文件夹.";
            if (sourcepath.ShowDialog() == DialogResult.OK)
            {
                FolderBrowserDialog targetpath = new FolderBrowserDialog();
                targetpath.Description = "请选择解包后存储的文件夹.";
                if (targetpath.ShowDialog() == DialogResult.OK)
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    new FileXMLPackage().UnPackage(sourcepath.SelectedPath + "\\", targetpath.SelectedPath + "\\");
                    watch.Stop();
                    System.Diagnostics.Debug.WriteLine(watch.ElapsedMilliseconds);
                    MessageBox.Show("unpackage right.");
                }
            }
        }
    }
    
}