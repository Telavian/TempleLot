using System.Text.RegularExpressions;
using TempleLotViewer.Enums;

namespace TempleLotViewer.Services.WitnessSearch.Models
{
    public class SearchInfo
    {
        public string[] SearchKeywords { get; }
        public SearchMode Mode { get; }
        public Regex? KeywordReplacerRegex { get; }

        public SearchInfo(SearchMode mode, string[] searchKeywords)
        {
            Mode = mode;
            SearchKeywords = searchKeywords;

            if (SearchKeywords.Length > 0)
            {
                var keywordText = string.Join("|", SearchKeywords);
                KeywordReplacerRegex = new Regex(keywordText, RegexOptions.IgnoreCase);
            }
        }
    }
}
