namespace PMPagkage
{
    internal class PKHandler
    {
        /* FB文件写入的序号 */
        private int FB_Index = 0;

        /* BC文件写入的序号 */
        private int BC_Index = 1;

        /* 当前正在写入的BC文件是否是首个BC文件 */
        public bool IsFirstBCIndex
        {
            get { return this.BC_Index == 1; }
        }

        /* FB文件序号前进 */
        public int NextFBIndex()
        {
            return FB_Index++;
        }

        /* BC文件序号前进 */
        public int NextBCIndex()
        {
            return BC_Index++;
        }

        /* 获取当前正在写入的FB文件序号 */
        public int GetFBIndex()
        {
            return FB_Index;
        }

        /* 获取当前正在写入的BC文件序号 */
        public int GetBCIndex()
        {
            return BC_Index;
        }

        /* 获取上一个写入的FB文件序号 */
        public int GetPrevBCIndex()
        {
            return BC_Index - 1;
        }
    }
}