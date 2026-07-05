using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FaceitDemoManager
{
    public class DemoBind
    {
        public bool IsEnabled { get; set; }
        public string ActionName { get; set; }
        public string Key { get; set; }
        public string Command { get; set; }
    }

    public class AppSettings
    {
        public string DownloadsPath { get; set; }
        public string CS2Path { get; set; }
        public string Nickname { get; set; }
        public bool WatchFolder { get; set; }
        public bool MinimizeTray { get; set; }
        public bool EnableDemoVoice { get; set; }
        public bool AutoApplyBinds { get; set; } // Global auto-apply flag
        public string ImportMode { get; set; }
        public string TargetImportFolder { get; set; }
        public Dictionary<string, string> FolderNicknames { get; set; }
        public List<DemoBind> Binds { get; set; }
    }

    public static class ConfigManager
    {
        public static List<DemoBind> GetDefaultBinds()
        {
            var list = new List<DemoBind>();
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Пауза / Воспроизведение", Key = "p", Command = "demo_togglepause" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Замедление (0.5x)", Key = "j", Command = "demo_timescale 0.5" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Обычная скорость (1x)", Key = "k", Command = "demo_timescale 1" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Ускорение (2x)", Key = "l", Command = "demo_timescale 2" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Ускорение (4x)", Key = "o", Command = "demo_timescale 4" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Быстрая перемотка (5x)", Key = ";", Command = "demo_timescale 5" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Предыдущий раунд", Key = "[", Command = "demoui; slot12" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Следующий раунд", Key = "]", Command = "demoui; slot13" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Отмотать назад (5 сек)", Key = "LEFTARROW", Command = "demo_goto -1000 1 0" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Чистый экран (Скрыть HUD)", Key = "h", Command = "toggle cl_draw_only_deathnotices 0 1" });
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Подсветка сквозь стены (X-Ray)", Key = "x", Command = "toggle spec_show_xray 0 1" });
            return list;
        }

        public static AppSettings Load(string configPath)
        {
            AppSettings settings = new AppSettings();
            
            // Default paths
            settings.DownloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            settings.CS2Path = "";
            settings.Nickname = "";
            settings.WatchFolder = true;
            settings.MinimizeTray = true;
            settings.EnableDemoVoice = true;
            settings.AutoApplyBinds = true;
            settings.ImportMode = "General";
            settings.TargetImportFolder = "General";
            settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            settings.Binds = GetDefaultBinds();

            if (File.Exists(configPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("DownloadsPath=")) settings.DownloadsPath = line.Substring("DownloadsPath=".Length).Trim();
                        else if (line.StartsWith("CS2Path=")) settings.CS2Path = line.Substring("CS2Path=".Length).Trim();
                        else if (line.StartsWith("Nickname=")) settings.Nickname = line.Substring("Nickname=".Length).Trim();
                        else if (line.StartsWith("WatchFolder=")) settings.WatchFolder = bool.Parse(line.Substring("WatchFolder=".Length).Trim());
                        else if (line.StartsWith("MinimizeTray=")) settings.MinimizeTray = bool.Parse(line.Substring("MinimizeTray=".Length).Trim());
                        else if (line.StartsWith("EnableDemoVoice=")) settings.EnableDemoVoice = bool.Parse(line.Substring("EnableDemoVoice=".Length).Trim());
                        else if (line.StartsWith("AutoApplyBinds=")) settings.AutoApplyBinds = bool.Parse(line.Substring("AutoApplyBinds=".Length).Trim());
                        else if (line.StartsWith("ImportMode=")) settings.ImportMode = line.Substring("ImportMode=".Length).Trim();
                        else if (line.StartsWith("TargetImportFolder=")) settings.TargetImportFolder = line.Substring("TargetImportFolder=".Length).Trim();
                        else if (line.StartsWith("FolderNickname:"))
                        {
                            string[] parts = line.Substring("FolderNickname:".Length).Split(new char[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                settings.FolderNicknames[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                        else if (line.StartsWith("Bind|"))
                        {
                            string[] parts = line.Substring("Bind|".Length).Split('|');
                            if (parts.Length >= 4)
                            {
                                bool isEnabled = bool.Parse(parts[0]);
                                string action = parts[1];
                                string key = parts[2];
                                string cmd = parts[3];

                                var existing = settings.Binds.Find(b => b.ActionName.Equals(action, StringComparison.OrdinalIgnoreCase));
                                if (existing != null)
                                {
                                    existing.IsEnabled = isEnabled;
                                    existing.Key = key;
                                    existing.Command = cmd;
                                }
                                else
                                {
                                    settings.Binds.Add(new DemoBind() { IsEnabled = isEnabled, ActionName = action, Key = key, Command = cmd });
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // Auto-detect CS2 path if empty or invalid
            if (string.IsNullOrEmpty(settings.CS2Path) || !Directory.Exists(settings.CS2Path))
            {
                string detected = AutoDetectCS2Path();
                if (!string.IsNullOrEmpty(detected))
                {
                    settings.CS2Path = detected;
                }
            }

            return settings;
        }

        public static string AutoDetectCS2Path()
        {
            string steamPath = null;
            try
            {
                steamPath = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null) as string;
                if (string.IsNullOrEmpty(steamPath))
                {
                    steamPath = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
                }
            }
            catch { }

            List<string> steamPaths = new List<string>();
            if (!string.IsNullOrEmpty(steamPath)) steamPaths.Add(steamPath);
            steamPaths.Add(@"C:\Program Files (x86)\Steam");
            steamPaths.Add(@"C:\Program Files\Steam");
            steamPaths.Add(@"D:\Steam");
            steamPaths.Add(@"E:\Steam");
            steamPaths.Add(@"D:\Games\Steam");

            foreach (string path in steamPaths)
            {
                if (!Directory.Exists(path)) continue;

                // Check default installation
                string defaultCs2 = Path.Combine(path, @"steamapps\common\Counter-Strike Global Offensive\game\csgo");
                if (Directory.Exists(defaultCs2)) return defaultCs2;

                // Parse secondary library folders
                string vdfPath = Path.Combine(path, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(vdfPath);
                        foreach (string line in lines)
                        {
                            Match m = Regex.Match(line, @"""path""\s+""([^""]+)""");
                            if (m.Success)
                            {
                                string libPath = m.Groups[1].Value.Replace(@"\\", @"\");
                                string cs2Path = Path.Combine(libPath, @"steamapps\common\Counter-Strike Global Offensive\game\csgo");
                                if (Directory.Exists(cs2Path)) return cs2Path;
                            }
                        }
                    }
                    catch { }
                }
            }

            return "";
        }

        public static void Save(string configPath, AppSettings settings)
        {
            try
            {
                List<string> lines = new List<string>() {
                    "DownloadsPath=" + settings.DownloadsPath.Trim(),
                    "CS2Path=" + settings.CS2Path.Trim(),
                    "Nickname=" + settings.Nickname.Trim(),
                    "WatchFolder=" + settings.WatchFolder.ToString(),
                    "MinimizeTray=" + settings.MinimizeTray.ToString(),
                    "EnableDemoVoice=" + settings.EnableDemoVoice.ToString(),
                    "AutoApplyBinds=" + settings.AutoApplyBinds.ToString(),
                    "ImportMode=" + settings.ImportMode.Trim(),
                    "TargetImportFolder=" + settings.TargetImportFolder.Trim()
                };

                if (settings.FolderNicknames != null)
                {
                    foreach (var kvp in settings.FolderNicknames)
                    {
                        lines.Add("FolderNickname:" + kvp.Key + "=" + kvp.Value.Trim());
                    }
                }

                if (settings.Binds != null)
                {
                    foreach (var b in settings.Binds)
                    {
                        lines.Add(string.Format("Bind|{0}|{1}|{2}|{3}", b.IsEnabled, b.ActionName, b.Key, b.Command));
                    }
                }

                File.WriteAllLines(configPath, lines.ToArray());
            }
            catch { }
        }
    }
}
