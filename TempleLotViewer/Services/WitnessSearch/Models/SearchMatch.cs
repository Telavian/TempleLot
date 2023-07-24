using TempleLotViewer.Enums;

namespace TempleLotViewer.Services.WitnessSearch.Models
{
    public class SearchMatch
    {
        public string Witness { get; init; } = "";

        public int WitnessNumber { get; init; }

        public int Question { get; init; }

        public string FormattedText { get; init; } = "";

        public string[] Keywords { get; init; } = Array.Empty<string>();

        public SearchMode Mode { get; init; } = SearchMode.None;

        public double Score { get; init; }

        public string Text { get; init; } = "";

        public string XPath { get; init; } = "";
    }
}
