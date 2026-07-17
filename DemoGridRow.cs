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
        public DateTime ImportDate { get; set; } // Date added to folder
        public string ImportDateFormatted { get; set; } // Formatted import date
        public string Folder { get; set; }
        public string Note { get; set; }
        public string FilePath { get; set; }

        public static string GetMapEmoji(string map)
        {
            if (string.IsNullOrEmpty(map)) return "🗺️";
            string name = map.Trim().ToLower();
            if (name.Contains("mirage")) return "🏜️";
            if (name.Contains("dust")) return "🏜️";
            if (name.Contains("ancient")) return "🌴";
            if (name.Contains("nuke")) return "☢️";
            if (name.Contains("inferno")) return "🔥";
            if (name.Contains("anubis")) return "🦂";
            if (name.Contains("vertigo")) return "🏗️";
            if (name.Contains("overpass")) return "🌉";
            return "🗺️";
        }

        public static DemoGridRow FromMetadata(DemoMetadata dm, string file, string baseDir)
        {
            string fileName = System.IO.Path.GetFileName(file);
            string folderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(file));

            // Win/Loss and Score
            bool isWin = false;
            string scoreText = dm.Score;
            if (!string.IsNullOrEmpty(dm.Score))
            {
                string[] parts = dm.Score.Split('-');
                if (parts.Length == 2)
                {
                    int s1, s2;
                    if (int.TryParse(parts[0].Trim(), out s1) && int.TryParse(parts[1].Trim(), out s2))
                    {
                        if (s1 > s2)
                        {
                            isWin = true;
                            scoreText = "W " + s1 + " : " + s2;
                        }
                        else
                        {
                            isWin = false;
                            scoreText = "L " + s1 + " : " + s2;
                        }
                    }
                }
            }

            // Stats: K/D/A and ADR
            string kdaText = "-";
            string kdRatioText = "-";
            string kdStatusText = "Normal";
            string adrText = "-";

            if (!string.IsNullOrEmpty(dm.KD) && dm.KD != "-")
            {
                var mRatio = System.Text.RegularExpressions.Regex.Match(dm.KD, @"^([\d.]+)");
                if (mRatio.Success)
                {
                    kdRatioText = mRatio.Groups[1].Value;
                    double ratioVal;
                    if (double.TryParse(kdRatioText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ratioVal))
                    {
                        kdStatusText = ratioVal >= 1.0 ? "High" : "Low";
                    }
                }

                var mKda = System.Text.RegularExpressions.Regex.Match(dm.KD, @"\(([^()]+)\)");
                if (mKda.Success)
                {
                    string kdaRaw = mKda.Groups[1].Value;
                    string[] kparts = kdaRaw.Split('/');
                    if (kparts.Length == 3)
                    {
                        kdaText = string.Format("{0} / {1} / {2}", kparts[0], kparts[1], kparts[2]);
                    }
                    else if (kparts.Length == 2)
                    {
                        kdaText = string.Format("{0} / {1} / -", kparts[0], kparts[1]);
                    }
                }

                var mAdrVal = System.Text.RegularExpressions.Regex.Match(dm.KD, @"\[([^[\]]+)\]");
                if (mAdrVal.Success)
                {
                    adrText = mAdrVal.Groups[1].Value;
                }
            }

            // Date
            string dateTextFormatted = dm.Date;
            DateTime dtVal;
            if (DateTime.TryParse(dm.Date, out dtVal))
            {
                dateTextFormatted = dtVal.ToString("ddd d MMM", new System.Globalization.CultureInfo("ru-RU")) + "\n" + dtVal.ToString("HH:mm");
            }

            // Import Date
            DateTime importDt = DateTime.Now;
            try
            {
                importDt = System.IO.File.GetCreationTime(file);
            }
            catch { }
            string importDateFormatted = importDt.ToString("ddd d MMM", new System.Globalization.CultureInfo("ru-RU")) + "\n" + importDt.ToString("HH:mm");

            return new DemoGridRow()
            {
                Check = false,
                Map = GetMapEmoji(dm.Map) + " " + dm.Map,
                Score = dm.Score,
                ScoreText = scoreText,
                IsWin = isWin,
                KDA = kdaText,
                KDRatio = kdRatioText,
                KDStatus = kdStatusText,
                ADR = adrText,
                Date = dm.Date,
                DateFormatted = dateTextFormatted,
                ImportDate = importDt,
                ImportDateFormatted = importDateFormatted,
                Folder = folderName.Equals("General", StringComparison.OrdinalIgnoreCase) ? "Общая" : folderName,
                Note = dm.Note,
                FilePath = file
            };
        }
    }
}
