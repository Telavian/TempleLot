using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using TempleLotViewer.Services.FileService.Interfaces;
using System.Diagnostics;
using TempleLotViewer.Enums;
using TempleLotViewer.Services.WitnessSearch.Models;
using System.IO.Compression;
using TempleLotViewer.Extensions;
using Directory = System.IO.Directory;

namespace TempleLotViewer.Services.WitnessSearch
{
    public class WitnessSearchService
    {
        private const string _indexName = "W-Index";
        private const string _indexPath = "./Witnesses/FullIndex.bin";

        private static readonly LuceneVersion _luceneVersion = LuceneVersion.LUCENE_48;
        private static FSDirectory? _searchDirectory;
        private static readonly Analyzer _analyzer = new StandardAnalyzer(_luceneVersion);
        private static DirectoryReader? _reader;
        private static IndexSearcher? _searcher;
        private readonly IFileService _fileService;
        private readonly Func<Task> _refresh;
        private bool _isInitialized;
        private readonly int _totalResults;

        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _searchLock = new SemaphoreSlim(1);

        private static readonly HashSet<string> _stopWords = StandardAnalyzer.STOP_WORDS_SET
            .Select(x => x.ToLower())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        public WitnessSearchService(IFileService fileService, Func<Task> refresh, int totalResults = 5000)
        {
            _fileService = fileService;
            _refresh = refresh;
            _totalResults = totalResults;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                await _initializationLock.WaitAsync();

                if (_isInitialized)
                    return;

                LogMessage("Starting search initialization");
                var timer = Stopwatch.StartNew();

                await InitializeSearchIndexAsync();

                LogMessage($"Search initialization complete: {timer.ElapsedMilliseconds} ms");
                _isInitialized = true;
                await RefreshAsync();
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public Task<SearchMatch[]> FindExactMatchesAsync(string text)
        {
            var lower = text;
            if (string.IsNullOrWhiteSpace(lower))
            {
                return Task.FromResult(Array.Empty<SearchMatch>());
            }

            lower = lower.ToLower();

            return ExecuteSearchAsync(SearchMode.Exact, () =>
            {
                lower = lower.Trim('\"');
                var phraseQueries = new PhraseQuery();
                var num = 0;
                var strArrays = lower.Split(Array.Empty<char>());

                foreach (var str in strArrays)
                {
                    if (_stopWords.Contains(str) == false)
                    {
                        phraseQueries.Add(new Term("Text", str), num);
                    }
                    num++;
                }

                var topDoc = _searcher!.Search(phraseQueries, _totalResults);
                var data = lower.Trim().Trim('\"');

                return (topDoc, data, data.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
            });
        }

        public Task<SearchMatch[]> FindPhraseMatchesAsync(string text)
        {
            var lower = text;
            if (string.IsNullOrWhiteSpace(lower))
            {
                return Task.FromResult(Array.Empty<SearchMatch>());
            }

            lower = lower.ToLower();

            return ExecuteSearchAsync(SearchMode.Phrase, () =>
            {
                var booleanQueries = new BooleanQuery();
                var strArrays = lower.Split(Array.Empty<char>());

                foreach (var str in strArrays)
                {
                    if (!_stopWords.Contains(str))
                    {
                        booleanQueries.Add(new TermQuery(new Term("Text", str)), Occur.MUST);
                    }
                }

                var topDoc = _searcher!.Search(booleanQueries, _totalResults);
                var chunks = lower.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

                return new ValueTuple<TopDocs, string, string[]>(topDoc, ConvertToRegexWildcardSearch(chunks), chunks);
            });
        }

        private async Task InitializeSearchIndexAsync()
        {
            var timer = Stopwatch.StartNew();
            var indexDirectory = Path.Combine(_fileService.DataRootDirectory, _indexName);
            var data = await _fileService.LoadDataAsync(_indexPath);

            LogMessage($"Data load: {timer.ElapsedMilliseconds}");
            timer.Restart();

            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                memoryStream.Position = 0L;
                await ProcessIndexStreamAsync(memoryStream);
            }

            LogMessage($"Process index: {timer.ElapsedMilliseconds}");
            timer.Restart();

            try
            {
                Console.WriteLine($"Loading index '{indexDirectory}'");
                await Task.Yield();
                _searchDirectory = FSDirectory.Open(indexDirectory, new SingleInstanceLockFactory());
                _reader = DirectoryReader.Open(_searchDirectory);
                _searcher = new IndexSearcher(_reader);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to build search index: {ex.Message}", ex);
            }

            LogMessage($"Load index: {timer.ElapsedMilliseconds}");
            timer.Restart();
        }

        private async Task ProcessIndexStreamAsync(Stream indexStream)
        {
            await Task.Yield();
            var location = Path.Combine(_fileService.DataRootDirectory, _indexName);
            var zipArchive = new ZipArchive(indexStream);

            Console.WriteLine($"Expanding index to folder '{location}'");
            if (Directory.Exists(location))
            {
                Directory.Delete(location, true);
            }

            zipArchive.ExtractToDirectory(location, true);
            Console.WriteLine("Expansion complete");
        }

        private async Task<SearchMatch[]> ProcessSearchMatchesAsync(TopDocs matches, string regex, SearchInfo search)
        {
            var searchMatches = matches.ScoreDocs
                .Select<ScoreDoc, SearchMatch?>(match =>
                {
                    var documents = _reader!.Document(match.Doc);
                    var field = documents.GetField(nameof(SearchItem.Text));
                    var stringValue = field?.GetStringValue() ?? "";

                    var text = ((string)stringValue)
                        .Trim()
                        .Replace("\n", " ")
                        .Replace("  ", " ");

                    var formatted = BuildFormattedMatchText(text, search);
                    var xpath = documents.GetField(nameof(SearchItem.XPath)).GetStringValue();
                    var witness = documents.GetField(nameof(SearchItem.Witness)).GetStringValue();
                    var witnessNumber = documents.GetField(nameof(SearchItem.WitnessNumber)).GetInt32Value();
                    var question = documents.GetField(nameof(SearchItem.Question)).GetInt32Value();

                    return new SearchMatch
                    {
                        Mode = search.Mode,
                        Witness = witness,
                        WitnessNumber = witnessNumber ?? 0,
                        Question = question ?? 0,
                        Text = text.LimitTo(100),
                        FormattedText = formatted.LimitTo(100),
                        Keywords = search.SearchKeywords,
                        Score = match.Score,
                        XPath = xpath
                    };
                });

            var matchResults = searchMatches
                .Where(x => x != null)
                .Select(x => x)
                .OrderByDescending(x => x!.Score);

            var list = new List<SearchMatch>();

            foreach (SearchMatch? match in matchResults)
            {
                list.Add(match!);
            }

            await RefreshAsync();
            return list.ToArray();
        }

        private async Task RefreshAsync()
        {
            await Task.Delay(1);
            await _refresh();
            await Task.Delay(1);
        }

        private static string BuildFormattedMatchText(string text, SearchInfo search)
        {
            return search.KeywordReplacerRegex == null
                ? text
                : search.KeywordReplacerRegex.Replace(text, "<b>$&</b>");
        }

        private static string ConvertToRegexWildcardSearch(string[] words)
        {
            if (words.Length == 0)
            {
                return "";
            }
            return words.StringJoin(".*?");
        }

        private async Task<SearchMatch[]> ExecuteSearchAsync(SearchMode mode, Func<ValueTuple<TopDocs, string, string[]>> action)
        {
            await _searchLock.WaitAsync();

            try
            {
                Stopwatch.StartNew();
                await RefreshAsync();

                var valueTuple = action();
                var searchInfo = new SearchInfo(mode, valueTuple.Item3);
                return await ProcessSearchMatchesAsync(valueTuple.Item1, valueTuple.Item2, searchInfo);
            }
            finally
            {
                _searchLock.Release();
            }
        }

        private static void LogMessage(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);
        }
    }
}
