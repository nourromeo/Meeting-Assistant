using System;
using System.Collections.Generic;
using System.Text;

namespace XlentWPF.Dtos
{
    class MeetingSummaryRequestDto
    {
        public List<TranscriptSegmentDto> Transcript { get; set; } = [];
        public string Language { get; set; } = "en-US";
    }
}
