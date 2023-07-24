using System.Collections.Concurrent;
using System.Reflection;
using TempleLotViewer.Extensions;
using TempleLotViewer.Services.FileService.Interfaces;

namespace TempleLotViewer.Services
{
    public class EmbeddedResourceFileService : IFileService
    {
        private static readonly ConcurrentDictionary<string, byte[]> _dataLookup = new ConcurrentDictionary<string, byte[]>();
        public string DataRootDirectory => "";

        public async Task<byte[]> LoadDataAsync(string path)
        {
            var isFound = _dataLookup.TryGetValue(path, out var numArray);

            if (isFound && numArray != null)
            {
                return numArray;
            }

            var str = path;
            if (str.StartsWith("./"))
            {
                str = str.Substring(2);
            }

            var name = "TempleLotViewer.wwwroot." + str.Replace('/', '.').Replace('\\', '.');
            using (var manifestResourceStream = LoadResourceStream(name))
            {
                if (manifestResourceStream == null)
                    throw new Exception("Unable to find resource '" + path + "'");

                await using (manifestResourceStream)
                {
                    using (MemoryStream memStream = new MemoryStream())
                    {
                        await manifestResourceStream.CopyToAsync(memStream);
                        byte[] array = memStream.ToArray();
                        _dataLookup.TryAdd(path, array);
                        return array;
                    }
                }
            }
        }

        private Stream? LoadResourceStream(string name)
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

                if (stream == null) throw new Exception($"Unknown resource '{name}'");
                return stream;
            }
            catch (Exception ex)
            {
                var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                    .Select(x => $"'{x}'")
                    .StringJoin(", ");
                throw new Exception($"Unable to load resource '{name}'. Known resources: {resources}", ex);
            }
        }
    }
}
