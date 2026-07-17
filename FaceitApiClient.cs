using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FaceitDemoManager
{
    public static class FaceitApiClient
    {
        private static readonly HttpClient client;

        static FaceitApiClient()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FaceitDemoHub");
        }

        public static async Task<(int level, int elo)> FetchUserEloAsync(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return (0, 0);

            try
            {
                string url = "https://api.faceit.com/users/v1/nicknames/" + Uri.EscapeDataString(nickname);
                string json = await client.GetStringAsync(url);
                
                int lvl = 0;
                int elo = 0;
                
                Match mLevel = Regex.Match(json, @"""skill_level""\s*:\s*(\d+)");
                Match mElo = Regex.Match(json, @"""faceit_elo""\s*:\s*(\d+)");
                
                if (mLevel.Success) lvl = int.Parse(mLevel.Groups[1].Value);
                if (mElo.Success) elo = int.Parse(mElo.Groups[1].Value);
                
                return (lvl, elo);
            }
            catch
            {
                return (0, 0);
            }
        }

        public static async Task UpdateFolderDemosStatsAsync(
            string folderName, 
            string nickname, 
            string baseDir, 
            Dictionary<string, DemoMetadata> metadataDb,
            Action<string> onLog,
            Action<string, DemoMetadata> onDemoUpdated,
            Action onFinished)
        {
            if (string.IsNullOrEmpty(folderName) || folderName == "[Все демки]" || string.IsNullOrEmpty(nickname)) return;

            onLog?.Invoke($"Обновление статистики для папки '{folderName}' с никнеймом '{nickname}'...");

            try
            {
                string playerId = "";
                string profileUrl = "https://api.faceit.com/users/v1/nicknames/" + Uri.EscapeDataString(nickname);
                string profileJson = await client.GetStringAsync(profileUrl);
                
                Match mId = Regex.Match(profileJson, @"""id""\s*:\s*""([^""]+)""");
                if (mId.Success)
                {
                    playerId = mId.Groups[1].Value;
                }

                if (string.IsNullOrEmpty(playerId))
                {
                    onLog?.Invoke("Не удалось найти игрока с таким никнеймом на Faceit.");
                    return;
                }

                string historyUrl = $"https://api.faceit.com/stats/v1/stats/time/users/{playerId}/games/cs2?size=100";
                string historyJson = await client.GetStringAsync(historyUrl);

                var matches = new Dictionary<string, DemoMetadata>(StringComparer.OrdinalIgnoreCase);
                MatchCollection mc = Regex.Matches(historyJson, @"\{[^{}]*""matchId""\s*:\s*""([^""]+)""[^{}]*\}");
                
                foreach (Match matchObj in mc)
                {
                    string block = matchObj.Value;
                    string matchId = matchObj.Groups[1].Value;
                    
                    Match mKills = Regex.Match(block, @"""i6""\s*:\s*""(\d+)""");
                    Match mAssists = Regex.Match(block, @"""i7""\s*:\s*""(\d+)""");
                    Match mDeaths = Regex.Match(block, @"""i8""\s*:\s*""(\d+)""");
                    Match mKD = Regex.Match(block, @"""c2""\s*:\s*""([\d.]+)""");
                    Match mADR = Regex.Match(block, @"""c10""\s*:\s*""([\d.]+)""");
                    Match mMap = Regex.Match(block, @"""i1""\s*:\s*""([^""]+)""");
                    Match mScore = Regex.Match(block, @"""i18""\s*:\s*""([^""]+)""");
                    Match mDate = Regex.Match(block, @"""date""\s*:\s*(\d+)");

                    if (mKills.Success && mDeaths.Success && mKD.Success)
                    {
                        string assists = mAssists.Success ? mAssists.Groups[1].Value : "0";
                        string adr = mADR.Success ? mADR.Groups[1].Value : "-";
                        string kdStr = $"{mKD.Groups[1].Value} ({mKills.Groups[1].Value}/{mDeaths.Groups[1].Value}/{assists}) [{adr}]";
                        
                        string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        if (mDate.Success)
                        {
                            long timestamp = long.Parse(mDate.Groups[1].Value);
                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                            dateStr = epoch.AddMilliseconds(timestamp).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                        }

                        string mapName = mMap.Success ? mMap.Groups[1].Value : "Unknown";
                        if (mapName.StartsWith("de_")) mapName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mapName.Substring(3));
                        string scoreStr = mScore.Success ? mScore.Groups[1].Value.Replace(" ", "").Replace("/", "-") : "?-?";

                        DemoMetadata dm = new DemoMetadata()
                        {
                            Map = mapName,
                            Score = scoreStr,
                            KD = kdStr,
                            Date = dateStr,
                            Note = ""
                        };
                        matches[matchId] = dm;
                    }
                }

                string targetFolder = Path.Combine(baseDir, folderName);
                if (Directory.Exists(targetFolder))
                {
                    string[] files = Directory.GetFiles(targetFolder, "*.dem");
                    bool updatedAny = false;
                    
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        Match mShortId = Regex.Match(fileName, @"_([a-f0-9]{8})\.dem$");
                        if (mShortId.Success)
                        {
                            string shortId = mShortId.Groups[1].Value;
                            string fullMatchId = null;
                            foreach (string mid in matches.Keys)
                            {
                                if (mid.Contains(shortId))
                                {
                                    fullMatchId = mid;
                                    break;
                                }
                            }

                            if (fullMatchId != null)
                            {
                                string relativePath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                                DemoMetadata apiDm = matches[fullMatchId];
                                
                                DemoMetadata existingDm;
                                if (metadataDb.TryGetValue(relativePath, out existingDm))
                                {
                                    apiDm.Note = existingDm.Note;
                                }

                                metadataDb[relativePath] = apiDm;
                                DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, relativePath, apiDm);
                                onDemoUpdated?.Invoke(relativePath, apiDm);
                                updatedAny = true;
                                onLog?.Invoke($"Обновлена статистика для демки: {fileName}");
                            }
                        }
                    }

                    if (updatedAny)
                      {
                          onLog?.Invoke($"Статистика для папки '{folderName}' успешно обновлена!");
                          onFinished?.Invoke();
                      }
                      else
                      {
                          onLog?.Invoke("В этой папке не найдено подходящих демок для обновления статистики.");
                      }
                }
            }
            catch (Exception ex)
            {
                onLog?.Invoke("Ошибка при обновлении статистики: " + ex.Message);
            }
        }
    }
}
