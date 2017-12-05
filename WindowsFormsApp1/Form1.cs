namespace WindowsFormsApp1
{
    using LDT.JudgetDoc.Infrastructure.Common.Tools;
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

    public interface PackageAble
    {
        /* 打包 */
        void Package(string sourcePath, string targetPath);

        /* 解包 */
        void UnPackage(string sourcePath, string targetPath);
    }

    public class FileXMLPackage : PackageAble
    {
        FileTools filetools = new FileTools();

        
        private readonly int BCL_SIZE = 4096 * 1000;

        private readonly string FBL_EXT = ".fbl";
        private readonly string BCL_EXT = ".bcl";

        private readonly int STREAM_WRITE_BUFFER = 4096;
        private readonly bool USE_STREAM_ASYNC = false;

        private readonly byte[] INTBYTES = System.BitConverter.GetBytes(0);
        private readonly byte[] OBytes = new byte[3];

        private readonly byte[] OReadBytes = new byte[4];

        public FileXMLPackage()
        {
        }

        public FileXMLPackage(bool useAsync):this()
        {
            USE_STREAM_ASYNC = useAsync;
        }

        public void Package(string sourceDirectoryPath, string targetDirectoryPath)
        {
            if ((File.GetAttributes(sourceDirectoryPath) & FileAttributes.Directory) == FileAttributes.Directory && (File.GetAttributes(targetDirectoryPath) & FileAttributes.Directory) == FileAttributes.Directory)
            {

                /* directory path */
                sourceDirectoryPath = new DirectoryInfo(sourceDirectoryPath).FullName;
                if (sourceDirectoryPath.EndsWith("\\"))
                {
                    sourceDirectoryPath = sourceDirectoryPath.Substring(0, sourceDirectoryPath.Length - 1);
                }

                targetDirectoryPath = new DirectoryInfo(targetDirectoryPath).FullName;
                if (!targetDirectoryPath.EndsWith("\\"))
                {
                    targetDirectoryPath += "\\";
                }
                /* directory path end */

                filetools.LocalDirectoryCreate(targetDirectoryPath);

                //TODO:验证文件夹中内容是否为空.
                //TODO:如果有内容，下一版支持文件续写.

                filetools.LocalDirectoryRemoveReadOnlyAttribute(sourceDirectoryPath);

                PKStream pk_stream = new PKStream(targetDirectoryPath);

                string source_root_directory_path = sourceDirectoryPath.Replace(new DirectoryInfo(sourceDirectoryPath).Name, "");

                this.LoopPackage(source_root_directory_path, sourceDirectoryPath, pk_stream);
            }
            else
            {
                throw new Exception("路径不是一个正确的目录.");
            }
        }

        public void UnPackage(string sourceDirectoryPath, string targetDirectoryPath)
        {
            if ((File.GetAttributes(sourceDirectoryPath) & FileAttributes.Directory) == FileAttributes.Directory && (File.GetAttributes(targetDirectoryPath) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                /* directory */
                sourceDirectoryPath = new DirectoryInfo(sourceDirectoryPath).FullName;
                if (!sourceDirectoryPath.EndsWith("\\"))
                {
                    sourceDirectoryPath += "\\";
                }

                targetDirectoryPath = new DirectoryInfo(targetDirectoryPath).FullName;
                if (!targetDirectoryPath.EndsWith("\\"))
                {
                    targetDirectoryPath += "\\";
                }

                this.LoopUnPackage(sourceDirectoryPath, targetDirectoryPath);
            }
            else
            {
                throw new Exception("路径不是一个正确的目录.");
            }
        }

        internal class PKStream
        {
            #region Member

            FileTools filetools = new FileTools();

            private readonly int BCL_SIZE = 4096 * 1000;

            private readonly int STREAM_WRITE_BUFFER = 4096;

            private string TargetDirectoryPath;

            private readonly byte[] INTBYTES = System.BitConverter.GetBytes(0);
            private readonly byte[] OBytes = new byte[3];
            private readonly byte[] OReadBytes = new byte[4];

            private readonly int FBL_NUMBER = 200;

            private readonly string FBL_EXT = ".fbl";
            private readonly string BCL_EXT = ".bcl";

            PKStreamOption StreamOption { get; set; }

            #endregion

            #region Ctor

            public PKStream(string targetDirectoryPath)
            {
                this.TargetDirectoryPath = targetDirectoryPath;

                Buffer.BlockCopy(INTBYTES, 0, OBytes, 0, OBytes.Length);
                Buffer.BlockCopy(INTBYTES, 0, OReadBytes, 0, OReadBytes.Length);

                StreamOption = new PKStreamOption();
            }

            #endregion

            public void Write(PKItem item)
            {
                if (null == item.Content)
                {
                    this.WriteFB(new string[] { item.ItemType, "Name=" + item.Name });
                }
                else
                {
                    /* 起始文件 */
                    int start_bcl_index = StreamOption.Handler.GetBCIndex();
                    string file_bcl_path = this.TargetDirectoryPath + StreamOption.Handler.GetBCIndex() + BCL_EXT;

                    /* 创建文件 */
                    this.BCIndexFileCreate(file_bcl_path);

                    /* 起始位置 */
                    int start_bcl_pointer = StreamOption.Pointer.GetBCPointer();

                    /* 文件剩余可写长度 */
                    int less_can_write_length = BCL_SIZE - StreamOption.Pointer.GetBCPointer();

                    if (less_can_write_length > item.Content.Length)
                    {
                        using (FileStream stream = new FileStream(file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                        {
                            stream.Position = stream.Length;
                            stream.Write(item.Content, 0, item.Content.Length);

                            StreamOption.Pointer.NextBCPointer(item.Content.Length);
                        }
                    }
                    else if (less_can_write_length < item.Content.Length)
                    {
                        int less_must_write_length = 0;

                        /* first file */
                        using (FileStream stream = new FileStream(file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                        {
                            stream.Position = stream.Length;
                            stream.Write(item.Content, 0, less_can_write_length);

                            StreamOption.Pointer.BCPointerReset();
                            StreamOption.Handler.NextBCIndex();
                        }

                        less_must_write_length = item.Content.Length - less_can_write_length;

                        int write_file_length = (less_must_write_length + (BCL_SIZE + (OBytes.Length * 2)) - 1) / (BCL_SIZE + (OBytes.Length * 2));

                        for (int j = 0; j < write_file_length; j++)
                        {
                            string middle_file_bcl_path = this.TargetDirectoryPath + StreamOption.Handler.GetBCIndex() + BCL_EXT;

                            this.BCIndexFileCreate(middle_file_bcl_path);

                            int middle_less_length = BCL_SIZE - StreamOption.Pointer.GetBCPointer();

                            if (middle_less_length > less_must_write_length)
                            {
                                using (FileStream stream = new FileStream(middle_file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                                {
                                    stream.Position = stream.Length;
                                    stream.Write(item.Content, item.Content.Length - less_must_write_length, less_must_write_length);

                                    StreamOption.Pointer.NextBCPointer(less_must_write_length);
                                }
                            }
                            else if (middle_less_length < less_must_write_length)
                            {
                                using (FileStream stream = new FileStream(middle_file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                                {
                                    stream.Position = stream.Length;
                                    stream.Write(item.Content, (item.Content.Length - less_must_write_length), middle_less_length);

                                    StreamOption.Pointer.BCPointerReset();
                                    StreamOption.Handler.NextBCIndex();

                                    less_must_write_length -= middle_less_length;
                                }
                            }
                            else/* middle_less_length == less_must_write_length */
                            {
                                using (FileStream stream = new FileStream(middle_file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                                {
                                    stream.Position = stream.Length;
                                    stream.Write(item.Content, item.Content.Length - less_must_write_length, less_must_write_length);

                                    StreamOption.Pointer.BCPointerReset();
                                    StreamOption.Handler.NextBCIndex();
                                }
                            }
                        }
                    }
                    else/* less_length == filelength */
                    {
                        using (FileStream stream = new FileStream(file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                        {
                            stream.Position = stream.Length;
                            stream.Write(item.Content, 0, item.Content.Length);

                            StreamOption.Pointer.BCPointerReset();
                            StreamOption.Handler.NextBCIndex();
                        }
                    }

                    this.WriteFB(new string[] { item.ItemType, "Name=" + item.Name, "BCLIndex=" + start_bcl_index, "StartIndex=" + start_bcl_pointer, "Length=" + item.Content.Length });
                }
            }

            public PKItem Read()
            {
                return null;
            }

            void BCIndexFileCreate(string file_bcl_path)
            {
                if (!filetools.LocalFileExists(file_bcl_path))
                {
                    /* 判断是否是首个文件 */
                    if (StreamOption.Handler.IsFirstBCIndex)
                    {
                        using (FileStream stream = new FileStream(file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                        {
                            stream.Write(OBytes, 0, OBytes.Length);
                            stream.Write(OBytes, 0, OBytes.Length);

                            StreamOption.Pointer.NextBCPointer(OBytes.Length * 2);
                        }
                    }
                    else
                    {
                        /* 文件头 */
                        using (FileStream stream = new FileStream(file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                        {
                            byte[] per_index_obytes = System.BitConverter.GetBytes(StreamOption.Handler.GetPrevBCIndex());
                            byte[] per_index_bytes = new byte[OBytes.Length];

                            Buffer.BlockCopy(per_index_obytes, 0, per_index_bytes, 0, per_index_bytes.Length);

                            stream.Write(per_index_bytes, 0, per_index_bytes.Length);
                            stream.Write(OBytes, 0, OBytes.Length);
                            StreamOption.Pointer.NextBCPointer(per_index_bytes.Length + OBytes.Length);
                        }

                        string per_bcl_path = this.TargetDirectoryPath + StreamOption.Handler.GetPrevBCIndex() + BCL_EXT;

                        /* 改写上个文件中的next文件名称 */
                        using (FileStream stream = new FileStream(per_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                        {
                            byte[] next_index_obytes = System.BitConverter.GetBytes(StreamOption.Handler.GetBCIndex());
                            byte[] next_index_bytes = new byte[OBytes.Length];

                            Buffer.BlockCopy(next_index_obytes, 0, next_index_bytes, 0, next_index_bytes.Length);

                            stream.Seek(OBytes.Length, SeekOrigin.Begin);
                            stream.Write(next_index_bytes, 0, next_index_bytes.Length);
                        }
                    }
                }
            }

            void WriteFB(string[] content)
            {
                string dir_fbl_path = this.TargetDirectoryPath + StreamOption.Handler.GetFBIndex() + FBL_EXT;

                filetools.LocalFileCreate(dir_fbl_path, false);

                //TODO:stream优化
                File.AppendAllLines(dir_fbl_path, content);

                StreamOption.Pointer.NextFBPointer();
            }
        }

        internal class PKItem
        {
            internal string ItemType { get; set; }

            internal string Name { get; set; }

            internal byte[] Content { get; set; }
        }

        internal class PKStreamOption
        {
            private readonly int FBL_NUMBER = 200;

            internal PKHandler Handler { get; set; }
            internal PKPointer Pointer { get; set; }

            public PKStreamOption()
            {
                Handler = new PKHandler();
                Pointer = new PKPointer();
            }

            public void NextFBPointer()
            {
                Pointer.NextFBPointer();

                if (Pointer.GetFBPointer() == FBL_NUMBER)
                {
                    Handler.NextFBIndex();
                    Pointer.FBPointerReset();
                }
            }
        }

        internal class PKHandler
        {
            private int FB_Index = 0;
            private int BC_Index = 1;

            public bool IsFirstBCIndex
            {
                get { return this.BC_Index == 1; }
            }

            public int NextFBIndex()
            {
                return FB_Index++;
            }

            public int NextBCIndex()
            {
                return BC_Index++;
            }

            public int GetFBIndex()
            {
                return FB_Index;
            }

            public int GetBCIndex()
            {
                return BC_Index;
            }

            public int GetPrevBCIndex()
            {
                return BC_Index - 1;
            }
        }

        internal class PKPointer
        {
            int FB_Pointer = 0;
            int BC_Pointer = 0;

            internal void FBPointerReset()
            {
                FB_Pointer = 0;
            }

            internal void BCPointerReset()
            {
                BC_Pointer = 0;
            }

            internal int GetFBPointer()
            {
                return this.FB_Pointer;
            }

            internal int GetBCPointer()
            {
                return this.BC_Pointer;
            }

            internal int NextFBPointer()
            {
                return this.FB_Pointer++;
            }

            internal int NextBCPointer(int num)
            {
                this.BC_Pointer += num;
                return this.BC_Pointer;
            }
        }

        #region Private Methods

        private void LoopPackage(string sourceRootDirectoryPath, string sourceDirectoryPath, PKStream pk_stream)
        {
            DirectoryInfo source_direcoty = new DirectoryInfo(sourceDirectoryPath);

            string directory_itemtype = "[Dirctory]";
            string directory_name = source_direcoty.FullName.Replace(sourceRootDirectoryPath, "");

            pk_stream.Write(new PKItem { ItemType = directory_itemtype, Name = directory_name });

            /* file package */
            FileInfo[] fileinfos = source_direcoty.GetFiles();
            for (int i = 0; i < fileinfos.Length; i++)
            {
                List<byte> allwritebytes = new List<byte>();

                FileInfo fileinfo = fileinfos[i];

                byte[] filebytes = File.ReadAllBytes(fileinfo.FullName);

                string file_itemtype = "[File]";
                string file_name = fileinfo.FullName.Replace(sourceRootDirectoryPath, "");

                pk_stream.Write(new PKItem { ItemType = file_itemtype, Name = file_name, Content = filebytes });
            }

            /* directory */
            DirectoryInfo[] directorys = source_direcoty.GetDirectories();
            for (int k = 0; k < directorys.Length; k++)
            {
                DirectoryInfo directory = directorys[k];

                LoopPackage(sourceRootDirectoryPath, directory.FullName, pk_stream);
            }

        }

        private void LoopUnPackage(string sourceRootDirectoryPath, string targetDirectoryPath)
        {
            int fbl_index = 0;

            while (true)
            {
                string fbl_path = sourceRootDirectoryPath + fbl_index + FBL_EXT;

                if (filetools.LocalFileExists(fbl_path))
                {
                    int analyze_model = 0;
                    string name = "";
                    string bclindex = "";
                    string startindex = "";
                    string length = "";

                    string[] alllines = File.ReadAllLines(fbl_path);

                    for (int line_i = 0; line_i <= alllines.Length; line_i++)
                    {
                        if (line_i == alllines.Length)
                        {
                            this.FileLoopUnPackage(sourceRootDirectoryPath, targetDirectoryPath, analyze_model, name, bclindex, startindex, length);
                        }
                        else
                        {
                            string line = alllines[line_i];

                            if ("[Dirctory]" == line)
                            {
                                this.FileLoopUnPackage(sourceRootDirectoryPath, targetDirectoryPath, analyze_model, name, bclindex, startindex, length);

                                analyze_model = 1;
                                name = "";
                                bclindex = "";
                                startindex = "";
                                length = "";
                            }
                            else if ("[File]" == line)
                            {
                                this.FileLoopUnPackage(sourceRootDirectoryPath, targetDirectoryPath, analyze_model, name, bclindex, startindex, length);

                                analyze_model = 2;
                                name = "";
                                bclindex = "";
                                startindex = "";
                                length = "";
                            }
                            else
                            {
                                if (line.StartsWith("Name="))
                                {
                                    name = line.Substring(5, line.Length - 5);
                                }
                                else if (line.StartsWith("BCLIndex="))
                                {
                                    bclindex = line.Substring(9, line.Length - 9);
                                }
                                else if (line.StartsWith("StartIndex="))
                                {
                                    startindex = line.Substring(11, line.Length - 11);
                                }
                                else if (line.StartsWith("Length="))
                                {
                                    length = line.Substring(7, line.Length - 7);
                                }
                            }
                        }
                    }
                }
                else
                {
                    break;
                }

                fbl_index++;
            }
        }

        private void FileLoopUnPackage(string sourceRootDirectoryPath, string targetDirectoryPath, int analyze_model, string name, string bclindex, string startindex, string length)
        {
            if (1 == analyze_model)
            {
                filetools.LocalDirectoryCreate(targetDirectoryPath + name);
            }
            else if (2 == analyze_model)
            {
                string bcl_path = sourceRootDirectoryPath + bclindex + BCL_EXT;

                if (!filetools.LocalFileExists(bcl_path))
                {

                }
                else
                {
                    int less_length = BCL_SIZE - Int32.Parse(startindex);
                    byte[] filebytes = new byte[Int32.Parse(length)];

                    if (less_length >= Int32.Parse(length))
                    {
                        using (FileStream stream = new FileStream(bcl_path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_WRITE_BUFFER, USE_STREAM_ASYNC))
                        {
                            stream.Seek(Int32.Parse(startindex), SeekOrigin.Begin);
                            stream.Read(filebytes, 0, Int32.Parse(length));
                        }
                    }
                    else
                    {
                        byte[] headbytes = new byte[less_length];
                        byte[] next_file_index_bytes_read = new byte[3];
                        int copy_index = 0;

                        using (FileStream stream = new FileStream(bcl_path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_WRITE_BUFFER, USE_STREAM_ASYNC))
                        {
                            stream.Seek(3, SeekOrigin.Begin);
                            stream.Read(next_file_index_bytes_read, 0, 3);

                            /* read */
                            stream.Seek(Int32.Parse(startindex), SeekOrigin.Begin);
                            stream.Read(headbytes, 0, less_length);

                            Buffer.BlockCopy(headbytes, 0, filebytes, 0, less_length);
                            copy_index += less_length;
                        }

                        int shao_length = Int32.Parse(length) - less_length;

                        byte[] next_file_index_bytes = null;
                        string next_bal_path = "";

                        DI_GUI_SHI_SHA:
                        if (sizeof(int) == 8)
                        {
                            next_file_index_bytes = new byte[8];
                        }
                        else
                        {
                            next_file_index_bytes = new byte[4];
                        }

                        Buffer.BlockCopy(next_file_index_bytes_read, 0, next_file_index_bytes, 0, 3);

                        if (sizeof(int) == 8)
                        {
                            next_bal_path = sourceRootDirectoryPath + System.BitConverter.ToInt64(next_file_index_bytes, 0) + BCL_EXT;
                        }
                        else
                        {
                            next_bal_path = sourceRootDirectoryPath + System.BitConverter.ToInt32(next_file_index_bytes, 0) + BCL_EXT;
                        }

                        //TODO:文件的连续读
                        using (FileStream stream = new FileStream(next_bal_path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_WRITE_BUFFER, USE_STREAM_ASYNC))
                        {
                            if (BCL_SIZE - 6 >= shao_length)
                            {
                                byte[] next_bytes = new byte[shao_length];

                                /* read */
                                stream.Seek(6, SeekOrigin.Begin);
                                stream.Read(next_bytes, 0, shao_length);

                                Buffer.BlockCopy(next_bytes, 0, filebytes, copy_index, shao_length);
                                //copy_index += less_length;
                            }
                            else
                            {
                                byte[] next_bytes = new byte[BCL_SIZE - 6];

                                stream.Seek(3, SeekOrigin.Begin);
                                stream.Read(next_file_index_bytes_read, 0, 3);

                                /* read */
                                stream.Read(next_bytes, 0, BCL_SIZE - 6);

                                Buffer.BlockCopy(next_bytes, 0, filebytes, copy_index, BCL_SIZE - 6);
                                copy_index += (BCL_SIZE - 6);

                                shao_length -= (BCL_SIZE - 6);

                                goto DI_GUI_SHI_SHA;
                            }
                        }
                    }

                    string file_path = targetDirectoryPath + name;
                    filetools.LocalFileCreate(file_path, filebytes, true);
                }
            }
        }

        #endregion
    }
}