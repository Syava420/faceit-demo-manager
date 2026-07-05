using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        // Navigation Buttons
        private Button tabImport;
        private Button tabLibrary;
        private Button tabBinds;
        private Grid sidebarLibraryControls;

        // Tab 1: Import Controls
        private Grid gridImportTab;
        private Border dragDropZone;
        private TextBox txtLogConsole;
        private TextBox txtDownloads;
        private TextBox txtCS2;
        private TextBox txtNickname;
        private CheckBox ChkWatchFolder;
        private CheckBox ChkTray;
        private CheckBox chkVoiceInDemos;
        private ComboBox CboImportMode;
        private ComboBox CboImportFolder;
        private Button btnBrowseDownloads;
        private Button btnAutoCS2;
        private Button btnBrowseCS2;
        private Button btnProcess;

        // Tab 2: Library Controls
        private Grid gridLibraryTab;
        private ListBox lstFolders;
        private TextBox txtSearch;
        private DataGrid dgvDemos;
        
        // Match Editing Fields
        private TextBox txtEditMap;
        private TextBox txtEditScore;
        private TextBox txtEditKD;
        private TextBox txtEditDate;
        private TextBox txtNoteEdit;
        
        // Library Actions
        private Button btnNewCategory;
        private Button btnDeleteCategory;
        private Button btnPlay;
        private Button btnMoveDemo;
        private Button btnDeleteDemo;

        // Tab 3: Binds Controls
        private Grid gridBindsTab;
        private DataGrid dgvBinds;
        private CheckBox chkAutoApplyBinds;
        private Button btnResetBinds;

        // Footer / Progress
        private TextBlock lblStatus;
        private ProgressBar prgBar;

        // App state
        private string configPath;
        private AppSettings settings;
        private FileSystemWatcher dlWatcher;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private Dictionary<string, DemoMetadata> metadataDb = new Dictionary<string, DemoMetadata>(StringComparer.OrdinalIgnoreCase);
        private bool isUpdatingFields = false;
        private int userLevel = 0;
        private int userElo = 0;

        public MainWindow()
        {
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            InitializeWindow();
            LoadConfig();
            RefreshFolders();
            UpdateImportFolderCombobox();
            InitializeWatcher();
            FetchUserEloAsync();
        }

        private void InitializeWindow()
        {
            this.Title = "FACEIT Demo Hub";
            this.Width = 1000;
            this.Height = 650;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Border root = (Border)XamlReader.Parse(GetXamlString());
            this.Content = root;

            // Resolve controls from XAML
            tabImport = (Button)root.FindName("TabImport");
            tabLibrary = (Button)root.FindName("TabLibrary");
            tabBinds = (Button)root.FindName("TabBinds");
            sidebarLibraryControls = (Grid)root.FindName("SidebarLibraryControls");

            gridImportTab = (Grid)root.FindName("GridImportTab");
            dragDropZone = (Border)root.FindName("DragDropZone");
            txtLogConsole = (TextBox)root.FindName("TxtLogConsole");
            txtDownloads = (TextBox)root.FindName("TxtDownloads");
            txtCS2 = (TextBox)root.FindName("TxtCS2");
            txtNickname = (TextBox)root.FindName("TxtNickname");
            ChkWatchFolder = (CheckBox)root.FindName("ChkWatchFolder");
            ChkTray = (CheckBox)root.FindName("ChkTray");
            chkVoiceInDemos = (CheckBox)root.FindName("ChkVoiceInDemos");
            CboImportMode = (ComboBox)root.FindName("CboImportMode");
            CboImportFolder = (ComboBox)root.FindName("CboImportFolder");
            btnBrowseDownloads = (Button)root.FindName("BtnBrowseDownloads");
            btnAutoCS2 = (Button)root.FindName("BtnAutoCS2");
            btnBrowseCS2 = (Button)root.FindName("BtnBrowseCS2");
            btnProcess = (Button)root.FindName("BtnProcess");

            gridLibraryTab = (Grid)root.FindName("GridLibraryTab");
            lstFolders = (ListBox)root.FindName("LstFolders");
            txtSearch = (TextBox)root.FindName("TxtSearch");
            dgvDemos = (DataGrid)root.FindName("DgvDemos");

            txtEditMap = (TextBox)root.FindName("TxtEditMap");
            txtEditScore = (TextBox)root.FindName("TxtEditScore");
            txtEditKD = (TextBox)root.FindName("TxtEditKD");
            txtEditDate = (TextBox)root.FindName("TxtEditDate");
            txtNoteEdit = (TextBox)root.FindName("TxtNoteEdit");

            btnNewCategory = (Button)root.FindName("BtnNewCategory");
            btnDeleteCategory = (Button)root.FindName("BtnDeleteCategory");
            btnPlay = (Button)root.FindName("BtnPlay");
            btnMoveDemo = (Button)root.FindName("BtnMoveDemo");
            btnDeleteDemo = (Button)root.FindName("BtnDeleteDemo");

            gridBindsTab = (Grid)root.FindName("GridBindsTab");
            dgvBinds = (DataGrid)root.FindName("DgvBinds");
            chkAutoApplyBinds = (CheckBox)root.FindName("ChkAutoApplyBinds");
            btnResetBinds = (Button)root.FindName("BtnResetBinds");

            lblStatus = (TextBlock)root.FindName("LblStatus");
            prgBar = (ProgressBar)root.FindName("PrgBar");

            // Event Bindings
            this.Closing += MainWindow_Closing;
            
            // TitleBar Drag
            Grid titleBar = (Grid)root.FindName("TitleBar");
            titleBar.MouseLeftButtonDown += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
            };

            Button btnMin = (Button)root.FindName("BtnMinimize");
            Button btnClose = (Button)root.FindName("BtnClose");
            btnMin.Click += (s, e) => this.WindowState = WindowState.Minimized;
            btnClose.Click += (s, e) => this.Close();

            // Tab Switching
            tabImport.Click += (s, e) => SwitchTab(0);
            tabLibrary.Click += (s, e) => SwitchTab(1);
            tabBinds.Click += (s, e) => SwitchTab(2);

            // Browse Buttons
            btnBrowseDownloads.Click += (s, e) => BrowseFolder("Выберите папку загрузок", txtDownloads);
            
            // CS2 Browse and Auto-detect
            btnBrowseCS2.Click += (s, e) => BrowseFolder("Выберите папку CS2 (game\\csgo)", txtCS2);
            btnAutoCS2.Click += (s, e) => {
                string detected = ConfigManager.AutoDetectCS2Path();
                if (!string.IsNullOrEmpty(detected))
                {
                    txtCS2.Text = detected;
                    SaveConfig();
                    ShowMessageDialog("Авто-поиск", "Папка CS2 успешно обнаружена:\n" + detected);
                }
                else
                {
                    ShowMessageDialog("Авто-поиск", "Не удалось автоматически найти папку CS2. Пожалуйста, укажите её вручную с помощью кнопки '...'.", true);
                }
            };

            // Drag and Drop
            dragDropZone.Drop += MainWindow_Drop;
            dragDropZone.DragOver += (s, e) => e.Effects = DragDropEffects.Copy;
            dragDropZone.MouseLeftButtonDown += DragDropZone_MouseLeftButtonDown;

            // Scan / Watcher triggers
            btnProcess.Click += BtnProcess_Click;
            ChkWatchFolder.Checked += (s, e) => ToggleWatcherSetting(true);
            ChkWatchFolder.Unchecked += (s, e) => ToggleWatcherSetting(false);
            
            // Voice Setting triggers
            chkVoiceInDemos.Checked += (s, e) => SaveConfig();
            chkVoiceInDemos.Unchecked += (s, e) => SaveConfig();

            // Auto-apply binds triggers
            chkAutoApplyBinds.Checked += (s, e) => SaveConfig();
            chkAutoApplyBinds.Unchecked += (s, e) => SaveConfig();

            // Reset binds button
            btnResetBinds.Click += BtnResetBinds_Click;

            // Library search & list selection
            txtSearch.TextChanged += (s, e) => RefreshDemoList();
            
            // Setup folder selection text updates contextually
            lstFolders.SelectionChanged += (s, e) => {
                UpdateNicknameInput();
                RefreshDemoList();
            };

            // Nickname contextual text edit
            txtNickname.TextChanged += (s, e) => {
                if (isUpdatingNickname || settings == null) return;
                string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
                if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Все демки]")
                {
                    settings.Nickname = txtNickname.Text.Trim();
                }
                else
                {
                    if (settings.FolderNicknames == null)
                    {
                        settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    settings.FolderNicknames[selectedFolder] = txtNickname.Text.Trim();
                }
                ConfigManager.Save(configPath, settings);
            };

            btnNewCategory.Click += BtnNewCategory_Click;
            btnDeleteCategory.Click += BtnDeleteCategory_Click;
            btnPlay.Click += BtnPlay_Click;
            btnMoveDemo.Click += BtnMoveDemo_Click;
            btnDeleteDemo.Click += BtnDeleteDemo_Click;

            dgvDemos.SelectionChanged += DgvDemos_SelectionChanged;
            txtEditMap.TextChanged += EditField_TextChanged;
            txtEditScore.TextChanged += EditField_TextChanged;
            txtEditKD.TextChanged += EditField_TextChanged;
            txtEditDate.TextChanged += EditField_TextChanged;
            txtNoteEdit.TextChanged += EditField_TextChanged;

            CboImportMode.SelectionChanged += CboImportMode_SelectionChanged;

            // Setup double-click in DataGrid to play demo
            dgvDemos.MouseDoubleClick += DgvDemos_MouseDoubleClick;

            // Setup Drag & Drop from DataGrid to side Folders
            dgvDemos.PreviewMouseLeftButtonDown += DgvDemos_PreviewMouseLeftButtonDown;
            dgvDemos.MouseMove += DgvDemos_MouseMove;
            lstFolders.DragOver += LstFolders_DragOver;
            lstFolders.Drop += LstFolders_Drop;
        }

        private void SwitchTab(int tabIndex)
        {
            // Reset backgrounds & foregrounds
            tabImport.Background = Brushes.Transparent;
            tabImport.Foreground = Brushes.Gray;
            tabLibrary.Background = Brushes.Transparent;
            tabLibrary.Foreground = Brushes.Gray;
            tabBinds.Background = Brushes.Transparent;
            tabBinds.Foreground = Brushes.Gray;

            gridImportTab.Visibility = Visibility.Collapsed;
            gridLibraryTab.Visibility = Visibility.Collapsed;
            gridBindsTab.Visibility = Visibility.Collapsed;
            sidebarLibraryControls.Visibility = Visibility.Collapsed;

            var root = (Border)this.Content;
            var catHeader = (TextBlock)root.FindName("SidebarCategoriesHeader");
            if (catHeader != null) catHeader.Visibility = Visibility.Collapsed;

            if (tabIndex == 0) // Import
            {
                tabImport.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6"));
                tabImport.Foreground = Brushes.White;
                gridImportTab.Visibility = Visibility.Visible;
            }
            else if (tabIndex == 1) // Library
            {
                tabLibrary.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6"));
                tabLibrary.Foreground = Brushes.White;
                gridLibraryTab.Visibility = Visibility.Visible;
                sidebarLibraryControls.Visibility = Visibility.Visible;
                if (catHeader != null) catHeader.Visibility = Visibility.Visible;
                RefreshFolders();
            }
            else if (tabIndex == 2) // Binds
            {
                tabBinds.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6"));
                tabBinds.Foreground = Brushes.White;
                gridBindsTab.Visibility = Visibility.Visible;
                
                // Refresh binds list to reflect updates
                dgvBinds.ItemsSource = null;
                dgvBinds.ItemsSource = settings.Binds;
            }
        }

        private void AppendLog(string message)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                txtLogConsole.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
                txtLogConsole.ScrollToEnd();
            }));
        }

        private void BrowseFolder(string title, TextBox tb)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = title;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tb.Text = dialog.SelectedPath;
                    RefreshFolders();
                    UpdateImportFolderCombobox();
                }
            }
        }

        private void DragDropZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Faceit Demo Files (*.dem.zst;*.dem)|*.dem.zst;*.dem";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                ProcessManualFiles(dialog.FileNames);
            }
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ProcessManualFiles(files);
            }
        }

        private string DetermineImportFolder(string fileName)
        {
            string mode = "General";
            string specificFolder = "General";

            this.Dispatcher.Invoke(new Action(() => {
                var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
                if (selectedItem != null) mode = selectedItem.Tag.ToString();
                specificFolder = CboImportFolder.SelectedItem != null ? CboImportFolder.SelectedItem.ToString() : "General";
            }));

            if (mode == "General") return "General";
            if (mode == "Specific") return specificFolder;

            // Ask mode (Interactive)
            string chosen = this.Dispatcher.Invoke(() => ShowSelectFolderDialog("Куда распаковать демку: " + Path.GetFileName(fileName)));
            if (string.IsNullOrEmpty(chosen)) return null; // Abort
            return chosen;
        }

        private void ProcessManualFiles(string[] files)
        {
            SwitchTab(0);
            
            string cs2 = txtCS2.Text.Trim();

            btnProcess.IsEnabled = false;
            AppendLog("Начало обработки файлов...");

            Thread thread = new Thread(() =>
            {
                int successCount = 0;
                int totalCount = 0;
                foreach (string file in files)
                {
                    if (file.EndsWith(".zst", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".dem", StringComparison.OrdinalIgnoreCase))
                    {
                        totalCount++;
                        string targetCategory = DetermineImportFolder(file);
                        if (targetCategory == null)
                        {
                            AppendLog("Пропущен файл (отмена выбора папки): " + Path.GetFileName(file));
                            continue;
                        }

                        // Determine nickname contextually
                        string nickForImport = settings.Nickname;
                        string folderNick;
                        if (!string.IsNullOrEmpty(targetCategory) && settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue(targetCategory, out folderNick) && !string.IsNullOrEmpty(folderNick))
                        {
                            nickForImport = folderNick;
                        }

                        try
                        {
                            Action<string, bool> logCallback = (msg, isErr) => {
                                AppendLog("Процессор демок: " + msg);
                            };
                            if (DemoProcessor.ProcessSingleFile(file, targetCategory, cs2, nickForImport, metadataDb, logCallback))
                            {
                                successCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendLog("Ошибка обработки файла " + Path.GetFileName(file) + ": " + ex.Message);
                        }
                    }
                }

                this.Dispatcher.BeginInvoke(new Action(() => {
                    btnProcess.IsEnabled = true;
                    AppendLog(string.Format("Обработка завершена. Успешно импортировано: {0}/{1}", successCount, totalCount));
                    RefreshFolders();
                    RefreshDemoList();
                }));
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            string downloads = txtDownloads.Text.Trim();
            if (!Directory.Exists(downloads))
            {
                AppendLog("Ошибка: Неверный путь к папке загрузок!");
                return;
            }

            string[] files = Directory.GetFiles(downloads, "*.dem.zst");
            if (files.Length == 0)
            {
                AppendLog("В папке загрузок не найдено новых файлов *.dem.zst");
                return;
            }

            ProcessManualFiles(files);
        }

        private void ToggleWatcherSetting(bool enabled)
        {
            if (dlWatcher != null)
            {
                dlWatcher.EnableRaisingEvents = enabled;
            }
        }

        private void InitializeWatcher()
        {
            try
            {
                string downloads = txtDownloads.Text.Trim();
                if (Directory.Exists(downloads))
                {
                    dlWatcher = new FileSystemWatcher(downloads, "*.dem.zst");
                    dlWatcher.Created += DlWatcher_Created;
                    dlWatcher.EnableRaisingEvents = ChkWatchFolder.IsChecked == true;
                }
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка авто-сканера: " + ex.Message);
            }
        }

        private void DlWatcher_Created(object sender, FileSystemEventArgs e)
        {
            AppendLog("Обнаружен файл загрузки: " + e.Name);
            Thread.Sleep(1000); // Wait for file handle to release
            
            string cs2 = "";
            this.Dispatcher.Invoke(new Action(() => {
                cs2 = txtCS2.Text.Trim();
            }));

            string targetCategory = DetermineImportFolder(e.FullPath);
            if (targetCategory == null)
            {
                AppendLog("Отмена авто-импорта (папка не выбрана): " + e.Name);
                return;
            }

            // Determine nickname contextually for auto-scanned import
            string nickForImport = settings.Nickname;
            string folderNick;
            if (!string.IsNullOrEmpty(targetCategory) && settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue(targetCategory, out folderNick) && !string.IsNullOrEmpty(folderNick))
            {
                nickForImport = folderNick;
            }

            try
            {
                Action<string, bool> logCallback = (msg, isErr) => {
                    AppendLog("Авто-процессор: " + msg);
                };
                DemoProcessor.ProcessSingleFile(e.FullPath, targetCategory, cs2, nickForImport, metadataDb, logCallback);
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка авто-процессора: " + ex.Message);
            }
        }

        private void RefreshFolders()
        {
            if (lstFolders == null) return;
            string selected = lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;

            lstFolders.Items.Clear();
            lstFolders.Items.Add("[Все демки]");
            
            string baseDir = GetDemosBaseDir();
            if (Directory.Exists(baseDir))
            {
                string genDir = Path.Combine(baseDir, "General");
                if (!Directory.Exists(genDir))
                {
                    try { Directory.CreateDirectory(genDir); } catch { }
                }

                foreach (string d in Directory.GetDirectories(baseDir))
                {
                    string name = Path.GetFileName(d);
                    lstFolders.Items.Add(name);
                }
            }

            if (selected != null && lstFolders.Items.Contains(selected))
            {
                lstFolders.SelectedItem = selected;
            }
            else
            {
                lstFolders.SelectedIndex = 0;
            }
        }

        private void UpdateImportFolderCombobox()
        {
            if (CboImportFolder == null) return;
            string selected = CboImportFolder.SelectedItem != null ? CboImportFolder.SelectedItem.ToString() : null;
            CboImportFolder.Items.Clear();
            CboImportFolder.Items.Add("General");

            string baseDir = GetDemosBaseDir();
            if (Directory.Exists(baseDir))
            {
                string[] subdirs = Directory.GetDirectories(baseDir);
                foreach (string subdir in subdirs)
                {
                    string name = Path.GetFileName(subdir);
                    if (!name.Equals("General", StringComparison.OrdinalIgnoreCase))
                    {
                        CboImportFolder.Items.Add(name);
                    }
                }
            }

            if (selected != null && CboImportFolder.Items.Contains(selected))
            {
                CboImportFolder.SelectedItem = selected;
            }
            else
            {
                CboImportFolder.SelectedIndex = 0;
            }
        }

        private void FetchUserEloAsync()
        {
            string nickname = "";
            this.Dispatcher.Invoke(new Action(() => {
                nickname = txtNickname.Text.Trim();
            }));

            if (string.IsNullOrEmpty(nickname))
            {
                userLevel = 0;
                userElo = 0;
                return;
            }

            Thread thread = new Thread(() =>
            {
                int lvl = 0;
                int elo = 0;
                try
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FaceitDemoHub");
                        string json = wc.DownloadString("https://api.faceit.com/users/v1/nicknames/" + nickname);
                        Match mLevel = Regex.Match(json, @"""skill_level""\s*:\s*(\d+)");
                        Match mElo = Regex.Match(json, @"""faceit_elo""\s*:\s*(\d+)");
                        if (mLevel.Success) lvl = int.Parse(mLevel.Groups[1].Value);
                        if (mElo.Success) elo = int.Parse(mElo.Groups[1].Value);
                    }
                }
                catch { }

                this.userLevel = lvl;
                this.userElo = elo;

                this.Dispatcher.Invoke(new Action(() => {
                    RefreshDemoList();
                }));
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void RefreshDemoList()
        {
            if (dgvDemos == null) return;
            dgvDemos.ItemsSource = null;

            string selectedFolder = lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (selectedFolder == null) return;

            if (selectedFolder == "[Все демки]") selectedFolder = "[All Demos]";

            string baseDir = GetDemosBaseDir();
            if (!Directory.Exists(baseDir)) return;

            DemoProcessor.LoadMetadataDb(baseDir, metadataDb);

            List<string> demoFiles = new List<string>();
            if (selectedFolder == "[All Demos]")
            {
                demoFiles.AddRange(Directory.GetFiles(baseDir, "*.dem", SearchOption.AllDirectories));
            }
            else
            {
                string targetDir = Path.Combine(baseDir, selectedFolder);
                if (Directory.Exists(targetDir))
                {
                    demoFiles.AddRange(Directory.GetFiles(targetDir, "*.dem"));
                }
            }

            string filter = txtSearch.Text.Trim();
            List<DemoGridRow> gridList = new List<DemoGridRow>();

            foreach (string file in demoFiles)
            {
                string fileName = Path.GetFileName(file);
                if (fileName.Equals("faceit.dem", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Skip layout demo
                }
                string relativePath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                string folderName = Path.GetFileName(Path.GetDirectoryName(file));

                DemoMetadata dm = null;
                if (!metadataDb.TryGetValue(relativePath, out dm))
                {
                    dm = DemoProcessor.ParseMetadataFromFilename(fileName);
                }

                if (!string.IsNullOrEmpty(filter))
                {
                    bool match = (dm.Map != null && dm.Map.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (dm.Note != null && dm.Note.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (dm.Date != null && dm.Date.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (folderName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!match) continue;
                }

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
                    Match mRatio = Regex.Match(dm.KD, @"^([\d.]+)");
                    if (mRatio.Success)
                    {
                        kdRatioText = mRatio.Groups[1].Value;
                        double ratioVal;
                        if (double.TryParse(kdRatioText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ratioVal))
                        {
                            kdStatusText = ratioVal >= 1.0 ? "High" : "Low";
                        }
                    }

                    Match mKda = Regex.Match(dm.KD, @"\(([^()]+)\)");
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

                    Match mAdrVal = Regex.Match(dm.KD, @"\[([^[\]]+)\]");
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

                gridList.Add(new DemoGridRow()
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
                    Folder = folderName.Equals("General", StringComparison.OrdinalIgnoreCase) ? "Общая" : folderName,
                    Note = dm.Note,
                    FilePath = file
                });
            }

            dgvDemos.ItemsSource = gridList;
        }

        private string GetMapEmoji(string map)
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

        private void BtnNewCategory_Click(object sender, RoutedEventArgs e)
        {
            string input = ShowInputDialog("Новая папка", "Введите имя новой папки:");
            if (string.IsNullOrEmpty(input)) return;

            string name = Regex.Replace(input, @"[\\/:*?""<>|]", "").Trim();
            if (string.IsNullOrEmpty(name)) return;

            string baseDir = GetDemosBaseDir();
            if (string.IsNullOrEmpty(baseDir))
            {
                ShowMessageDialog("Ошибка", "Ошибка: проверьте путь к CS2.", true);
                return;
            }

            string targetDir = Path.Combine(baseDir, name);
            try
            {
                Directory.CreateDirectory(targetDir);
                RefreshFolders();
                UpdateImportFolderCombobox();
                lstFolders.SelectedItem = name;
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Не удалось создать папку: " + ex.Message, true);
            }
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            string current = lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (string.IsNullOrEmpty(current) || current == "[Все демки]" || current == "General")
            {
                ShowMessageDialog("Предупреждение", "Эту папку нельзя удалить.", true);
                return;
            }

            bool mr = ShowConfirmDialog("Удалить папку", string.Format("Вы действительно хотите удалить папку '{0}'? Демки внутри нее останутся на диске.", current));
            if (!mr) return;

            string baseDir = GetDemosBaseDir();
            string targetDir = Path.Combine(baseDir, current);
            string genDir = Path.Combine(baseDir, "General");

            try
            {
                if (Directory.Exists(targetDir))
                {
                    // Move files to General folder to avoid deleting demos
                    foreach (string file in Directory.GetFiles(targetDir, "*.dem"))
                    {
                        string dest = Path.Combine(genDir, Path.GetFileName(file));
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(file, dest);
                        
                        // Update metadata path
                        string oldRel = current + "/" + Path.GetFileName(file);
                        string newRel = "General/" + Path.GetFileName(file);
                        DemoMetadata dm;
                        if (metadataDb.TryGetValue(oldRel, out dm))
                        {
                            metadataDb.Remove(oldRel);
                            DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, newRel, dm);
                        }
                    }
                    Directory.Delete(targetDir, true);
                }

                RefreshFolders();
                UpdateImportFolderCombobox();
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Ошибка при удалении: " + ex.Message, true);
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void DgvDemos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaySelected();
        }

        private Point dragStartPoint;
        private void DgvDemos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
        }

        private void DgvDemos_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (dgvDemos.SelectedItems.Count > 0)
                    {
                        List<DemoGridRow> selectedRows = new List<DemoGridRow>();
                        foreach (var item in dgvDemos.SelectedItems)
                        {
                            selectedRows.Add((DemoGridRow)item);
                        }
                        
                        DataObject data = new DataObject("DemoGridRows", selectedRows);
                        DragDrop.DoDragDrop(dgvDemos, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void LstFolders_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("DemoGridRows"))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void LstFolders_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("DemoGridRows"))
            {
                var targetItem = e.Source as FrameworkElement;
                string targetFolder = null;
                
                // Traverse visual tree to find ListBoxItem
                DependencyObject parent = targetItem;
                while (parent != null && !(parent is ListBoxItem))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                
                if (parent is ListBoxItem)
                {
                    targetFolder = ((ListBoxItem)parent).DataContext as string;
                }
                
                if (string.IsNullOrEmpty(targetFolder))
                {
                    // If dropped directly on empty ListBox space, fallback to hovered item
                    ListBoxItem item = lstFolders.InputHitTest(e.GetPosition(lstFolders)) as ListBoxItem;
                    if (item != null) targetFolder = item.DataContext as string;
                }
                
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    if (targetFolder == "[Все демки]")
                    {
                        ShowMessageDialog("Предупреждение", "Демки нельзя перетащить в общую категорию всех демок.", true);
                        return;
                    }
                    
                    var rows = e.Data.GetData("DemoGridRows") as List<DemoGridRow>;
                    if (rows != null && rows.Count > 0)
                    {
                        MoveSelectedDemos(rows, targetFolder);
                    }
                }
            }
        }

        private void MoveSelectedDemos(List<DemoGridRow> rows, string targetFolder)
        {
            string baseDir = GetDemosBaseDir();
            if (string.IsNullOrEmpty(baseDir)) return;

            string targetPath = Path.Combine(baseDir, targetFolder);
            if (!Directory.Exists(targetPath)) return;

            int movedCount = 0;
            foreach (var row in rows)
            {
                string oldPath = row.FilePath;
                string newPath = Path.Combine(targetPath, Path.GetFileName(oldPath));
                
                if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    if (File.Exists(newPath)) File.Delete(newPath);
                    File.Move(oldPath, newPath);

                    // Update metadata file
                    string oldRel = oldPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    string newRel = newPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    
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
                    ShowMessageDialog("Ошибка", string.Format("Не удалось перенести демо {0}: {1}", Path.GetFileName(oldPath), ex.Message), true);
                }
            }

            if (movedCount > 0)
            {
                RefreshDemoList();
                lblStatus.Text = string.Format("Перемещено демок: {0}", movedCount);
            }
        }

        private void PlaySelected()
        {
            if (dgvDemos.SelectedItem == null)
            {
                ShowMessageDialog("Информация", "Выберите демо-файл из списка.");
                return;
            }

            DemoGridRow row = (DemoGridRow)dgvDemos.SelectedItem;
            string file = row.FilePath;

            string cs2Dir = txtCS2.Text.Trim();
            string cs2Exe = Path.Combine(Path.GetDirectoryName(cs2Dir), "bin", "win64", "cs2.exe");

            if (!File.Exists(cs2Exe))
            {
                ShowMessageDialog("Ошибка CS2", "Не найден cs2.exe по пути: " + cs2Exe, true);
                return;
            }

            string baseDir = GetDemosBaseDir();
            // Copy selected demo to target faceit.dem in General
            string destPath = Path.Combine(Path.Combine(baseDir, "General"), "faceit.dem");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(file, destPath, true);
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Ошибка подготовки запуска: " + ex.Message, true);
                return;
            }

            // Always copy command to clipboard for easy manual play
            string playCmd = "playdemo faceit_demos/General/faceit.dem";
            if (settings.EnableDemoVoice)
            {
                playCmd += "; tv_listen_voice_indices -1; tv_listen_voice_indices_h -1";
            }

            // Append active CS2 binds to clipboard
            string bindArgs = "";
            if (settings.AutoApplyBinds && settings.Binds != null)
            {
                foreach (var b in settings.Binds)
                {
                    if (b.IsEnabled && !string.IsNullOrEmpty(b.Key) && !string.IsNullOrEmpty(b.Command))
                    {
                        playCmd += string.Format("; bind \"{0}\" \"{1}\"", b.Key, b.Command);
                        bindArgs += string.Format(" +bind \"{0}\" \"{1}\"", b.Key, b.Command.Replace("\"", "\\\""));
                    }
                }
            }

            try
            {
                Clipboard.SetText(playCmd);
            }
            catch { }

            // Check if CS2 is already running
            bool isRunning = false;
            try
            {
                Process[] pname = Process.GetProcessesByName("cs2");
                if (pname.Length > 0) isRunning = true;
            }
            catch { }

            if (isRunning)
            {
                lblStatus.Text = "Команда скопирована для запущенной CS2";
                ShowMessageDialog("Игра уже запущена", "Демка готова! Команда для запуска скопирована в буфер обмена:\n\n" + playCmd + "\n\nПросто откройте консоль в CS2 (~) и нажмите Ctrl+V.");
                return;
            }

            // Try to find Steam directory to launch via Steam client to avoid connection issues
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

            string steamExe = null;
            if (!string.IsNullOrEmpty(steamPath))
            {
                string testPath = Path.Combine(steamPath, "steam.exe");
                if (File.Exists(testPath)) steamExe = testPath;
            }

            if (string.IsNullOrEmpty(steamExe))
            {
                string[] commonPaths = new string[] {
                    @"C:\Program Files (x86)\Steam\steam.exe",
                    @"C:\Program Files\Steam\steam.exe",
                    @"D:\Steam\steam.exe",
                    @"E:\Steam\steam.exe"
                };
                foreach (string p in commonPaths)
                {
                    if (File.Exists(p)) { steamExe = p; break; }
                }
            }

            string voiceArgs = settings.EnableDemoVoice ? " +tv_listen_voice_indices -1 +tv_listen_voice_indices_h -1" : "";
            string launchArgs = voiceArgs + bindArgs;

            try
            {
                if (!string.IsNullOrEmpty(steamExe))
                {
                    string steamArgs = "-applaunch 730 -steam -game csgo +playdemo faceit_demos/General/faceit.dem" + launchArgs;
                    Process.Start(steamExe, steamArgs);
                    lblStatus.Text = "Запуск CS2 (через Steam): " + Path.GetFileName(file);
                }
                else
                {
                    string args = "-steam -game csgo +playdemo faceit_demos/General/faceit.dem" + launchArgs;
                    Process.Start(cs2Exe, args);
                    lblStatus.Text = "Запуск CS2 (напрямую): " + Path.GetFileName(file);
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Ошибка запуска процесса: " + ex.Message, true);
            }
        }

        private void BtnMoveDemo_Click(object sender, RoutedEventArgs e)
        {
            if (dgvDemos.SelectedItems.Count == 0)
            {
                ShowMessageDialog("Информация", "Выберите хотя бы одну демку для переноса.");
                return;
            }

            string target = ShowSelectFolderDialog("Куда перенести выбранные демки?");
            if (string.IsNullOrEmpty(target)) return;

            List<DemoGridRow> selectedRows = new List<DemoGridRow>();
            foreach (var item in dgvDemos.SelectedItems)
            {
                selectedRows.Add((DemoGridRow)item);
            }

            MoveSelectedDemos(selectedRows, target);
        }

        private void BtnDeleteDemo_Click(object sender, RoutedEventArgs e)
        {
            if (dgvDemos.SelectedItems.Count == 0)
            {
                ShowMessageDialog("Информация", "Выберите хотя бы одну демку для удаления.");
                return;
            }

            bool mr = ShowConfirmDialog("Удаление файлов", string.Format("Вы уверены, что хотите навсегда удалить выбранные демки ({0} шт.)?", dgvDemos.SelectedItems.Count));
            if (!mr) return;

            string baseDir = GetDemosBaseDir();
            int deletedCount = 0;

            List<DemoGridRow> toDelete = new List<DemoGridRow>();
            foreach (var item in dgvDemos.SelectedItems)
            {
                toDelete.Add((DemoGridRow)item);
            }

            foreach (var row in toDelete)
            {
                try
                {
                    if (File.Exists(row.FilePath))
                    {
                        File.Delete(row.FilePath);
                    }
                    string relPath = row.FilePath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    if (metadataDb.ContainsKey(relPath))
                    {
                        metadataDb.Remove(relPath);
                    }
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    ShowMessageDialog("Ошибка", string.Format("Не удалось удалить файл {0}: {1}", Path.GetFileName(row.FilePath), ex.Message), true);
                }
            }

            // Save cleaned metadata db
            if (deletedCount > 0)
            {
                try
                {
                    string path = Path.Combine(baseDir, "metadata.txt");
                    List<string> lines = new List<string>();
                    foreach (var kvp in metadataDb)
                    {
                        lines.Add(string.Format("{0}|{1}|{2}|{3}|{4}|{5}", kvp.Key, kvp.Value.Map, kvp.Value.Score, kvp.Value.KD, kvp.Value.Date, kvp.Value.Note));
                    }
                    File.WriteAllLines(path, lines);
                }
                catch { }

                RefreshDemoList();
                lblStatus.Text = string.Format("Успешно удалено файлов: {0}", deletedCount);
            }
        }

        private string GetDemosBaseDir()
        {
            string cs2Dir = "";
            if (txtCS2 != null)
            {
                if (Thread.CurrentThread == this.Dispatcher.Thread)
                {
                    cs2Dir = txtCS2.Text.Trim();
                }
                else
                {
                    this.Dispatcher.Invoke(new Action(() => {
                        cs2Dir = txtCS2.Text.Trim();
                    }));
                }
            }
            if (string.IsNullOrEmpty(cs2Dir)) return "";
            return Path.Combine(cs2Dir, "faceit_demos");
        }

        private void DgvDemos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvDemos.SelectedItem == null)
            {
                isUpdatingFields = true;
                txtEditMap.Text = "";
                txtEditScore.Text = "";
                txtEditKD.Text = "";
                txtEditDate.Text = "";
                txtNoteEdit.Text = "";

                txtEditMap.IsEnabled = false;
                txtEditScore.IsEnabled = false;
                txtEditKD.IsEnabled = false;
                txtEditDate.IsEnabled = false;
                txtNoteEdit.IsEnabled = false;
                isUpdatingFields = false;
                return;
            }

            txtEditMap.IsEnabled = true;
            txtEditScore.IsEnabled = true;
            txtEditKD.IsEnabled = true;
            txtEditDate.IsEnabled = true;
            txtNoteEdit.IsEnabled = true;

            DemoGridRow row = (DemoGridRow)dgvDemos.SelectedItem;
            string file = row.FilePath;
            string baseDir = GetDemosBaseDir();
            string relPath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');

            DemoMetadata dm = null;
            if (!metadataDb.TryGetValue(relPath, out dm))
            {
                dm = DemoProcessor.ParseMetadataFromFilename(Path.GetFileName(file));
            }

            isUpdatingFields = true;
            txtEditMap.Text = dm.Map;
            txtEditScore.Text = dm.Score;
            txtEditKD.Text = dm.KD;
            txtEditDate.Text = dm.Date;
            txtNoteEdit.Text = dm.Note;
            isUpdatingFields = false;
        }

        private void EditField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingFields) return;
            if (dgvDemos.SelectedItem == null) return;

            DemoGridRow row = (DemoGridRow)dgvDemos.SelectedItem;
            string file = row.FilePath;
            string baseDir = GetDemosBaseDir();
            string relPath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');

            DemoMetadata dm = null;
            if (!metadataDb.TryGetValue(relPath, out dm))
            {
                dm = DemoProcessor.ParseMetadataFromFilename(Path.GetFileName(file));
            }

            dm.Map = txtEditMap.Text.Trim();
            dm.Score = txtEditScore.Text.Trim();
            dm.KD = txtEditKD.Text.Trim();
            dm.Date = txtEditDate.Text.Trim();
            dm.Note = txtNoteEdit.Text.Trim();

            DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, relPath, dm);

            // Update row properties
            row.Map = GetMapEmoji(dm.Map) + " " + dm.Map;
            row.Score = dm.Score;
            row.Date = dm.Date;
            row.Note = dm.Note;

            // Re-calculate formatted fields
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
                        if (s1 > s2) { isWin = true; scoreText = "W " + s1 + " : " + s2; }
                        else { isWin = false; scoreText = "L " + s1 + " : " + s2; }
                    }
                }
            }
            row.IsWin = isWin;
            row.ScoreText = scoreText;

            string kdaText = "-";
            string kdRatioText = "-";
            string kdStatusText = "Normal";
            string adrText = "-";
            if (!string.IsNullOrEmpty(dm.KD) && dm.KD != "-")
            {
                Match mRatio = Regex.Match(dm.KD, @"^([\d.]+)");
                if (mRatio.Success)
                {
                    kdRatioText = mRatio.Groups[1].Value;
                    double ratioVal;
                    if (double.TryParse(kdRatioText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ratioVal))
                    {
                        kdStatusText = ratioVal >= 1.0 ? "High" : "Low";
                    }
                }
                Match mKda = Regex.Match(dm.KD, @"\(([^()]+)\)");
                if (mKda.Success)
                {
                    string kdaRaw = mKda.Groups[1].Value;
                    string[] kparts = kdaRaw.Split('/');
                    if (kparts.Length == 3) kdaText = string.Format("{0} / {1} / {2}", kparts[0], kparts[1], kparts[2]);
                    else if (kparts.Length == 2) kdaText = string.Format("{0} / {1} / -", kparts[0], kparts[1]);
                }
                Match mAdrVal = Regex.Match(dm.KD, @"\[([^[\]]+)\]");
                if (mAdrVal.Success) adrText = mAdrVal.Groups[1].Value;
            }
            row.KDA = kdaText;
            row.KDRatio = kdRatioText;
            row.KDStatus = kdStatusText;
            row.ADR = adrText;

            string dateTextFormatted = dm.Date;
            DateTime dtVal;
            if (DateTime.TryParse(dm.Date, out dtVal))
            {
                dateTextFormatted = dtVal.ToString("ddd d MMM", new System.Globalization.CultureInfo("ru-RU")) + "\n" + dtVal.ToString("HH:mm");
            }
            row.DateFormatted = dateTextFormatted;

            dgvDemos.Items.Refresh();
        }

        private bool isUpdatingNickname = false;
        private void UpdateNicknameInput()
        {
            if (txtNickname == null || settings == null) return;
            
            isUpdatingNickname = true;
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            
            if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Все демки]")
            {
                txtNickname.Text = settings.Nickname;
            }
            else
            {
                string folderNick = "";
                if (settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue(selectedFolder, out folderNick))
                {
                    txtNickname.Text = folderNick;
                }
                else
                {
                    // Fallback to global default nickname
                    txtNickname.Text = settings.Nickname;
                }
            }
            isUpdatingNickname = false;
        }

        private void BtnResetBinds_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = ShowConfirmDialog("Сброс биндов", "Вы действительно хотите сбросить все горячие клавиши к настройкам по умолчанию?");
            if (confirm)
            {
                settings.Binds = ConfigManager.GetDefaultBinds();
                dgvBinds.ItemsSource = null;
                dgvBinds.ItemsSource = settings.Binds;
                SaveConfig();
            }
        }

        private void LoadConfig()
        {
            settings = ConfigManager.Load(configPath);

            txtDownloads.Text = settings.DownloadsPath;
            txtCS2.Text = settings.CS2Path;
            
            UpdateNicknameInput();
            
            ChkWatchFolder.IsChecked = settings.WatchFolder;
            ChkTray.IsChecked = settings.MinimizeTray;
            chkVoiceInDemos.IsChecked = settings.EnableDemoVoice;
            chkAutoApplyBinds.IsChecked = settings.AutoApplyBinds;

            dgvBinds.ItemsSource = settings.Binds;

            // Set Import Mode ComboBox selection
            foreach (ComboBoxItem item in CboImportMode.Items)
            {
                if (item.Tag.ToString().Equals(settings.ImportMode, StringComparison.OrdinalIgnoreCase))
                {
                    CboImportMode.SelectedItem = item;
                    break;
                }
            }

            // Set Target Import Folder
            UpdateImportFolderCombobox();
            if (CboImportFolder.Items.Contains(settings.TargetImportFolder))
            {
                CboImportFolder.SelectedItem = settings.TargetImportFolder;
            }
        }

        private void SaveConfig()
        {
            if (settings == null) settings = new AppSettings();

            settings.DownloadsPath = txtDownloads.Text.Trim();
            settings.CS2Path = txtCS2.Text.Trim();
            
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Все демки]")
            {
                settings.Nickname = txtNickname.Text.Trim();
            }
            else
            {
                if (settings.FolderNicknames == null)
                {
                    settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                settings.FolderNicknames[selectedFolder] = txtNickname.Text.Trim();
            }

            settings.WatchFolder = ChkWatchFolder.IsChecked == true;
            settings.MinimizeTray = ChkTray.IsChecked == true;
            settings.EnableDemoVoice = chkVoiceInDemos.IsChecked == true;
            settings.AutoApplyBinds = chkAutoApplyBinds.IsChecked == true;

            var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
            if (selectedItem != null) settings.ImportMode = selectedItem.Tag.ToString();
            
            settings.TargetImportFolder = CboImportFolder.SelectedItem != null ? CboImportFolder.SelectedItem.ToString() : "General";

            ConfigManager.Save(configPath, settings);
        }

        private void CboImportMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboImportFolder == null) return;
            var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
            if (selectedItem != null)
            {
                string mode = selectedItem.Tag.ToString();
                CboImportFolder.IsEnabled = (mode == "Specific");
            }
            SaveConfig();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveConfig();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }

        private void Log(string message, bool isError)
        {
            AppendLog((isError ? "Ошибка: " : "") + message);
        }

        private void UpdateProgress(int val, int max)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                prgBar.Maximum = max;
                prgBar.Value = val;
            }));
        }

        private void ResetProgress()
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                prgBar.Value = 0;
            }));
        }
    }
}
