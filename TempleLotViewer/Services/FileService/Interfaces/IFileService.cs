namespace TempleLotViewer.Services.FileService.Interfaces
{
    public interface IFileService
    {
        public string DataRootDirectory { get; }
        public Task<byte[]> LoadDataAsync(string path);
    }
}
