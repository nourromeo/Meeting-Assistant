using System;
using System.Collections.Generic;
using System.Text;

namespace XlentWPF.Dtos
{
    public class MeetingSummaryResponseDto
    {
        public string Summary { get; set; } = "";
        public List<string> ActionItems { get; set; } = [];
        public List<string> KeyPoints { get; set; } = [];
    }
}
