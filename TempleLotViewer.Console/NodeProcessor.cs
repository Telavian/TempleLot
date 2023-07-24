using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TempleLotViewer.Console
{
    public class NodeProcessor
    {
        public async Task ProcessAllFilesAsync(string location)
        {
            var files = Directory.GetFiles(location, "*.html");
            foreach (var file in files)
            {
                var doc = new HtmlDocument();
                doc.Load(file);

                var isChanged = ProcessNode(file, doc.DocumentNode);

                if (isChanged)
                {
                    var html = doc.DocumentNode.WriteContentTo();
                    await File.WriteAllTextAsync(file, html);
                }
            }
        }

        private bool ProcessNode(string file, HtmlNode node)
        {
            if (node.Name == "questionnumber" && node.ParentNode.Name != "body")
            {
                System.Console.WriteLine($"{node.InnerHtml} - {file}");
                return false;
            }

            var isChanged = false;
            foreach (var child in node.ChildNodes.ToArray())
            {
                isChanged = ProcessNode(file, child) || isChanged;
            }

            return isChanged;
        }

        private HtmlNode? GetNodeNextSibling(HtmlNode? node, string[] skipTags = null)
        {
            skipTags ??= Array.Empty<string>();

            while (node != null)
            {
                node = node.NextSibling;

                if (node == null)
                {
                    return null;
                }

                if (skipTags.Length > 0 && skipTags.Any(x => node.Name == x)) continue;

                if (node.InnerHtml.Trim().Length > 0) return node;
                if (node == null) break;
            }

            throw new Exception("Unable to find node next sibling");
        }
    }
}
