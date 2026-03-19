using System;
using System.Collections.Generic;
using System.Text;

namespace XlentWPF.Dtos
{
    public class FastTranscriptionResultDto
    {
        public string FullText { get; set; } = "";
        public List<FastTranscriptSegmentDto> Segments { get; set; } = [];
    }

    public class FastTranscriptSegmentDto
    {
        public string SpeakerId { get; set; } = "";
        public string Text { get; set; } = "";
        public double StartOffsetSeconds { get; set; }
        public double DurationSeconds { get; set; }
    }
}
