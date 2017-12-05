/*
 * V1.0：支持基本的文件打包和解包功能，对大文件的打包和解包没有优化
 * V1.1：支持大文件的打包和解包
 * V1.2：优化文件读取和写入流
 * V2.0：文件流复用
 * V2.1：支持随机存取
 */
namespace PMPagkage
{
    using LDT.JudgetDoc.Infrastructure.Common.Tools;
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class PKStream
    {
        #region Member

        FileTools filetools = new FileTools();

        /* BC文件的最大写入长度 */
        private readonly int BCL_SIZE = 4096 * 1000;

        private readonly int STREAM_WRITE_BUFFER = 4096;

        /* 目标路径 */
        private string TargetDirectoryPath;

        private readonly byte[] INTBYTES = System.BitConverter.GetBytes(0);
        private readonly byte[] OBytes = new byte[3];
        private readonly byte[] OReadBytes = new byte[4];

        /* FB文件的最大写入目录数 */
        //private readonly int FBL_NUMBER = 200;

        private readonly string FBL_EXT = ".fbl";
        private readonly string BCL_EXT = ".bcl";

        PKStreamOption StreamOption { get; set; }

        /* read option */
        List<FBLLineItem> fbl_items = null;
        private int read_index = 0;

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

        /* 写入内容 */
        public void Write(PKItem item)
        {
            /* 内容为null,不做文件内容的写入操作 */
            if (null == item.Content)
            {
                /* 目录文件写入 */
                this.WriteFB(new string[] { "[BCIndex]", "ItemType=" + item.ItemType, "Name=" + item.Name });
            }
            else
            {
                /* 起始BCL文件 */
                int start_bcl_index = StreamOption.Handler.GetBCIndex();
                string file_bcl_path = this.TargetDirectoryPath + StreamOption.Handler.GetBCIndex() + BCL_EXT;

                /* 创建BCL文件 */
                this.BCIndexFileCreate(file_bcl_path);

                /* 当前BCL文件可写入的起始位置 */
                int start_bcl_pointer = StreamOption.Pointer.GetBCPointer();

                /* 当前BCL文件剩余可写长度 */
                int less_can_write_length = BCL_SIZE - StreamOption.Pointer.GetBCPointer();

                /* 当前BCL文件剩余长度可满足全部写入 */
                if (less_can_write_length > item.Content.Length)
                {
                    using (FileStream stream = new FileStream(file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                    {
                        stream.Position = stream.Length;
                        stream.Write(item.Content, 0, item.Content.Length);

                        StreamOption.Pointer.NextBCPointer(item.Content.Length);
                    }
                }
                /* 当前BCL文件剩余长度不满足全部写入 */
                else if (less_can_write_length < item.Content.Length)
                {
                    int less_must_write_length = 0;

                    /* 当前BCL文件的剩余长度全部写入 */
                    using (FileStream stream = new FileStream(file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                    {
                        stream.Position = stream.Length;
                        stream.Write(item.Content, 0, less_can_write_length);

                        StreamOption.Pointer.BCPointerReset();
                        StreamOption.Handler.NextBCIndex();
                    }

                    /* 还剩余写入的长度 */
                    less_must_write_length = item.Content.Length - less_can_write_length;

                    /* 计算剩余长度全部写入需要的文件数量 */
                    int write_file_length = (less_must_write_length + (BCL_SIZE + (OBytes.Length * 2)) - 1) / (BCL_SIZE + (OBytes.Length * 2));

                    /* 循环写入剩余长度 */
                    for (int j = 0; j < write_file_length; j++)
                    {
                        /* 当前需要写入的BCL文件 */
                        string middle_file_bcl_path = this.TargetDirectoryPath + StreamOption.Handler.GetBCIndex() + BCL_EXT;

                        /* 创建BCL文件 */
                        this.BCIndexFileCreate(middle_file_bcl_path);

                        /* 当前BCL文件的剩余可写入长度 */
                        int middle_less_length = BCL_SIZE - StreamOption.Pointer.GetBCPointer();

                        /* 当前BCL文件剩余长度可满足全部写入 */
                        if (middle_less_length > less_must_write_length)
                        {
                            using (FileStream stream = new FileStream(middle_file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                            {
                                stream.Position = stream.Length;
                                stream.Write(item.Content, item.Content.Length - less_must_write_length, less_must_write_length);

                                StreamOption.Pointer.NextBCPointer(less_must_write_length);
                            }
                        }
                        /* 当前BCL文件剩余长度不满足全部写入 */
                        else if (middle_less_length < less_must_write_length)
                        {
                            using (FileStream stream = new FileStream(middle_file_bcl_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, STREAM_WRITE_BUFFER, false))
                            {
                                stream.Position = stream.Length;
                                stream.Write(item.Content, (item.Content.Length - less_must_write_length), middle_less_length);

                                StreamOption.Pointer.BCPointerReset();
                                StreamOption.Handler.NextBCIndex();

                                /* 重新计算剩余需要写入的长度 */
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

                /* 目录文件写入 */
                this.WriteFB(new string[] { "[BCIndex]", "ItemType=" + item.ItemType, "Name=" + item.Name, "BCLIndex=" + start_bcl_index, "StartIndex=" + start_bcl_pointer, "Length=" + item.Content.Length });
            }
        }

        /* 顺序读取内容 */
        public PKItem Read()
        {
            if((List<FBLLineItem>)null == fbl_items)
            {
                fbl_items = new List<FBLLineItem>();

                this.FBLReadInit();
            }

            if(read_index >= 0 && read_index < fbl_items.Count && fbl_items.Count > 0)
            {
                FBLLineItem read_item = fbl_items[read_index];

                PKItem pkitem = this.LoadFBLineItemContent(read_item);

                read_index++;

                return pkitem;
            }
            else
            {
                return null;
            }
        }

        /* 获取所有目录项 */
        public List<FBLLineItem> GetAllItem()
        {
            if ((List<FBLLineItem>)null == fbl_items)
            {
                fbl_items = new List<FBLLineItem>();

                this.FBLReadInit();
            }

            return fbl_items;
        }

        /* 根据项读取内容 */
        public PKItem Read(FBLLineItem read_item)
        {
            return this.LoadFBLineItemContent(read_item);
        }

        #region Private Methods

        PKItem LoadFBLineItemContent(FBLLineItem read_item)
        {
            if (null == read_item.Length)
            {
                PKItem pkitem = new PKItem();

                pkitem.ItemType = read_item.ItemType;
                pkitem.Name = read_item.Name;
                pkitem.Content = null;

                return pkitem;
            }
            else
            {
                string bcl_path = this.TargetDirectoryPath + read_item.BCLIndex + BCL_EXT;

                if (!filetools.LocalFileExists(bcl_path))
                {
                    return null;
                }
                else
                {
                    string bclindex = read_item.BCLIndex;
                    string startindex = read_item.StartIndex;
                    string length = read_item.Length;

                    int less_length = BCL_SIZE - Int32.Parse(startindex);
                    byte[] filebytes = new byte[Int32.Parse(length)];

                    if (less_length >= Int32.Parse(length))
                    {
                        using (FileStream stream = new FileStream(bcl_path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_WRITE_BUFFER, false))
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

                        using (FileStream stream = new FileStream(bcl_path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_WRITE_BUFFER, false))
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
                            next_bal_path = this.TargetDirectoryPath + System.BitConverter.ToInt64(next_file_index_bytes, 0) + BCL_EXT;
                        }
                        else
                        {
                            next_bal_path = this.TargetDirectoryPath + System.BitConverter.ToInt32(next_file_index_bytes, 0) + BCL_EXT;
                        }

                        //TODO:文件的连续读
                        using (FileStream stream = new FileStream(next_bal_path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_WRITE_BUFFER, false))
                        {
                            if (BCL_SIZE - 6 >= shao_length)
                            {
                                byte[] next_bytes = new byte[shao_length];

                                /* read */
                                stream.Seek(6, SeekOrigin.Begin);
                                stream.Read(next_bytes, 0, shao_length);

                                Buffer.BlockCopy(next_bytes, 0, filebytes, copy_index, shao_length);
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

                    PKItem pkitem = new PKItem();

                    pkitem.ItemType = read_item.ItemType;
                    pkitem.Name = read_item.Name;
                    pkitem.Content = filebytes;

                    return pkitem;
                }
            }
        }

        /* 一次性加载所有FBL目录 */
        void FBLReadInit()
        {
            int fbl_index = 0;

            while (true)
            {
                string fbl_path = this.TargetDirectoryPath + fbl_index + FBL_EXT;

                if (filetools.LocalFileExists(fbl_path))
                {
                    string itemtype = "";
                    string name = "";
                    string bclindex = null;
                    string startindex = null;
                    string length = null;

                    string[] alllines = File.ReadAllLines(fbl_path);

                    for (int line_i = 0; line_i <= alllines.Length; line_i++)
                    {
                        if (line_i == alllines.Length)
                        {
                            this.fbl_items.Add(new FBLLineItem { Name = name, ItemType = itemtype, StartIndex = startindex, BCLIndex = bclindex, Length = length });
                        }
                        else if (line_i != 0)
                        {
                            string line = alllines[line_i];

                            if ("[BCIndex]" == line)
                            {
                                this.fbl_items.Add(new FBLLineItem { Name = name, ItemType = itemtype, StartIndex = startindex, BCLIndex = bclindex, Length = length });

                                itemtype = "";
                                name = "";
                                bclindex = null;
                                startindex = null;
                                length = null;
                            }
                            else
                            {
                                if (line.StartsWith("ItemType="))
                                {
                                    itemtype = line.Substring(9, line.Length - 9);
                                }
                                else if (line.StartsWith("Name="))
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

        /* 新建BCL文件 */
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

        /* 目录文件写入 */
        void WriteFB(string[] content)
        {
            string dir_fbl_path = this.TargetDirectoryPath + StreamOption.Handler.GetFBIndex() + FBL_EXT;

            filetools.LocalFileCreate(dir_fbl_path, false);

            //TODO:stream优化
            File.AppendAllLines(dir_fbl_path, content);

            StreamOption.Pointer.NextFBPointer();
        }

        #endregion
    }
}