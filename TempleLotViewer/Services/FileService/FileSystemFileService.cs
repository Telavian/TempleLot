using TempleLotViewer.Services.FileService.Interfaces;

namespace TempleLotViewer.Services.FileService
{
    public class FileSystemFileService : IFileService
    {
        private readonly string _rootPath;
        public string DataRootDirectory => _rootPath;

        public FileSystemFileService(string rootPath)
        {
            if (rootPath.EndsWith("/") == false)
            {
                rootPath += "/";
            }

            _rootPath = rootPath;
        }

        public Task<byte[]> LoadDataAsync(string path)
        {
            path = path.Replace("./", _rootPath);
            return File.ReadAllBytesAsync(path);
        }
    }
}
