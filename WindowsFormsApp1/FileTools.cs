namespace LDT.JudgetDoc.Infrastructure.Common.Tools
{
    #region Using

    using System;
    using System.IO;

    #endregion

    /// <summary>
    /// 本类非线程安全
    /// </summary>
    public class FileTools
    {
        private void FileDirectoryCreate(string localfilepath)
        {
            FileInfo fileinfo = new FileInfo(localfilepath);

            this.LocalDirectoryCreate(fileinfo.DirectoryName);
        }

        public bool LocalFileExists(string localPath)
        {
            return File.Exists(localPath);
        }

        public bool LocalFileDelete(string localPath)
        {
            if (!this.LocalFileExists(localPath))
            {
                return false;
            }

            bool result = false;

            try
            {
                File.Delete(localPath);
                result = true;
            }
            catch (Exception)
            {
                // ignored
            }

            return result;
        }

        public bool UriFileExists(string uri)
        {
            return false;
        }

        /// <summary>
        /// 本地文件创建
        /// </summary>
        /// <param name="localfilepath">文件全路径</param>
        /// <param name="datas">文件内容</param>
        /// <param name="isDelete">是否删除已存在文件</param>
        public void LocalFileCreate(string localfilepath, byte[] datas, bool isDelete)
        {
            if (isDelete)
            {
                this.LocalFileDelete(localfilepath);
            }

            this.FileDirectoryCreate(localfilepath);

            using (FileStream filestream = File.Create(localfilepath))
            {
                filestream.Write(datas, 0, datas.Length);
            }
        }

        public bool LocalFileCreate(string localPath, bool isDelete)
        {
            bool iscreate = false;

            if (isDelete)
            {
                this.LocalFileDelete(localPath);
            }

            if (this.LocalFileExists(localPath))
            {
                return false;
            }
            else
            {
                this.FileDirectoryCreate(localPath);

                File.Create(localPath).Dispose();
                iscreate = true;
            }

            return iscreate;
        }

        public bool LocalFileCreate(string localPath, string content, bool isDelete)
        {
            bool iscreate = false;

            if (isDelete)
            {
                this.LocalFileDelete(localPath);
            }

            if (this.LocalFileExists(localPath))
            {
                return false;
            }
            else
            {
                File.AppendAllText(localPath, content);
                iscreate = true;
            }

            return iscreate;
        }

        private bool UriFileCreate(string uriPath)
        {
            return false;
        }

        public bool LocalFileCopy(string sourceFilePath, string destFiePath)
        {
            bool result = false;
            if (this.LocalFileExists(sourceFilePath))
            {
                if (this.LocalFileExists(destFiePath))
                {
                    this.LocalFileDelete(destFiePath);
                }
                
                //TODO:check file isinuse
                try
                {
                    File.Copy(sourceFilePath, destFiePath);
                    result = true;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            else
            {
                return false;
            }

            return result;
        }

        public void LocalFileReName(string sourceFilePath, string destFiePath)
        {
            if (this.LocalFileCopy(sourceFilePath, destFiePath))
            {
                this.LocalFileDelete(sourceFilePath);
            }
        }

        /*Dictory*/

        public bool LocalDirectoryExists(string localdirectorypath)
        {
            return Directory.Exists(localdirectorypath);
        }

        public bool LocalDirectoryDelete(string localdirectorypath)
        {
            if (!this.LocalDirectoryExists(localdirectorypath))
            {
                return false;
            }

            bool result = false;

            try
            {
                Directory.Delete(localdirectorypath, true);
                result = true;
            }
            catch (Exception ex)
            {
                // ignored
            }

            return result;
        }

        public bool LocalDirectoryMove(string sourcedirectorypath, string destdirectorypath)
        {
            bool result = false;
            if (this.LocalDirectoryExists(sourcedirectorypath))
            {
                if (this.LocalDirectoryExists(sourcedirectorypath))
                {
                    this.LocalDirectoryDelete(destdirectorypath);
                }

                try
                {
                    Directory.Move(sourcedirectorypath, destdirectorypath);
                    result = true;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            else
            {
                return false;
            }

            return result;
        }

        public void LocalDirectoryCreate(string localdirectorypath)
        {
            if (!this.LocalDirectoryExists(localdirectorypath))
            {
                Directory.CreateDirectory(localdirectorypath);
            }
        }

        public void LocalDirectoryRemoveReadOnlyAttribute(string localdirectorypath)
        {
            System.IO.DirectoryInfo dirinfo = new DirectoryInfo(localdirectorypath);

            dirinfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;

            System.IO.FileInfo[] childfiles = dirinfo.GetFiles();

            for (int i = 0; i < childfiles.Length; i++)
            {
                System.IO.FileInfo currentfile = childfiles[i];

                currentfile.Attributes = FileAttributes.Normal & FileAttributes.Archive;
            }

            System.IO.DirectoryInfo[] childdirs = dirinfo.GetDirectories();

            for (int i = 0; i < childdirs.Length; i++)
            {
                System.IO.DirectoryInfo currentdir = childdirs[i];

                this.LocalDirectoryRemoveReadOnlyAttribute(currentdir.FullName);
            }

        }
    }
}