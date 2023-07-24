using TempleLotViewer.Services.WitnessSearch.Models;

namespace TempleLotViewer.Search
{
    public class SearchResults
    {
        public SearchMatch[] AllMatches { get; init; } = Array.Empty<SearchMatch>();
        public SearchMatchMode MatchMode { get; set; }
    }
}
