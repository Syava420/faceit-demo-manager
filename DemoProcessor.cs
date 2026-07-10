using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

namespace FaceitDemoManager
{
    public class DemoMetadata
    {
        public string Map { get; set; }
        public string Score { get; set; }
        public string KD { get; set; }
        public string Date { get; set; }
        public string Note { get; set; }
    }

    public static class DemoProcessor
    {
        private const string zstdExeName = "zstd.exe";

        public static DemoMetadata ParseMetadataFromFilename(string filename)
        {
            DemoMetadata dm = new DemoMetadata() { Map = "Unknown", Score = "?-?", KD = "-", Date = "-", Note = "" };
            Match m = Regex.Match(filename, @"faceit_(\d{4}-\d{2}-\d{2})_([a-zA-Z0-9]+)_(\d+-\d+)_[a-f0-9]{8}\.dem");
            if (m.Success)
            {
                dm.Date = m.Groups[1].Value;
                dm.Map = m.Groups[2].Value;
                dm.Score = m.Groups[3].Value;
            }
            return dm;
        }

        public static DemoMetadata GetStatsFromApi(string matchId, string nickname, Action<string, bool> logCallback)
        {
            DemoMetadata dm = new DemoMetadata() { Map = "Unknown", Score = "?-?", KD = "-", Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Note = "" };
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FaceitDemoHub");
                    string json = wc.DownloadString("https://api.faceit.com/stats/v1/stats/matches/" + matchId);

                    Match mDate = Regex.Match(json, @"""date""\s*:\s*(\d+)");
                    if (mDate.Success)
                    {
                        long timestamp = long.Parse(mDate.Groups[1].Value);
                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        dm.Date = epoch.AddMilliseconds(timestamp).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    Match mMap = Regex.Match(json, @"""i1""\s*:\s*""([^""]+)""");
                    if (mMap.Success)
                    {
                        string rawMap = mMap.Groups[1].Value;
                        dm.Map = rawMap.StartsWith("de_") ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawMap.Substring(3)) : rawMap;
                    }

                    Match mScore = Regex.Match(json, @"""i18""\s*:\s*""([^""]+)""");
                    if (mScore.Success)
                    {
                        dm.Score = mScore.Groups[1].Value.Replace(" ", "").Replace("/", "-");
                    }

                    if (!string.IsNullOrEmpty(nickname))
                    {
                        string pattern = @"\{[^{}]*""nickname""\s*:\s*""" + Regex.Escape(nickname) + @"""[^{}]*\}";
                        Match mPlayerBlock = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
                        if (mPlayerBlock.Success)
                        {
                            string block = mPlayerBlock.Value;
                            Match mKills = Regex.Match(block, @"""i6""\s*:\s*""(\d+)""");
                            Match mAssists = Regex.Match(block, @"""i7""\s*:\s*""(\d+)""");
                            Match mDeaths = Regex.Match(block, @"""i8""\s*:\s*""(\d+)""");
                            Match mKD = Regex.Match(block, @"""c2""\s*:\s*""([\d.]+)""");
                            Match mADR = Regex.Match(block, @"""c10""\s*:\s*""([\d.]+)""");

                            if (mKills.Success && mDeaths.Success && mKD.Success)
                            {
                                string assists = mAssists.Success ? mAssists.Groups[1].Value : "0";
                                string adr = mADR.Success ? mADR.Groups[1].Value : "-";
                                dm.KD = string.Format("{0} ({1}/{2}/{3}) [{4}]", mKD.Groups[1].Value, mKills.Groups[1].Value, mDeaths.Groups[1].Value, assists, adr);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (logCallback != null) logCallback("Предупреждение API: " + ex.Message, false);
            }
            return dm;
        }

        public static void LoadMetadataDb(string baseDemosDir, Dictionary<string, DemoMetadata> db)
        {
            db.Clear();
            if (string.IsNullOrEmpty(baseDemosDir)) return;
            string path = Path.Combine(baseDemosDir, "metadata.txt");
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        DemoMetadata dm = new DemoMetadata()
                        {
                            Map = parts[1],
                            Score = parts[2],
                            KD = parts[3],
                            Date = parts[4],
                            Note = parts.Length >= 6 ? parts[5] : ""
                        };
                        db[parts[0]] = dm;
                    }
                }
            }
            catch { }
        }

        public static void SaveMetadataForDemo(string baseDemosDir, Dictionary<string, DemoMetadata> db, string relativePath, DemoMetadata dm)
        {
            if (string.IsNullOrEmpty(baseDemosDir)) return;
            string path = Path.Combine(baseDemosDir, "metadata.txt");
            LoadMetadataDb(baseDemosDir, db);
            db[relativePath] = dm;

            try
            {
                List<string> lines = new List<string>();
                foreach (var kvp in db)
                {
                    lines.Add(string.Format("{0}|{1}|{2}|{3}|{4}|{5}", kvp.Key, kvp.Value.Map, kvp.Value.Score, kvp.Value.KD, kvp.Value.Date, kvp.Value.Note));
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        public static bool ProcessSingleFile(
            string zstPath,
            string targetCategory,
            string cs2Directory,
            string nickname,
            Dictionary<string, DemoMetadata> db,
            Action<string, bool> logCallback,
            bool deleteZstAfter = true)
        {
            string scriptDir = AppDomain.CurrentDomain.BaseDirectory;
            string zstdPath = Path.Combine(scriptDir, zstdExeName);

            string baseDemosDir = Path.Combine(cs2Directory, "faceit_demos");
            string fileName = Path.GetFileName(zstPath);
            bool isZst = zstPath.EndsWith(".zst", StringComparison.OrdinalIgnoreCase);

            string extractedPath = "";

            if (isZst)
            {
                if (!File.Exists(zstdPath))
                {
                    throw new Exception("Не найден архиватор zstd.exe в папке программы!");
                }
                string fileBaseName = Path.GetFileNameWithoutExtension(zstPath); // оставляет .dem
                extractedPath = Path.Combine(Path.GetDirectoryName(zstPath), fileBaseName);

                if (logCallback != null) logCallback("Распаковка: " + fileName, false);
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = zstdPath;
                psi.Arguments = string.Format("-d \"{0}\" -o \"{1}\" --force", zstPath, extractedPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        throw new Exception("zstd завершился с кодом ошибки " + p.ExitCode);
                    }
                }

                if (!File.Exists(extractedPath))
                {
                    throw new Exception("Распакованный файл не найден на диске!");
                }
            }
            else
            {
                extractedPath = zstPath;
            }

            string fileBaseNameForRename = Path.GetFileName(extractedPath);
            string shortName = "faceit_latest";
            Match m = Regex.Match(fileName, @"(1-[a-f0-9]{8})-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}");

            DemoMetadata dm = new DemoMetadata() { Map = "Unknown", Score = "?-?", KD = "-", Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Note = "" };

            if (m.Success)
            {
                string matchId = m.Groups[0].Value;
                string shortId = m.Groups[1].Value.Substring(2, 8); // убираем "1-" и берем 8 символов
                
                if (logCallback != null) logCallback("Запрос статистики с Faceit API...", false);
                dm = GetStatsFromApi(matchId, nickname, logCallback);
                shortName = string.Format("faceit_{0}_{1}_{2}_{3}", dm.Date.Substring(0, 10), dm.Map.Replace(" ", ""), dm.Score, shortId);
            }
            else
            {
                shortName = "faceit_" + Path.GetFileNameWithoutExtension(extractedPath);
            }

            string targetFolderFolder = Path.Combine(baseDemosDir, targetCategory);
            Directory.CreateDirectory(targetFolderFolder);

            // Очистка от запрещенных символов в названии (типа '?' при падении API)
            string cleanShortName = shortName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                cleanShortName = cleanShortName.Replace(c, '-');
            }

            string destPathUnique = Path.Combine(targetFolderFolder, cleanShortName + ".dem");
            string destPathLatest = Path.Combine(Path.Combine(baseDemosDir, "General"), "faceit.dem");

            if (File.Exists(destPathUnique)) File.Delete(destPathUnique);

            try
            {
                if (extractedPath.Equals(destPathUnique, StringComparison.OrdinalIgnoreCase))
                {
                    // Already in target destination
                }
                else
                {
                    File.Move(extractedPath, destPathUnique);
                }
            }
            catch (Exception ex)
            {
                // Резервный вариант переноса между дисками C: и D:
                try
                {
                    File.Copy(extractedPath, destPathUnique, true);
                    if (!isZst)
                    {
                        File.Delete(extractedPath);
                    }
                }
                catch (Exception copyEx)
                {
                    throw new Exception("Не удалось переместить файл: " + ex.Message + " (Копирование тоже сбойнуло: " + copyEx.Message + ")");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPathLatest));
            File.Copy(destPathUnique, destPathLatest, true);

            string relPath = destPathUnique.Substring(baseDemosDir.Length).TrimStart('\\', '/').Replace('\\', '/');
            SaveMetadataForDemo(baseDemosDir, db, relPath, dm);

            if (isZst && File.Exists(zstPath) && deleteZstAfter)
            {
                File.Delete(zstPath);
            }

            if (logCallback != null) logCallback("Успешно добавлен матч: " + cleanShortName, false);
            return true;
        }
    }
}
