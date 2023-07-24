namespace TempleLotViewer.Services.WitnessSearch.Models
{
    public class SearchItem
    {
        public string Text { get; set; } = "";
        public string XPath { get; set; } = "";
        public string Witness { get; set; } = "";
        public int WitnessNumber { get; set; } = 0;
        public int Question { get; set; } = 0;

        public override string ToString()
        {
            return $"T:{Text}, WN:{WitnessNumber}, W:{Witness}, Q:{Question}, X:{XPath}";
        }
    }
}
