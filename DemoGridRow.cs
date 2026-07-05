using System;

namespace FaceitDemoManager
{
    public class DemoGridRow
    {
        public bool Check { get; set; }
        public string Map { get; set; }
        public string Score { get; set; } // Raw score, e.g. "16-14"
        public string ScoreText { get; set; } // Formatted, e.g. "W 16 : 14"
        public bool IsWin { get; set; }
        public string KDA { get; set; } // e.g. "21 / 15 / 4"
        public string KDRatio { get; set; } // e.g. "1.40"
        public string KDStatus { get; set; } // "High" or "Low"
        public string ADR { get; set; } // e.g. "85.2" or "-"
        public string Date { get; set; } // Raw date
        public string DateFormatted { get; set; } // Formatted date & time
        public string Folder { get; set; }
        public string Note { get; set; }
        public string FilePath { get; set; }
    }
}
