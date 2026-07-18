using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Threading;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        public void SendAllStateToWeb()
        {
            if (webBridge == null) return;

            // 1. Settings
            webBridge.SendToWeb(new
            {
                type = "updateSettings",
                settings = new
                {
                    downloadsPath = settings.DownloadsPath ?? "",
                    cs2Path = settings.CS2Path ?? "",
                    nickname = settings.Nickname ?? "",
                    watchFolder = settings.WatchFolder,
                    minimizeTray = settings.MinimizeTray,
                    enableDemoVoice = settings.EnableDemoVoice,
                    deleteArchivesAfterUnpack = settings.DeleteArchivesAfterUnpack,
                    autoApplyBinds = settings.AutoApplyBinds,
                    importMode = settings.ImportMode ?? "General",
                    targetImportFolder = settings.TargetImportFolder ?? "General",
                    folderNicknames = settings.FolderNicknames
                }
            });

            // 2. Profile
            webBridge.SendToWeb(new
            {
                type = "updateProfile",
                nickname = settings.Nickname ?? "Гость",
                elo = userElo > 0 ? userElo.ToString() : "----",
                level = userLevel > 0 ? userLevel.ToString() : "--",
                avatar = "https://assets.faceit-cdn.net/avatars/default_avatar.jpg"
            });

            // 3. Categories
            string demosDir = GetDemosBaseDir();
            var folderItems = new System.Collections.Generic.List<FolderItem>();
            if (Directory.Exists(demosDir))
            {
                LoadSubfoldersRecursive(demosDir, "", 0, folderItems);
            }

            var categoriesJson = new System.Collections.Generic.List<object>();
            categoriesJson.Add(new {
                relativePath = "General",
                displayName = "General",
                depth = 0,
                hasChildren = false,
                isCollapsed = false
            });

            foreach (var fi in folderItems)
            {
                if (fi.RelativePath.Equals("General", StringComparison.OrdinalIgnoreCase)) continue;

                string currentFullDir = Path.Combine(demosDir, fi.RelativePath);
                bool hasChildren = false;
                try
                {
                    hasChildren = Directory.GetDirectories(currentFullDir).Length > 0;
                }
                catch { }

                categoriesJson.Add(new {
                    relativePath = fi.RelativePath,
                    displayName = Path.GetFileName(fi.RelativePath),
                    depth = fi.Depth,
                    hasChildren = hasChildren,
                    isCollapsed = collapsedFolders.Contains(fi.RelativePath)
                });
            }

            webBridge.SendToWeb(new
            {
                type = "updateCategories",
                categories = categoriesJson
            });

            // 4. Demos
            SendDemosToWeb();

            // 5. Binds
            webBridge.SendToWeb(new
            {
                type = "updateBinds",
                binds = settings.Binds
            });
        }

         public void SendDemosToWeb()
        {
            if (webBridge == null) return;

            string demosDir = GetDemosBaseDir();
            if (Directory.Exists(demosDir))
            {
                try
                {
                    metadataDb.Clear();
                    DemoProcessor.LoadMetadataDb(demosDir, metadataDb);
                }
                catch { }
            }

            string categoryDir = Path.Combine(demosDir, selectedCategory ?? "General");
            if (!Directory.Exists(categoryDir)) categoryDir = demosDir;

            var files = Directory.Exists(categoryDir) ? Directory.GetFiles(categoryDir, "*.dem") : new string[0];
            var demoList = files.Select(f => {
                string filename = Path.GetFileName(f);
                string relativePath = "";
                if (!string.IsNullOrEmpty(demosDir) && f.StartsWith(demosDir, StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = f.Substring(demosDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                }
                else
                {
                    relativePath = filename;
                }

                DemoMetadata meta;
                if (!metadataDb.TryGetValue(relativePath, out meta)) {
                    if (!metadataDb.TryGetValue(filename, out meta)) {
                        meta = new DemoMetadata { Map = "de_mirage", Score = "13:10", KD = "1.25", Date = File.GetCreationTime(f).ToString("dd.MM.yyyy HH:mm") };
                    }
                }

                bool? isWin = null;
                if (!string.IsNullOrEmpty(meta.Score))
                {
                    string[] parts = meta.Score.Split(new char[] { '-', ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int s1) && int.TryParse(parts[1], out int s2))
                    {
                        if (s1 != s2) isWin = s1 > s2;
                    }
                }

                return new {
                    fileName = filename,
                    filePath = f,
                    map = meta.Map ?? "Unknown",
                    mapEmoji = DemoGridRow.GetMapEmoji(meta.Map),
                    score = meta.Score ?? "-",
                    isWin = isWin,
                    kd = meta.KD ?? "-",
                    date = meta.Date ?? File.GetCreationTime(f).ToString("dd.MM.yyyy"),
                    note = meta.Note ?? ""
                };
            }).ToList();

            webBridge.SendToWeb(new
            {
                type = "updateDemos",
                demos = demoList
            });
        }

        public void ReorderDemosWeb(System.Collections.Generic.List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return;
            string baseDir = GetDemosBaseDir();
            var newDb = new System.Collections.Generic.Dictionary<string, DemoMetadata>(StringComparer.OrdinalIgnoreCase);

            foreach (string fp in filePaths)
            {
                string relPath = "";
                if (fp.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    relPath = fp.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                }
                else
                {
                    relPath = Path.GetFileName(fp);
                }

                if (metadataDb.TryGetValue(relPath, out DemoMetadata dm))
                {
                    newDb[relPath] = dm;
                }
            }

            foreach (var kvp in metadataDb)
            {
                if (!newDb.ContainsKey(kvp.Key))
                {
                    newDb[kvp.Key] = kvp.Value;
                }
            }

            metadataDb = newDb;
            SaveEntireMetadataDb(baseDir);
        }

        public void PlayDemoByPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            filePath = filePath.Replace('/', '\\');

            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл демок не найден на диске!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                PlaySelectedFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка запуска CS2: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BrowseDownloadsPath()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите папку загрузок"
            };
            if (dialog.ShowDialog(this) == true)
            {
                settings.DownloadsPath = dialog.FolderName;
                SaveConfig();
                InitializeWatcher();
                SendAllStateToWeb();
                if (webBridge != null) webBridge.SendLog("Папка Загрузок успешно изменена: " + settings.DownloadsPath);
            }
        }

        public void AutoDetectCS2Path()
        {
            if (webBridge != null) webBridge.SendLog("[Нажата кнопка Авто] Запуск поиска...");
            string detected = ConfigManager.AutoDetectCS2Path(msg => {
                if (webBridge != null) webBridge.SendLog(msg);
            });
            if (!string.IsNullOrEmpty(detected))
            {
                settings.CS2Path = detected;
                SaveConfig();
                SendAllStateToWeb();
            }
        }

        public void BrowseCS2Path()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите папку CS2 (game/csgo)"
            };
            if (dialog.ShowDialog(this) == true)
            {
                settings.CS2Path = dialog.FolderName;
                SaveConfig();
                SendAllStateToWeb();
                if (webBridge != null) webBridge.SendLog("Папка CS2 успешно изменена: " + settings.CS2Path);
            }
        }

        public void ProcessDownloadsFolder()
        {
            try
            {
                ProcessDownloadsFolderLogic();
            }
            catch (Exception ex)
            {
                if (webBridge != null) webBridge.SendLog("Ошибка обработки Загрузок: " + ex.Message);
            }
        }

        public void CreateNewCategoryPrompt()
        {
            string newCat = ShowInputDialog("Новая категория", "Введите название новой папки категорий:");
            if (!string.IsNullOrEmpty(newCat))
            {
                string path = Path.Combine(GetDemosBaseDir(), newCat);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                SendAllStateToWeb();
            }
        }

        public void SelectCategory(string catName)
        {
            if (!string.IsNullOrEmpty(catName))
            {
                selectedCategory = catName;
                SendDemosToWeb();
            }
        }

        public void ImportSingleDroppedFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            try
            {
                ImportSingleFile(filePath);
                SendAllStateToWeb();
            }
            catch (Exception ex)
            {
                if (webBridge != null) webBridge.SendLog("Ошибка импорта: " + ex.Message);
            }
        }

        public void UpdateSettingsFromJson(JsonElement element)
        {
            if (element.TryGetProperty("downloadsPath", out JsonElement dl))
            {
                string val = dl.GetString();
                if (!string.IsNullOrEmpty(val) || string.IsNullOrEmpty(settings.DownloadsPath))
                {
                    settings.DownloadsPath = val;
                }
            }
            if (element.TryGetProperty("cs2Path", out JsonElement cs))
            {
                string val = cs.GetString();
                if (!string.IsNullOrEmpty(val) || string.IsNullOrEmpty(settings.CS2Path))
                {
                    settings.CS2Path = val;
                }
            }
            if (element.TryGetProperty("nickname", out JsonElement nick)) settings.Nickname = nick.GetString();
            if (element.TryGetProperty("watchFolder", out JsonElement wf)) settings.WatchFolder = wf.GetBoolean();
            if (element.TryGetProperty("minimizeTray", out JsonElement mt)) settings.MinimizeTray = mt.GetBoolean();
            if (element.TryGetProperty("enableDemoVoice", out JsonElement voice)) settings.EnableDemoVoice = voice.GetBoolean();
            if (element.TryGetProperty("deleteArchivesAfterUnpack", out JsonElement del)) settings.DeleteArchivesAfterUnpack = del.GetBoolean();
            if (element.TryGetProperty("importMode", out JsonElement im)) settings.ImportMode = im.GetString();
            if (element.TryGetProperty("targetImportFolder", out JsonElement tif)) settings.TargetImportFolder = tif.GetString();
            if (element.TryGetProperty("folderNicknames", out JsonElement fnProp))
            {
                settings.FolderNicknames.Clear();
                foreach (var prop in fnProp.EnumerateObject())
                {
                    settings.FolderNicknames[prop.Name] = prop.Value.GetString();
                }
            }

            SaveConfig();
            InitializeWatcher();
            FetchUserEloAsync();
            SendAllStateToWeb();
        }

        public void SetFolderNickname(string relativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(relativePath)) return;
                string currentNick = "";
                settings.FolderNicknames.TryGetValue(relativePath, out currentNick);
                string newNick = ShowInputDialog("Никнейм игрока", string.Format("Введите FACEIT никнейм для папки '{0}':", relativePath), currentNick);
                
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_nick.txt"), 
                    string.Format("Path: {0}, Current: {1}, New: {2}", relativePath, currentNick, newNick ?? "null"));

                if (newNick != null)
                {
                    if (string.IsNullOrEmpty(newNick.Trim()))
                    {
                        settings.FolderNicknames.Remove(relativePath);
                    }
                    else
                    {
                        settings.FolderNicknames[relativePath] = newNick.Trim();
                    }
                    SaveConfig();
                    SendAllStateToWeb();
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_nick_error.txt"), ex.ToString());
            }
        }

        public void UpdateBindsFromJson(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var bindsList = new System.Collections.Generic.List<DemoBind>();
                foreach (var item in element.EnumerateArray())
                {
                    var bind = new DemoBind();
                    
                    if (item.TryGetProperty("isEnabled", out JsonElement val1)) bind.IsEnabled = val1.GetBoolean();
                    else if (item.TryGetProperty("IsEnabled", out JsonElement val1P)) bind.IsEnabled = val1P.GetBoolean();

                    if (item.TryGetProperty("actionName", out JsonElement val2)) bind.ActionName = val2.GetString();
                    else if (item.TryGetProperty("ActionName", out JsonElement val2P)) bind.ActionName = val2P.GetString();

                    if (item.TryGetProperty("key", out JsonElement val3)) bind.Key = val3.GetString();
                    else if (item.TryGetProperty("Key", out JsonElement val3P)) bind.Key = val3P.GetString();

                    if (item.TryGetProperty("command", out JsonElement val4)) bind.Command = val4.GetString();
                    else if (item.TryGetProperty("Command", out JsonElement val4P)) bind.Command = val4P.GetString();

                    bindsList.Add(bind);
                }
                settings.Binds = bindsList;
            }
            SaveConfig();
        }

        public void CreateSubfolder(string parentRelativePath)
        {
            string newCat = ShowInputDialog("Новая подпапка", string.Format("Введите название новой подпапки для '{0}':", parentRelativePath));
            if (!string.IsNullOrEmpty(newCat))
            {
                string relativePath = string.IsNullOrEmpty(parentRelativePath) || parentRelativePath == "[Все демки]" 
                    ? newCat 
                    : parentRelativePath + "/" + newCat;
                string baseDir = GetDemosBaseDir();
                if (LibraryFileService.CreateFolder(baseDir, relativePath))
                {
                    SendAllStateToWeb();
                }
            }
        }

        public void RenameCategory(string oldRelativePath)
        {
            if (string.IsNullOrEmpty(oldRelativePath) || oldRelativePath == "[Все демки]" || oldRelativePath == "General") return;
            string newName = ShowInputDialog("Переименовать папку", "Введите новое название папки:");
            if (!string.IsNullOrEmpty(newName))
            {
                string parentDir = Path.GetDirectoryName(oldRelativePath.Replace('/', '\\'));
                string newRelativePath = string.IsNullOrEmpty(parentDir) ? newName : Path.Combine(parentDir, newName).Replace('\\', '/');
                string baseDir = GetDemosBaseDir();
                if (LibraryFileService.RenameFolder(baseDir, oldRelativePath, newRelativePath))
                {
                    // Update metadata keys
                    string oldRelPrefix = oldRelativePath + "/";
                    string newRelPrefix = newRelativePath + "/";
                    System.Collections.Generic.List<string> keysToUpdate = new System.Collections.Generic.List<string>();
                    foreach (string key in metadataDb.Keys)
                    {
                        if (key.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            keysToUpdate.Add(key);
                        }
                        else if (key.Equals(oldRelativePath, StringComparison.OrdinalIgnoreCase))
                        {
                            keysToUpdate.Add(key);
                        }
                    }

                    foreach (string oldKey in keysToUpdate)
                    {
                        DemoMetadata dm = metadataDb[oldKey];
                        metadataDb.Remove(oldKey);
                        string newKey = oldKey.Equals(oldRelativePath, StringComparison.OrdinalIgnoreCase) 
                            ? newRelativePath 
                            : newRelPrefix + oldKey.Substring(oldRelPrefix.Length);
                        metadataDb[newKey] = dm;
                    }

                    SaveEntireMetadataDb(baseDir);

                    if (selectedCategory == oldRelativePath) selectedCategory = newRelativePath;
                    else if (selectedCategory.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedCategory = newRelPrefix + selectedCategory.Substring(oldRelPrefix.Length);
                    }

                    SendAllStateToWeb();
                }
            }
        }

        public void DeleteCategoryWeb(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || relativePath == "[Все демки]" || relativePath == "General") return;
            bool confirm = ShowConfirmDialog("Удалить папку", string.Format("Вы действительно хотите удалить папку '{0}'? Все демо-файлы внутри будут перемещены в папку General.", relativePath));
            if (confirm)
            {
                string baseDir = GetDemosBaseDir();
                if (LibraryFileService.DeleteCategoryFolder(baseDir, relativePath, metadataDb))
                {
                    if (selectedCategory == relativePath || selectedCategory.StartsWith(relativePath + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        selectedCategory = "General";
                    }
                    SendAllStateToWeb();
                }
            }
        }

        public void ToggleFolderCollapse(string folderRelativePath)
        {
            if (string.IsNullOrEmpty(folderRelativePath)) return;
            if (collapsedFolders.Contains(folderRelativePath))
            {
                collapsedFolders.Remove(folderRelativePath);
            }
            else
            {
                collapsedFolders.Add(folderRelativePath);
            }
            SendAllStateToWeb();
        }

        public void MoveFolderWeb(string srcRelativePath, string destRelativePath)
        {
            if (srcRelativePath == destRelativePath) return;
            if (srcRelativePath == "General" || srcRelativePath == "[Все демки]") return;

            if (destRelativePath.StartsWith(srcRelativePath + "/", StringComparison.OrdinalIgnoreCase) || destRelativePath.Equals(srcRelativePath, StringComparison.OrdinalIgnoreCase))
            {
                ShowMessageDialog("Предупреждение", "Нельзя переместить папку саму в себя или в свои подпапки.", true);
                return;
            }

            string baseDir = GetDemosBaseDir();
            string srcPath = Path.Combine(baseDir, srcRelativePath);
            string folderName = Path.GetFileName(srcPath);
            string destPath = (destRelativePath == "[Все демки]" || string.IsNullOrEmpty(destRelativePath) || destRelativePath == "General") 
                ? Path.Combine(baseDir, folderName) 
                : Path.Combine(baseDir, destRelativePath, folderName);

            if (Directory.Exists(destPath))
            {
                ShowMessageDialog("Ошибка", "Папка с таким именем уже существует в месте назначения.", true);
                return;
            }

            try
            {
                Directory.Move(srcPath, destPath);
                
                string oldRelPrefix = srcRelativePath + "/";
                string newRelPrefix = (destRelativePath == "[Все демки]" || string.IsNullOrEmpty(destRelativePath) || destRelativePath == "General") 
                    ? folderName + "/" 
                    : destRelativePath + "/" + folderName + "/";

                System.Collections.Generic.List<string> keysToUpdate = new System.Collections.Generic.List<string>();
                foreach (string key in metadataDb.Keys)
                {
                    if (key.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase)) keysToUpdate.Add(key);
                }

                foreach (string oldKey in keysToUpdate)
                {
                    DemoMetadata dm = metadataDb[oldKey];
                    metadataDb.Remove(oldKey);
                    string newKey = newRelPrefix + oldKey.Substring(oldRelPrefix.Length);
                    metadataDb[newKey] = dm;
                }

                SaveEntireMetadataDb(baseDir);
                SendAllStateToWeb();
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Не удалось переместить папку: " + ex.Message, true);
            }
        }

        public void MoveDemoWeb(string filePath, string targetCategoryRelativePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            string baseDir = GetDemosBaseDir();
            string fileName = Path.GetFileName(filePath);
            string targetPath = Path.Combine(baseDir, targetCategoryRelativePath);
            string destPath = Path.Combine(targetPath, fileName);

            if (filePath.Equals(destPath, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
                if (File.Exists(destPath)) File.Delete(destPath);
                File.Move(filePath, destPath);

                string oldRel = filePath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                string newRel = destPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');

                DemoMetadata dm;
                if (metadataDb.TryGetValue(oldRel, out dm))
                {
                    metadataDb.Remove(oldRel);
                    DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, newRel, dm);
                }

                SendAllStateToWeb();
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Не удалось переместить демо: " + ex.Message, true);
            }
        }

        public void ImportFilesInto(string targetCategoryRelativePath)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Faceit Demo Files (*.dem.zst;*.dem)|*.dem.zst;*.dem";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                string cs2 = settings != null ? settings.CS2Path : "";
                string nickForImport = "";
                if (targetCategoryRelativePath == "General")
                {
                    nickForImport = settings != null ? settings.Nickname : "";
                }
                else
                {
                    nickForImport = GetFolderNicknameRecursive(targetCategoryRelativePath);
                }
                string baseDir = GetDemosBaseDir();

                Thread thread = new Thread(() =>
                {
                    foreach (string file in dialog.FileNames)
                    {
                        try
                        {
                            Action<string, bool> logCallback = (msg, isErr) => {
                                AppendLog("Процессор демок: " + msg);
                            };
                            DemoProcessor.ProcessSingleFile(file, targetCategoryRelativePath, cs2, nickForImport, metadataDb, logCallback, settings.DeleteArchivesAfterUnpack);
                        }
                        catch (Exception ex)
                        {
                            AppendLog("Ошибка обработки файла: " + ex.Message);
                        }
                    }

                    this.Dispatcher.BeginInvoke(new Action(() => {
                        SendAllStateToWeb();
                    }));
                });
                thread.IsBackground = true;
                thread.Start();
            }
        }

        public string GetFolderNicknameRecursive(string category)
        {
            if (string.IsNullOrEmpty(category) || category.Equals("General", StringComparison.OrdinalIgnoreCase))
                return "";

            if (settings.FolderNicknames != null)
            {
                string normalizedCat = category.Replace('\\', '/');
                if (settings.FolderNicknames.TryGetValue(normalizedCat, out string nick) && !string.IsNullOrEmpty(nick))
                {
                    return nick;
                }

                string currentPath = normalizedCat;
                while (currentPath.Contains("/"))
                {
                    int lastSlash = currentPath.LastIndexOf('/');
                    currentPath = currentPath.Substring(0, lastSlash);
                    if (settings.FolderNicknames.TryGetValue(currentPath, out nick) && !string.IsNullOrEmpty(nick))
                    {
                        return nick;
                    }
                }
            }
            return "";
        }

        public void BrowseDemosManual()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Faceit Demo Files (*.dem.zst;*.dem)|*.dem.zst;*.dem";
            dialog.Multiselect = true;
            if (dialog.ShowDialog(this) == true)
            {
                ProcessManualFiles(dialog.FileNames);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public void DragWindowNative()
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(this);
                    ReleaseCapture();
                    SendMessage(helper.Handle, WM_NCLBUTTONDOWN, new IntPtr(HT_CAPTION), IntPtr.Zero);
                });
            }
            catch { }
        }

        public void ResetBindsToDefault()
        {
            settings.Binds = ConfigManager.GetDefaultBinds();
            SaveConfig();
            SendAllStateToWeb();
        }

        public void SaveDemoMetadataWeb(string filePath, string map, string score, string kd, string date, string note)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            string baseDir = GetDemosBaseDir();
            string relPath = "";
            if (filePath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                relPath = filePath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
            }
            else
            {
                relPath = Path.GetFileName(filePath);
            }

            DemoMetadata dm = new DemoMetadata
            {
                Map = map,
                Score = score,
                KD = kd,
                Date = date,
                Note = note
            };

            DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, relPath, dm);
            
            // Re-send updated list to web UI
            SendDemosToWeb();
        }

        public void CopyDemoConfigToClipboard(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            filePath = filePath.Replace('/', '\\');

            if (!File.Exists(filePath)) return;

            // Always copy command to clipboard for easy manual play
            string playCmd = "playdemo faceit_demos/General/faceit.dem";
            if (settings.EnableDemoVoice)
            {
                playCmd += "; tv_listen_voice_indices -1; tv_listen_voice_indices_h -1";
            }

            // Append active CS2 binds to clipboard
            if (settings.AutoApplyBinds && settings.Binds != null)
            {
                foreach (var b in settings.Binds)
                {
                    if (b.IsEnabled && !string.IsNullOrEmpty(b.Key) && !string.IsNullOrEmpty(b.Command))
                    {
                        playCmd += string.Format("; bind \"{0}\" \"{1}\"", b.Key, b.Command);
                    }
                }
            }

            try
            {
                this.Dispatcher.Invoke(() => {
                    Clipboard.SetText(playCmd);
                });
                AppendLog("Конфиг для запуска скопирован в буфер обмена!");
            }
            catch (Exception ex)
            {
                AppendLog("Не удалось скопировать в буфер обмена: " + ex.Message);
            }

            // Try to copy file to General/faceit.dem, but don't fail config copy if it is locked
            string baseDir = GetDemosBaseDir();
            string destPath = Path.Combine(Path.Combine(baseDir, "General"), "faceit.dem");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(filePath, destPath, true);
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("used by another process") || ioEx.Message.Contains("занят другим процессом"))
            {
                AppendLog("Предупреждение: faceit.dem занят игрой CS2. Новая демка будет доступна после ввода disconnect/выхода из игры.");
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка копирования файла демки: " + ex.Message);
            }
        }

        public void DeleteDemosWeb(System.Collections.Generic.List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return;

            bool mr = ShowConfirmDialog("Удаление файлов", string.Format("Вы уверены, что хотите навсегда удалить выбранные демки ({0} шт.)?", filePaths.Count));
            if (!mr) return;

            string baseDir = GetDemosBaseDir();
            int deletedCount = 0;

            foreach (var filePath in filePaths)
            {
                if (string.IsNullOrEmpty(filePath)) continue;
                string cleanPath = filePath.Replace('/', '\\');
                if (!File.Exists(cleanPath)) continue;

                try
                {
                    File.Delete(cleanPath);
                    string relPath = cleanPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    if (metadataDb.ContainsKey(relPath))
                    {
                        metadataDb.Remove(relPath);
                    }
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    ShowMessageDialog("Ошибка", string.Format("Не удалось удалить файл {0}: {1}", Path.GetFileName(cleanPath), ex.Message), true);
                }
            }

            if (deletedCount > 0)
            {
                SaveEntireMetadataDb(baseDir);
                SendAllStateToWeb();
                AppendLog(string.Format("Успешно удалено файлов: {0}", deletedCount));
            }
        }

        public void MoveSelectedDemosWebPrompt(System.Collections.Generic.List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                ShowMessageDialog("Информация", "Выберите хотя бы одну демку для переноса.");
                return;
            }

            string target = ShowSelectFolderDialog("Куда перенести выбранные демки?");
            if (string.IsNullOrEmpty(target)) return;

            MoveDemosWeb(filePaths, target);
        }

        public void MoveDemosWeb(System.Collections.Generic.List<string> filePaths, string targetCategoryRelativePath)
        {
            if (filePaths == null || filePaths.Count == 0) return;
            string baseDir = GetDemosBaseDir();
            if (string.IsNullOrEmpty(baseDir)) return;

            string targetPath = Path.Combine(baseDir, targetCategoryRelativePath);
            if (!Directory.Exists(targetPath))
            {
                try { Directory.CreateDirectory(targetPath); } catch { }
            }

            int movedCount = 0;
            foreach (var filePath in filePaths)
            {
                if (string.IsNullOrEmpty(filePath)) continue;
                string cleanPath = filePath.Replace('/', '\\');
                if (!File.Exists(cleanPath)) continue;

                string fileName = Path.GetFileName(cleanPath);
                string destPath = Path.Combine(targetPath, fileName);

                if (cleanPath.Equals(destPath, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    if (File.Exists(destPath)) File.Delete(destPath);
                    File.Move(cleanPath, destPath);

                    string oldRel = cleanPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    string newRel = destPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');

                    DemoMetadata dm;
                    if (metadataDb.TryGetValue(oldRel, out dm))
                    {
                        metadataDb.Remove(oldRel);
                        DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, newRel, dm);
                    }
                    movedCount++;
                }
                catch (Exception ex)
                {
                    ShowMessageDialog("Ошибка", string.Format("Не удалось перенести демо {0}: {1}", fileName, ex.Message), true);
                }
            }

            if (movedCount > 0)
            {
                SendAllStateToWeb();
                AppendLog(string.Format("Успешно перемещено файлов: {0}", movedCount));
            }
        }
    }
}
