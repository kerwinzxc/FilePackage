using LDT.JudgetDoc.Infrastructure.Common.Tools;
using System;
using System.IO;

namespace PMPagkage
{
    public class FileXMLPackage : IPackageAble
    {
        FileTools filetools = new FileTools();

        private readonly bool USE_STREAM_ASYNC = false;

        public FileXMLPackage()
        {
        }

        public FileXMLPackage(bool useAsync) : this()
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
                /* directory path */
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
                /* directory path end */

                PKStream pk_stream = new PKStream(sourceDirectoryPath);

                this.LoopUnPackage(targetDirectoryPath, pk_stream);
            }
            else
            {
                throw new Exception("路径不是一个正确的目录.");
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

        private void LoopUnPackage(string targetDirectoryPath, PKStream pk_stream)
        {
            PKItem pkitem = pk_stream.Read();

            while ((PKItem)null != pkitem)
            {
                if(pkitem.ItemType == "[Dirctory]")
                {
                    filetools.LocalDirectoryCreate(targetDirectoryPath + pkitem.Name);
                }
                else if(pkitem.ItemType == "[File]")
                {
                    filetools.LocalFileCreate(targetDirectoryPath + pkitem.Name, pkitem.Content, true);
                }

                pkitem = pk_stream.Read();
            }
        }

        #endregion
    }
}
