using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private void InitializeWindow()
        {
            this.Title = "FACEIT Demo Hub";
            this.Width = 1000;
            this.Height = 650;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Opacity = 0;
            this.Loaded += MainWindow_Loaded;

            try
            {
                this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/FaceitDemoManager;component/app.ico", UriKind.RelativeOrAbsolute));
            }
            catch (Exception)
            {
                // Fallback
            }

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
            chkDeleteArchives = (CheckBox)root.FindName("ChkDeleteArchives");
            CboImportMode = (ComboBox)root.FindName("CboImportMode");
            CboImportFolder = (ComboBox)root.FindName("CboImportFolder");
            btnBrowseDownloads = (Button)root.FindName("BtnBrowseDownloads");
            btnAutoCS2 = (Button)root.FindName("BtnAutoCS2");
            btnBrowseCS2 = (Button)root.FindName("BtnBrowseCS2");
            btnProcess = (Button)root.FindName("BtnProcess");
            btnClearLog = (Button)root.FindName("BtnClearLog");

            gridLibraryTab = (Grid)root.FindName("GridLibraryTab");
            lstFolders = (ListBox)root.FindName("LstFolders");
            if (lstFolders != null) lstFolders.AllowDrop = true;
            txtSearch = (TextBox)root.FindName("TxtSearch");
            dgvDemos = (DataGrid)root.FindName("DgvDemos");
            pnlMapFilters = (WrapPanel)root.FindName("PnlMapFilters");

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
            btnAddBind = (Button)root.FindName("BtnAddBind");
            btnDeleteBind = (Button)root.FindName("BtnDeleteBind");

            lblStatus = (TextBlock)root.FindName("LblStatus");
            prgBar = (ProgressBar)root.FindName("PrgBar");

            // Event Bindings
            this.Closing += MainWindow_Closing;
            
            // TitleBar Drag & Double-Click to test error handler
            Grid titleBar = (Grid)root.FindName("TitleBar");
            titleBar.MouseLeftButtonDown += (s, e) => {
                if (e.ClickCount == 2)
                {
                    throw new InvalidOperationException("Тестовое исключение для проверки глобального перехватчика ошибок (Global Runtime Error Monitoring)!");
                }
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
            dragDropZone.AllowDrop = true;
            dragDropZone.Drop += MainWindow_Drop;
            dragDropZone.DragOver += (s, e) => {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            };
            dragDropZone.MouseLeftButtonDown += DragDropZone_MouseLeftButtonDown;

            // Scan / Watcher triggers
            btnProcess.Click += BtnProcess_Click;
            if (btnClearLog != null)
            {
                btnClearLog.Click += (s, e) => {
                    if (txtLogConsole != null) txtLogConsole.Clear();
                };
            }
            ChkWatchFolder.Checked += (s, e) => ToggleWatcherSetting(true);
            ChkWatchFolder.Unchecked += (s, e) => ToggleWatcherSetting(false);
            
            // Voice Setting triggers
            if (chkVoiceInDemos != null)
            {
                chkVoiceInDemos.Checked += (s, e) => SaveConfig();
                chkVoiceInDemos.Unchecked += (s, e) => SaveConfig();
            }

            // Auto-apply binds triggers
            chkAutoApplyBinds.Checked += (s, e) => SaveConfig();
            chkAutoApplyBinds.Unchecked += (s, e) => SaveConfig();

            if (chkDeleteArchives != null)
            {
                chkDeleteArchives.Checked += (s, e) => SaveConfig();
                chkDeleteArchives.Unchecked += (s, e) => SaveConfig();
            }

            // Reset binds button
            btnResetBinds.Click += BtnResetBinds_Click;
            btnAddBind.Click += BtnAddBind_Click;
            btnDeleteBind.Click += BtnDeleteBind_Click;
            dgvBinds.CellEditEnding += (s, e) => {
                Dispatcher.BeginInvoke(new Action(() => SaveConfig()), System.Windows.Threading.DispatcherPriority.Background);
            };

            // Library search & list selection
            txtSearch.TextChanged += (s, e) => RefreshDemoList();
            
            // Setup folder selection text updates contextually
            lstFolders.SelectionChanged += (s, e) => {
                UpdateNicknameInput();
                selectedMapFilter = null;
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
            lstFolders.PreviewMouseLeftButtonDown += LstFolders_PreviewMouseLeftButtonDown;
            lstFolders.MouseMove += LstFolders_MouseMove;
            lstFolders.ContextMenuOpening += LstFolders_ContextMenuOpening;
            lstFolders.MouseDoubleClick += LstFolders_MouseDoubleClick;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.25)));
            this.BeginAnimation(Window.OpacityProperty, anim);
        }
    }
}
