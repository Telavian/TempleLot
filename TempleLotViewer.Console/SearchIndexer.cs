using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using HtmlAgilityPack;
using Lucene.Net.Index;
using Lucene.Net.Store;
using TempleLotViewer.Extensions;
using TempleLotViewer.Pages.Models;
using TempleLotViewer.Services.WitnessSearch.Models;
using Directory = System.IO.Directory;
using Lucene.Net.Documents;
using System.Text.RegularExpressions;

namespace TempleLotViewer.Console
{
    public class SearchIndexer
    {
        private static readonly string _rootPath = @"G:\UncorrelatedMormonism\TempleLotViewer\TempleLotViewer\wwwroot";
        private static readonly LuceneVersion _luceneVersion = LuceneVersion.LUCENE_48;
        private static readonly Analyzer _analyzer = new StandardAnalyzer(_luceneVersion);

        public async Task IndexAllWitnessesAsync()
        {
            var location = Path.Combine(_rootPath, "Witnesses");
            var files = Directory.GetFiles(location, "*.html");
            var items = new List<SearchItem>();

            foreach (var file in files)
            {
                await IndexWitnessAsync(file, items);
            }

            var path = Path.Combine(_rootPath, @"Witnesses\FullIndex");
            using (var directory = FSDirectory.Open(path))
            {
                IndexWitnessData(items, directory);
            }
        }

        private async Task IndexWitnessAsync(string file, List<SearchItem> items)
        {
            if (file.Contains("Overview"))
            {
                return;
            }

            var doc = new HtmlDocument();
            doc.Load(file);

            var questionNumber = 0;
            var witnessInfo = GetWitnessInfo(file);

            ProcessIndexWitnessNode(witnessInfo.Name, witnessInfo.Number, doc.DocumentNode, items, ref questionNumber);
        }

        private (int Number, string Name) GetWitnessInfo(string file)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var parts = name.Split('_', 2);

            return (int.Parse(parts[0]) + 1, parts[1]);
        }

        private void ProcessIndexWitnessNode(string witnessName, int witnessNumber, HtmlNode node, List<SearchItem> items, ref int questionNumber)
        {
            if (node.Name == "questionnumber")
            {
                questionNumber = int.Parse(node.InnerText);
                return;
            }

            if (node.Name == "question" || node.Name == "questionanswer")
            {
                var text = node.InnerText.Trim();
                if (text.Length == 0) return;

                items.Add(new SearchItem
                {
                    Witness = witnessName,
                    WitnessNumber = witnessNumber,
                    Text = text,
                    Question = questionNumber,
                    XPath = node.XPath
                });
                return;
            }

            foreach (var child in node.ChildNodes)
            {
                ProcessIndexWitnessNode(witnessName, witnessNumber, child, items, ref questionNumber);
            }
        }

        private void IndexWitnessData(IReadOnlyCollection<SearchItem> bookItems, FSDirectory directory)
        {
            System.Console.WriteLine($"Indexing {bookItems.Count} documents");
            var config = new IndexWriterConfig(_luceneVersion, _analyzer);
            config.OpenMode = OpenMode.CREATE;

            using (var writer = new IndexWriter(directory, config))
            {
                foreach (var item in bookItems)
                {
                    var text = item.Text;
                    text = Regex.Replace(text, @"\d{1,}", @" $& ").Replace("  ", " ");

                    var doc = new Document
                    {
                        new TextField(nameof(SearchItem.Text), text, Field.Store.YES),
                        new StringField($"{nameof(SearchItem.XPath)}", item.XPath, Field.Store.YES),
                        new StringField($"{nameof(SearchItem.Witness)}", item.Witness, Field.Store.YES),
                        new Int32Field($"{nameof(SearchItem.WitnessNumber)}", item.WitnessNumber, Field.Store.YES),
                        new Int32Field($"{nameof(SearchItem.Question)}", item.Question, Field.Store.YES),
                    };
                    writer.AddDocument(doc);
                }

                writer.Flush(true, true);
                writer.Commit();
            }
        }
    }
}
