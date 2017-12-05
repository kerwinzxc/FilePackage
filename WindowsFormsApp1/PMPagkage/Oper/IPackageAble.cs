namespace PMPagkage
{
    public interface IPackageAble
    {
        /* 打包 */
        void Package(string sourcePath, string targetPath);

        /* 解包 */
        void UnPackage(string sourcePath, string targetPath);
    }
}