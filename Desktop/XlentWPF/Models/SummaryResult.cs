

namespace XlentWPF.Models
{
    public class SummaryResult
    {
        public string Overview { get; set; } = string.Empty;
        public List<string> KeyPoints { get; set; } = [];
        public List<string> ActionItems { get; set; } = [];
    }
}
