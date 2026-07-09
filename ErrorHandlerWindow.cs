using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FaceitDemoManager
{
    public class ErrorHandlerWindow : Window
    {
        private Exception _exception;
        private bool _isRecoverable;

        public ErrorHandlerWindow(Exception exception, bool isRecoverable)
        {
            _exception = exception;
            _isRecoverable = isRecoverable;

            this.Title = "Критический сбой";
            this.Width = 620;
            this.Height = 460;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Topmost = true;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            string xaml = @"
<Border xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        CornerRadius=""12"" Background=""#121214"" BorderBrush=""#ef4444"" BorderThickness=""1.5"">
    <Grid Margin=""20"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""*""/>
            <RowDefinition Height=""Auto""/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Grid Grid.Row=""0"" Margin=""0,0,0,12"">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""*""/>
                <ColumnDefinition Width=""Auto""/>
            </Grid.ColumnDefinitions>
            
            <StackPanel Orientation=""Horizontal"" Grid.Column=""0"">
                <TextBlock Text=""⚠"" Foreground=""#ef4444"" FontSize=""22"" FontWeight=""Bold"" Margin=""0,10,10,0"" VerticalAlignment=""Center""/>
                <TextBlock Text=""Критический сбой программы"" Foreground=""White"" FontSize=""16"" FontWeight=""Bold"" VerticalAlignment=""Center""/>
            </StackPanel>

            <Button Name=""BtnClose"" Grid.Column=""1"" Content=""✕"" Foreground=""#71717a"" Background=""Transparent"" BorderThickness=""0"" FontSize=""14"" Cursor=""Hand"" Width=""28"" Height=""28"" VerticalAlignment=""Center"">
                <Button.Template>
                    <ControlTemplate TargetType=""Button"">
                        <Border Name=""b"" Background=""{TemplateBinding Background}"" CornerRadius=""14"">
                            <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property=""IsMouseOver"" Value=""True"">
                                <Setter TargetName=""b"" Property=""Background"" Value=""#27272a""/>
                                <Setter Property=""Foreground"" Value=""White""/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Button.Template>
            </Button>
        </Grid>

        <!-- Content -->
        <Grid Grid.Row=""1"" Margin=""0,0,0,15"">
            <Grid.RowDefinitions>
                <RowDefinition Height=""Auto""/>
                <RowDefinition Height=""*""/>
            </Grid.RowDefinitions>

            <TextBlock Grid.Row=""0"" TextWrapping=""Wrap"" Foreground=""#a1a1aa"" FontSize=""13"" Margin=""0,0,0,12"" LineHeight=""18"">
                Произошла непредвиденная ошибка во время работы FACEIT Demo Hub. Приложение может работать нестабильно. Вы можете скопировать подробный отчет для отправки разработчику или попробовать продолжить работу.
            </TextBlock>

            <Border Grid.Row=""1"" CornerRadius=""8"" Background=""#18181b"" BorderBrush=""#27272a"" BorderThickness=""1"" Padding=""12"">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height=""Auto""/>
                        <RowDefinition Height=""*""/>
                    </Grid.RowDefinitions>

                    <TextBlock Name=""TxtErrorMsg"" Grid.Row=""0"" Foreground=""#f4f4f5"" FontWeight=""SemiBold"" TextWrapping=""Wrap"" FontSize=""12"" Margin=""0,0,0,8""/>
                    
                    <ScrollViewer Grid.Row=""1"" VerticalScrollBarVisibility=""Auto"" HorizontalScrollBarVisibility=""Auto"">
                        <TextBox Name=""TxtDetails"" Foreground=""#ef4444"" Background=""Transparent"" BorderThickness=""0"" IsReadOnly=""True"" TextWrapping=""Wrap"" FontSize=""11"" FontFamily=""Consolas"" AcceptsReturn=""True"" AcceptsTab=""True""/>
                    </ScrollViewer>
                </Grid>
            </Border>
        </Grid>

        <!-- Action Buttons -->
        <StackPanel Grid.Row=""2"" Orientation=""Horizontal"" HorizontalAlignment=""Right"">
            <Button Name=""BtnCopy"" Content=""Скопировать отчет"" Margin=""0,0,10,0"" Width=""145"" Height=""34""/>
            <Button Name=""BtnContinue"" Content=""Продолжить"" Margin=""0,0,10,0"" Width=""110"" Height=""34""/>
            <Button Name=""BtnRestart"" Content=""Перезапустить"" Margin=""0,0,10,0"" Width=""120"" Height=""34""/>
            <Button Name=""BtnExit"" Content=""Выход"" Width=""80"" Height=""34""/>
        </StackPanel>
    </Grid>
</Border>";

            Border root = (Border)XamlReader.Parse(xaml);
            this.Content = root;

            // Handle Dragging
            root.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    this.DragMove();
            };

            // Find elements
            Button btnClose = (Button)root.FindName("BtnClose");
            Button btnCopy = (Button)root.FindName("BtnCopy");
            Button btnContinue = (Button)root.FindName("BtnContinue");
            Button btnRestart = (Button)root.FindName("BtnRestart");
            Button btnExit = (Button)root.FindName("BtnExit");
            TextBlock txtErrorMsg = (TextBlock)root.FindName("TxtErrorMsg");
            TextBox txtDetails = (TextBox)root.FindName("TxtDetails");

            // Fill text
            txtErrorMsg.Text = _exception.GetType().Name + ": " + _exception.Message;
            txtDetails.Text = _exception.ToString();

            // Enable/disable continue
            if (!_isRecoverable)
            {
                btnContinue.IsEnabled = false;
                btnContinue.Opacity = 0.5;
            }

            // Apply button styles dynamically to match the app's modern purple styling
            StyleBtn(btnCopy, "#27272a", "#3f3f46", "#ffffff");
            StyleBtn(btnContinue, "#7c3aed", "#8b5cf6", "#ffffff");
            StyleBtn(btnRestart, "#18181b", "#27272a", "#a1a1aa");
            StyleBtn(btnExit, "#dc2626", "#ef4444", "#ffffff");

            // Wire up event handlers
            btnClose.Click += (s, e) => this.Close();
            btnExit.Click += (s, e) => {
                Application.Current.Shutdown();
                Environment.Exit(0);
            };

            btnContinue.Click += (s, e) => {
                this.DialogResult = true;
                this.Close();
            };

            btnRestart.Click += (s, e) => {
                try
                {
                    string appPath = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось автоматически перезапустить приложение: " + ex.Message, "Ошибка перезапуска", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Application.Current.Shutdown();
                Environment.Exit(0);
            };

            btnCopy.Click += (s, e) => {
                try
                {
                    string report = GenerateReport();
                    Clipboard.SetText(report);
                    btnCopy.Content = "Скопировано! ✓";
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(2);
                    timer.Tick += (sender, args) =>
                    {
                        btnCopy.Content = "Скопировать отчет";
                        timer.Stop();
                    };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось скопировать в буфер обмена: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            // Window animations
            this.Loaded += (s, e) =>
            {
                var transform = new TranslateTransform(0, -30);
                root.RenderTransform = transform;
                root.RenderTransformOrigin = new Point(0.5, 0.5);

                var slideAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(350))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(TranslateTransform.YProperty, slideAnim);

                var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                this.BeginAnimation(Window.OpacityProperty, fadeAnim);
            };
        }

        private void StyleBtn(Button btn, string normalBg, string hoverBg, string fgColor)
        {
            string templateXaml = $@"
<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 TargetType=""Button"">
    <Border Name=""b"" CornerRadius=""8"" Background=""{normalBg}"" SnapsToDevicePixels=""True"">
        <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
    </Border>
    <ControlTemplate.Triggers>
        <Trigger Property=""IsMouseOver"" Value=""True"">
            <Setter TargetName=""b"" Property=""Background"" Value=""{hoverBg}""/>
        </Trigger>
        <Trigger Property=""IsEnabled"" Value=""False"">
            <Setter TargetName=""b"" Property=""Background"" Value=""#1e1e1e""/>
            <Setter Property=""Foreground"" Value=""#444444""/>
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>";

            btn.Template = (ControlTemplate)XamlReader.Parse(templateXaml);
            btn.Background = (Brush)new BrushConverter().ConvertFromString(normalBg);
            btn.Foreground = (Brush)new BrushConverter().ConvertFromString(fgColor);
            btn.FontWeight = FontWeights.Bold;
            btn.FontSize = 12;
            btn.Cursor = Cursors.Hand;
        }

        private string GenerateReport()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var winVersion = Environment.OSVersion.ToString();
            var netVersion = RuntimeInformation.FrameworkDescription;
            var is64Bit = Environment.Is64BitProcess ? "64-bit" : "32-bit";
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            return $@"### FACEIT Demo Hub - Отчет об ошибке
- **Время:** {time}
- **ОС:** {winVersion} ({is64Bit})
- **Среда выполнения:** {netVersion}
- **Версия сборки:** {assembly.GetName().Version}

#### Тип ошибки
`{_exception.GetType().FullName}`

#### Сообщение
`{_exception.Message}`

#### Стек вызовов
```
{_exception.ToString()}
```
";
        }
    }
}
