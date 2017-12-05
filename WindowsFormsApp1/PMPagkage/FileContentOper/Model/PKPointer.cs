namespace PMPagkage
{
    internal class PKPointer
    {
        /* FB文件写入数量 */
        int FB_Pointer = 0;

        /* BC文件写入数量 */
        int BC_Pointer = 0;

        /* 重置FB文件写入数量 */
        internal void FBPointerReset()
        {
            FB_Pointer = 0;
        }

        /* 重置BC文件写入数量 */
        internal void BCPointerReset()
        {
            BC_Pointer = 0;
        }

        /* 获取当前FB文件已写入的数量 */
        internal int GetFBPointer()
        {
            return this.FB_Pointer;
        }

        /* 获取当前BC文件已写入的数量 */
        internal int GetBCPointer()
        {
            return this.BC_Pointer;
        }

        /* FB文件已写入的数量前进一位 */
        internal int NextFBPointer()
        {
            return this.FB_Pointer++;
        }

        /* BC文件已写入的数量前进{num}位 */
        internal int NextBCPointer(int num)
        {
            this.BC_Pointer += num;
            return this.BC_Pointer;
        }
    }
}