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
        private TextBox txtSidebarNick;
        private TextBlock lblSidebarNick;

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
        private string lastSidebarNick = "";
        private string lastGlobalNick = "";

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
            txtSidebarNick = (TextBox)root.FindName("TxtSidebarNick");
            lblSidebarNick = (TextBlock)root.FindName("LblSidebarNick");

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

            // Nickname commit event handlers
            txtNickname.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                {
                    CommitGlobalNickname();
                    txtNickname.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
            };
            txtNickname.LostFocus += (s, e) => CommitGlobalNickname();

            if (txtSidebarNick != null)
            {
                txtSidebarNick.KeyDown += (s, e) => {
                    if (e.Key == Key.Enter)
                    {
                        CommitSidebarNickname();
                        txtSidebarNick.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        e.Handled = true;
                    }
                };
                txtSidebarNick.LostFocus += (s, e) => CommitSidebarNickname();
            }

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
            CboImportFolder.SelectionChanged += CboImportFolder_SelectionChanged;

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

        private bool isUpdatingNickname = false;
        private void UpdateNicknameInput()
        {
            if (settings == null) return;
            
            isUpdatingNickname = true;
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            
            if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Все devки]" || selectedFolder == "[Все демки]")
            {
                if (lblSidebarNick != null) lblSidebarNick.Visibility = Visibility.Collapsed;
                if (txtSidebarNick != null)
                {
                    txtSidebarNick.Visibility = Visibility.Collapsed;
                    txtSidebarNick.Text = "";
                    lastSidebarNick = "";
                }
            }
            else
            {
                string folderNick = "";
                if (settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue(selectedFolder, out folderNick))
                {
                    // Found
                }
                else
                {
                    folderNick = "";
                }
                
                if (lblSidebarNick != null)
                {
                    lblSidebarNick.Visibility = Visibility.Visible;
                    lblSidebarNick.Text = string.Format("Никнейм для '{0}':", selectedFolder);
                }
                if (txtSidebarNick != null)
                {
                    txtSidebarNick.Visibility = Visibility.Visible;
                    txtSidebarNick.Text = folderNick;
                    lastSidebarNick = folderNick;
                }
            }
            
            isUpdatingNickname = false;
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

        private void LoadConfig()
        {
            settings = ConfigManager.Load(configPath);

            txtDownloads.Text = settings.DownloadsPath;
            txtCS2.Text = settings.CS2Path;
            
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

            UpdateImportNicknameDisplay();
            UpdateNicknameInput();
        }

        private void SaveConfig()
        {
            if (settings == null) settings = new AppSettings();

            settings.DownloadsPath = txtDownloads.Text.Trim();
            settings.CS2Path = txtCS2.Text.Trim();
            
            // Save global nickname ONLY if we are not editing specific folders
            string importMode = CboImportMode != null && CboImportMode.SelectedItem != null ? ((ComboBoxItem)CboImportMode.SelectedItem).Tag.ToString() : "General";
            if (importMode == "Ask")
            {
                settings.Nickname = txtNickname.Text.Trim();
            }
            else if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                settings.FolderNicknames[targetFolder] = txtNickname.Text.Trim();
            }
            else if (importMode == "General")
            {
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                settings.FolderNicknames["General"] = txtNickname.Text.Trim();
            }
            
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (!string.IsNullOrEmpty(selectedFolder) && selectedFolder != "[Все демки]")
            {
                if (settings.FolderNicknames == null)
                {
                    settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                if (txtSidebarNick != null)
                {
                    settings.FolderNicknames[selectedFolder] = txtSidebarNick.Text.Trim();
                }
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
            UpdateImportNicknameDisplay();
            SaveConfig();
        }

        private void CboImportFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateImportNicknameDisplay();
            SaveConfig();
        }

        private void UpdateImportNicknameDisplay()
        {
            if (CboImportMode == null || CboImportFolder == null || txtNickname == null || settings == null) return;

            isUpdatingNickname = true;

            var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
            string mode = selectedItem != null ? selectedItem.Tag.ToString() : "General";

            string currentNick = "";
            if (mode == "Specific" && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue(targetFolder, out currentNick))
                {
                    // Found
                }
                else
                {
                    currentNick = settings.Nickname;
                }
            }
            else if (mode == "General")
            {
                if (settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue("General", out currentNick))
                {
                    // Found
                }
                else
                {
                    currentNick = settings.Nickname;
                }
            }
            else
            {
                currentNick = settings.Nickname;
            }

            txtNickname.Text = currentNick;
            lastGlobalNick = currentNick;

            isUpdatingNickname = false;
        }

        private void CommitSidebarNickname()
        {
            if (settings == null || isUpdatingNickname) return;
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Все демки]") return;

            string newNick = txtSidebarNick.Text.Trim();
            if (newNick == lastSidebarNick) return; // No change

            isUpdatingNickname = true;
            if (settings.FolderNicknames == null)
            {
                settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            settings.FolderNicknames[selectedFolder] = newNick;
            ConfigManager.Save(configPath, settings);
            
            // Sync settings textbox if the same folder is currently selected in the import tab
            string importMode = CboImportMode != null && CboImportMode.SelectedItem != null ? ((ComboBoxItem)CboImportMode.SelectedItem).Tag.ToString() : "General";
            if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (targetFolder.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    if (txtNickname != null) txtNickname.Text = newNick;
                }
            }
            else if (importMode == "General" && selectedFolder.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                if (txtNickname != null) txtNickname.Text = newNick;
            }
            
            lastSidebarNick = newNick;
            isUpdatingNickname = false;

            // Trigger re-fetching stats for this folder
            UpdateFolderDemosStats(selectedFolder, newNick);
        }

        private void CommitGlobalNickname()
        {
            if (settings == null || isUpdatingNickname) return;
            string newNick = txtNickname.Text.Trim();
            if (newNick == lastGlobalNick) return; // No change

            isUpdatingNickname = true;
            string importMode = CboImportMode != null && CboImportMode.SelectedItem != null ? ((ComboBoxItem)CboImportMode.SelectedItem).Tag.ToString() : "General";
            
            if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                settings.FolderNicknames[targetFolder] = newNick;
                
                // Sync sidebar if the same folder is selected there
                string selectedSidebarFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
                if (selectedSidebarFolder != null && selectedSidebarFolder.Equals(targetFolder, StringComparison.OrdinalIgnoreCase))
                {
                    if (txtSidebarNick != null) txtSidebarNick.Text = newNick;
                }
            }
            else if (importMode == "General")
            {
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                settings.FolderNicknames["General"] = newNick;
                
                // Sync sidebar if General is selected
                string selectedSidebarFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
                if (selectedSidebarFolder != null && selectedSidebarFolder.Equals("General", StringComparison.OrdinalIgnoreCase))
                {
                    if (txtSidebarNick != null) txtSidebarNick.Text = newNick;
                }
            }
            else
            {
                settings.Nickname = newNick;
            }

            ConfigManager.Save(configPath, settings);
            lastGlobalNick = newNick;
            isUpdatingNickname = false;

            // If a specific folder's nickname was changed via the settings tab, update its demos stats too!
            if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                UpdateFolderDemosStats(CboImportFolder.SelectedItem.ToString(), newNick);
            }
            else if (importMode == "General")
            {
                UpdateFolderDemosStats("General", newNick);
            }
        }

        private void UpdateFolderDemosStats(string folderName, string nickname)
        {
            if (string.IsNullOrEmpty(folderName) || folderName == "[Все демки]" || string.IsNullOrEmpty(nickname)) return;

            AppendLog(string.Format("Обновление статистики для папки '{0}' с никнеймом '{1}'...", folderName, nickname));
            
            Thread thread = new Thread(() =>
            {
                try
                {
                    string playerId = "";
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FaceitDemoHub");
                        string profileJson = wc.DownloadString("https://api.faceit.com/users/v1/nicknames/" + nickname);
                        Match mId = Regex.Match(profileJson, @"""id""\s*:\s*""([^""]+)""");
                        if (mId.Success)
                        {
                            playerId = mId.Groups[1].Value;
                        }
                    }

                    if (string.IsNullOrEmpty(playerId))
                    {
                        AppendLog("Не удалось найти игрока с таким никнеймом на Faceit.");
                        return;
                    }

                    string historyUrl = string.Format("https://api.faceit.com/stats/v1/stats/time/users/{0}/games/cs2?size=100", playerId);
                    string historyJson = "";
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FaceitDemoHub");
                        historyJson = wc.DownloadString(historyUrl);
                    }

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
                            string kdStr = string.Format("{0} ({1}/{2}/{3}) [{4}]", mKD.Groups[1].Value, mKills.Groups[1].Value, mDeaths.Groups[1].Value, assists, adr);
                            
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

                    string baseDir = GetDemosBaseDir();
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
                                    updatedAny = true;
                                }
                            }
                        }

                        if (updatedAny)
                        {
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                RefreshDemoList();
                                AppendLog(string.Format("Статистика для папки '{0}' успешно обновлена!", folderName));
                            }));
                        }
                        else
                        {
                            AppendLog("В этой папке не найдено подходящих демок для обновления статистики.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog("Ошибка при обновлении статистики: " + ex.Message);
                }
            });
            thread.IsBackground = true;
            thread.Start();
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
