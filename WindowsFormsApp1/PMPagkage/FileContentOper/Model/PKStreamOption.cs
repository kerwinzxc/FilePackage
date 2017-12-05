namespace PMPagkage
{
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

        /* FB文件已写入的数量前进1位 */
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
}