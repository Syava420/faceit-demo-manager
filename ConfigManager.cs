using System;
using System.IO;
using System.Linq;
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
        public bool DeleteArchivesAfterUnpack { get; set; }
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
            list.Add(new DemoBind() { IsEnabled = true, ActionName = "Вкл/Выкл войс-чат в демке", Key = "v", Command = "toggle tv_listen_voice_indices -1 0; toggle tv_listen_voice_indices_h -1 0" });
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
            settings.DeleteArchivesAfterUnpack = true;
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
                        else if (line.StartsWith("DeleteArchivesAfterUnpack=")) settings.DeleteArchivesAfterUnpack = bool.Parse(line.Substring("DeleteArchivesAfterUnpack=".Length).Trim());
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

            // Auto-detect Downloads path if empty
            if (string.IsNullOrEmpty(settings.DownloadsPath) || !Directory.Exists(settings.DownloadsPath))
            {
                string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(userDownloads))
                {
                    settings.DownloadsPath = userDownloads;
                }
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

        public static string AutoDetectCS2Path(Action<string> logAction = null)
        {
            Action<string> log = msg => {
                try {
                    logAction?.Invoke(msg);
                    string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FaceitDemoHub", "debug_log.txt");
                    string logFolder = Path.GetDirectoryName(logFile);
                    if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
                    File.AppendAllText(logFile, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + Environment.NewLine);
                } catch { }
            };

            log("=== НАЧАЛО АВТОПОИСКА ПАПКИ CS2 ===");
            List<string> candidates = new List<string>();

            // 1. HKCU Registry
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string val = key.GetValue("SteamPath") as string ?? key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(val))
                        {
                            string norm = val.Replace('/', '\\');
                            log("[Реестр HKCU] Найдена запись SteamPath: " + norm);
                            candidates.Add(norm);
                        }
                        else { log("[Реестр HKCU] Ключ Software\\Valve\\Steam существует, но значения пусты."); }
                    }
                    else { log("[Реестр HKCU] Ключ Software\\Valve\\Steam не найден."); }
                }
            }
            catch (Exception ex) { log("[Реестр HKCU Ошибка] " + ex.Message); }

            // 2. HKLM Registry (64-bit and 32-bit views)
            try
            {
                using (var key = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string val = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(val)) { log("[Реестр HKLM 64-bit] Найден InstallPath: " + val); candidates.Add(val); }
                    }
                }
            }
            catch (Exception ex) { log("[Реестр HKLM 64-bit Ошибка] " + ex.Message); }

            try
            {
                using (var key = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string val = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(val)) { log("[Реестр HKLM 32-bit] Найден InstallPath: " + val); candidates.Add(val); }
                    }
                }
            }
            catch (Exception ex) { log("[Реестр HKLM 32-bit Ошибка] " + ex.Message); }

            // 3. Known Common Paths
            candidates.Add(@"C:\Program Files (x86)\Steam");
            candidates.Add(@"C:\Program Files\Steam");
            candidates.Add(@"D:\Pro\Steam");
            candidates.Add(@"D:\Steam");
            candidates.Add(@"E:\Steam");

            var distinctCandidates = candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            log("[Автопоиск] Список кандидатов папок Steam (" + distinctCandidates.Count + " шт.):");
            foreach (var c in distinctCandidates) log("  - " + c);

            foreach (string path in distinctCandidates)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (!Directory.Exists(path))
                {
                    log("[Проверка] Каталог не существует: " + path);
                    continue;
                }

                log("[Проверка] Каталог Steam найден: " + path);

                // Check default installation
                string defaultCs2 = Path.Combine(path, @"steamapps\common\Counter-Strike Global Offensive\game\csgo");
                if (Directory.Exists(defaultCs2))
                {
                    log("[УСПЕХ] Найдена папка CS2 (game/csgo) по прямому пути -> " + defaultCs2);
                    return defaultCs2;
                }
                else
                {
                    log("[Проверка] Прямой путь csgo отсутствует: " + defaultCs2);
                }

                // Parse secondary library folders
                string vdfPath = Path.Combine(path, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    log("[Проверка] Файл библиотеки libraryfolders.vdf найден: " + vdfPath);
                    try
                    {
                        string[] lines = File.ReadAllLines(vdfPath);
                        foreach (string line in lines)
                        {
                            Match m = Regex.Match(line, @"""path""\s+""([^""]+)""");
                            if (m.Success)
                            {
                                string libPath = m.Groups[1].Value.Replace(@"\\", @"\");
                                log("[VDF Библиотека] Проверка пути из vdf: " + libPath);
                                string cs2Path = Path.Combine(libPath, @"steamapps\common\Counter-Strike Global Offensive\game\csgo");
                                if (Directory.Exists(cs2Path))
                                {
                                    log("[УСПЕХ] Найдена папка CS2 через libraryfolders.vdf -> " + cs2Path);
                                    return cs2Path;
                                }
                            }
                        }
                    }
                    catch (Exception ex) { log("[VDF Ошибка чтения] " + ex.Message); }
                }
            }

            // 4. Fallback scan across all local drives
            log("[Автопоиск] Запуск глубокого сканирования по дискам...");
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                    string root = drive.RootDirectory.FullName;
                    log("[Сканирование диска] " + root);
                    
                    string[] possibleSubfolders = new string[] {
                        @"Pro\Steam", @"Games\Steam", @"Steam", @"SteamLibrary", @"Program Files (x86)\Steam", @"Program Files\Steam"
                    };

                    foreach (var sub in possibleSubfolders)
                    {
                        string target = Path.Combine(root, sub, @"steamapps\common\Counter-Strike Global Offensive\game\csgo");
                        if (Directory.Exists(target))
                        {
                            log("[УСПЕХ] Глубокое сканирование найдено -> " + target);
                            return target;
                        }
                    }
                }
            }
            catch (Exception ex) { log("[Сканирование дисков Ошибка] " + ex.Message); }

            log("[ОШИБКА] Папка CS2 (game/csgo) не найдена ни по одному маршруту!");
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
                    "DeleteArchivesAfterUnpack=" + settings.DeleteArchivesAfterUnpack.ToString(),
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
