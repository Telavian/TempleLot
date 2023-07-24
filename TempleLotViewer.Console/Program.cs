using TempleLotViewer.Services.FileService;
using TempleLotViewer.Services.WitnessSearch;

namespace TempleLotViewer.Console
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Task.Yield();
            System.Console.WriteLine("Continue?");
            System.Console.ReadLine();

            //var processor = new NodeProcessor();
            //await processor.ProcessAllFilesAsync(@"G:\UncorrelatedMormonism\TempleLotViewer\TempleLotViewer\wwwroot\Witnesses");

            //var indexer = new SearchIndexer();
            //await indexer.IndexAllWitnessesAsync();

            //var fileService = new FileSystemFileService(@"G:\UncorrelatedMormonism\TempleLotViewer\TempleLotViewer\wwwroot");
            //var search = new WitnessSearchService(fileService, () => Task.CompletedTask);
            //await search.InitializeAsync();

            //var results = await search.FindPhraseMatchesAsync("Joseph Smith");
            //results = await search.FindExactMatchesAsync("Joseph Smith");
        }
    }
}
