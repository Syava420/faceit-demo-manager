using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private string GetDialogResourcesXml()
        {
            return @"<Border.Resources>
                <Style TargetType=""Button"">
                    <Setter Property=""Background"" Value=""#27272a""/>
                    <Setter Property=""Foreground"" Value=""White""/>
                    <Setter Property=""FontWeight"" Value=""Bold""/>
                    <Setter Property=""BorderThickness"" Value=""0""/>
                    <Setter Property=""Cursor"" Value=""Hand""/>
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""Button"">
                                <Border x:Name=""border"" CornerRadius=""14"" Background=""{TemplateBinding Background}"">
                                    <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsMouseOver"" Value=""True"">
                                        <Setter TargetName=""border"" Property=""Background"" Value=""#3f3f46""/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
                <Style x:Key=""PrimaryBtn"" TargetType=""Button"" BasedOn=""{StaticResource {x:Type Button}}"">
                    <Setter Property=""Background"" Value=""#8b5cf6""/>
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""Button"">
                                <Border x:Name=""border"" CornerRadius=""14"" Background=""{TemplateBinding Background}"">
                                    <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsMouseOver"" Value=""True"">
                                        <Setter TargetName=""border"" Property=""Background"" Value=""#a78bfa""/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
                <Style x:Key=""DangerBtn"" TargetType=""Button"" BasedOn=""{StaticResource {x:Type Button}}"">
                    <Setter Property=""Background"" Value=""#ef4444""/>
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""Button"">
                                <Border x:Name=""border"" CornerRadius=""14"" Background=""{TemplateBinding Background}"">
                                    <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsMouseOver"" Value=""True"">
                                        <Setter TargetName=""border"" Property=""Background"" Value=""#f87171""/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
                <Style TargetType=""TextBox"">
                    <Setter Property=""Background"" Value=""#27272a""/>
                    <Setter Property=""Foreground"" Value=""White""/>
                    <Setter Property=""BorderBrush"" Value=""#3f3f46""/>
                    <Setter Property=""BorderThickness"" Value=""1""/>
                    <Setter Property=""Padding"" Value=""6,4""/>
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""TextBox"">
                                <Border Name=""Border"" CornerRadius=""8"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"">
                                    <ScrollViewer Name=""PART_ContentHost"" Focusable=""False"" HorizontalScrollBarVisibility=""Hidden"" VerticalScrollBarVisibility=""Hidden""/>
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsFocused"" Value=""True"">
                                        <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""#8b5cf6""/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </Border.Resources>";
        }

        // Ввод текста
        private string ShowInputDialog(string title, string promptText)
        {
            Window w = new Window();
            w.Title = title;
            w.Width = 350;
            w.Height = 160;
            w.WindowStyle = WindowStyle.None;
            w.AllowsTransparency = true;
            w.Background = Brushes.Transparent;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            string xaml = @"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    CornerRadius='12' Background='#121214' BorderBrush='#2e2e34' BorderThickness='1' Padding='15'>
                " + GetDialogResourcesXml() + @"
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height='Auto'/>
                        <RowDefinition Height='*'/>
                        <RowDefinition Height='Auto'/>
                    </Grid.RowDefinitions>
                    <TextBlock Text='{PROMPT}' Name='Prompt' Foreground='#a1a1aa' FontSize='11' FontWeight='Bold' Margin='0,0,0,10'/>
                    <TextBox Name='Input' Grid.Row='1' Height='25' VerticalContentAlignment='Center'/>
                    <StackPanel Grid.Row='2' Orientation='Horizontal' HorizontalAlignment='Right' Margin='0,10,0,0'>
                        <Button Name='Ok' Content='OK' Style='{StaticResource PrimaryBtn}' Width='80' Height='28' Margin='0,0,8,0'/>
                        <Button Name='Cancel' Content='Отмена' Width='80' Height='28'/>
                    </StackPanel>
                </Grid>
            </Border>";
            xaml = xaml.Replace("{PROMPT}", promptText);
            Border root = (Border)XamlReader.Parse(xaml);
            w.Content = root;

            TextBox inputTxt = (TextBox)root.FindName("Input");
            Button btnOk = (Button)root.FindName("Ok");
            Button btnCancel = (Button)root.FindName("Cancel");

            string result = "";
            btnOk.Click += (s, e) => { result = inputTxt.Text; w.DialogResult = true; w.Close(); };
            btnCancel.Click += (s, e) => { w.DialogResult = false; w.Close(); };

            root.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) w.DragMove(); };

            w.ShowDialog();
            return w.DialogResult == true ? result : "";
        }

        // Выбор папки из списка
        private string ShowSelectFolderDialog(string titleText)
        {
            Window w = new Window();
            w.Title = "Выбор папки";
            w.Width = 380;
            w.Height = 300;
            w.WindowStyle = WindowStyle.None;
            w.AllowsTransparency = true;
            w.Background = Brushes.Transparent;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            string xaml = @"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    CornerRadius='12' Background='#121214' BorderBrush='#2e2e34' BorderThickness='1' Padding='15'>
                " + GetDialogResourcesXml() + @"
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height='Auto'/>
                        <RowDefinition Height='*'/>
                        <RowDefinition Height='Auto'/>
                    </Grid.RowDefinitions>
                    
                    <TextBlock Text='{TITLE}' Foreground='White' FontWeight='Bold' FontSize='13' Margin='0,0,0,12' TextWrapping='Wrap'/>
                    
                    <ListBox Name='LstFoldersSelect' Grid.Row='1' Background='#18181b' BorderBrush='#27272a' BorderThickness='1' Margin='0,0,0,12' Padding='5'>
                        <ListBox.ItemContainerStyle>
                            <Style TargetType='ListBoxItem'>
                                <Setter Property='Foreground' Value='Gray'/>
                                <Setter Property='Padding' Value='10,8'/>
                                <Setter Property='Margin' Value='0,2'/>
                                <Setter Property='Cursor' Value='Hand'/>
                                <Setter Property='Template'>
                                    <Setter.Value>
                                        <ControlTemplate TargetType='ListBoxItem'>
                                            <Border Name='Border' CornerRadius='8' Background='Transparent' Padding='{TemplateBinding Padding}'>
                                                <ContentPresenter VerticalAlignment='Center'/>
                                            </Border>
                                            <ControlTemplate.Triggers>
                                                <Trigger Property='IsMouseOver' Value='True'>
                                                    <Setter TargetName='Border' Property='Background' Value='#202024'/>
                                                    <Setter Property='Foreground' Value='White'/>
                                                </Trigger>
                                                <Trigger Property='IsSelected' Value='True'>
                                                    <Setter TargetName='Border' Property='Background' Value='#27272a'/>
                                                    <Setter Property='Foreground' Value='#a78bfa'/>
                                                </Trigger>
                                            </ControlTemplate.Triggers>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </ListBox.ItemContainerStyle>
                    </ListBox>
                    
                    <Grid Grid.Row='2'>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width='*'/>
                            <ColumnDefinition Width='8'/>
                            <ColumnDefinition Width='*'/>
                            <ColumnDefinition Width='8'/>
                            <ColumnDefinition Width='*'/>
                        </Grid.ColumnDefinitions>
                        <Button Name='BtnNewFolder' Grid.Column='0' Content='+ Нов. папка' Height='30'/>
                        <Button Name='BtnOk' Grid.Column='2' Content='Выбрать' Style='{StaticResource PrimaryBtn}' Height='30'/>
                        <Button Name='BtnCancel' Grid.Column='4' Content='Отмена' Height='30'/>
                    </Grid>
                </Grid>
            </Border>";
            xaml = xaml.Replace("{TITLE}", titleText);
            Border root = (Border)XamlReader.Parse(xaml);
            w.Content = root;

            ListBox lstSelect = (ListBox)root.FindName("LstFoldersSelect");
            Button btnNew = (Button)root.FindName("BtnNewFolder");
            Button btnOk = (Button)root.FindName("BtnOk");
            Button btnCancel = (Button)root.FindName("BtnCancel");

            // Заполнить список
            Action fillList = () => {
                lstSelect.Items.Clear();
                lstSelect.Items.Add("General");
                string baseDir = GetDemosBaseDir();
                if (Directory.Exists(baseDir))
                {
                    foreach (string d in Directory.GetDirectories(baseDir))
                    {
                        string name = Path.GetFileName(d);
                        if (!name.Equals("General", StringComparison.OrdinalIgnoreCase)) lstSelect.Items.Add(name);
                    }
                }
                lstSelect.SelectedIndex = 0;
            };
            fillList();

            btnNew.Click += (s, e) => {
                string input = ShowInputDialog("Новая папка", "Введите имя новой папки:");
                if (!string.IsNullOrEmpty(input))
                {
                    string name = Regex.Replace(input, @"[\\/:*?""<>|]", "").Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        string targetDir = Path.Combine(GetDemosBaseDir(), name);
                        try
                        {
                            Directory.CreateDirectory(targetDir);
                            fillList();
                            lstSelect.SelectedItem = name;
                        }
                        catch { }
                    }
                }
            };

            string result = "";
            btnOk.Click += (s, e) => {
                result = lstSelect.SelectedItem != null ? lstSelect.SelectedItem.ToString() : "General";
                w.DialogResult = true;
                w.Close();
            };
            btnCancel.Click += (s, e) => { w.DialogResult = false; w.Close(); };

            root.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) w.DragMove(); };

            w.ShowDialog();
            return w.DialogResult == true ? result : "";
        }

        // Подтверждение действия (Да/Нет)
        private bool ShowConfirmDialog(string title, string message)
        {
            Window w = new Window();
            w.Title = title;
            w.Width = 360;
            w.Height = 160;
            w.WindowStyle = WindowStyle.None;
            w.AllowsTransparency = true;
            w.Background = Brushes.Transparent;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            string xaml = @"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    CornerRadius='12' Background='#121214' BorderBrush='#2e2e34' BorderThickness='1' Padding='15'>
                " + GetDialogResourcesXml() + @"
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height='Auto'/>
                        <RowDefinition Height='*'/>
                        <RowDefinition Height='Auto'/>
                    </Grid.RowDefinitions>
                    <TextBlock Text='{TITLE}' Foreground='White' FontWeight='Bold' Margin='0,0,0,10'/>
                    <TextBlock Text='{MESSAGE}' Grid.Row='1' Foreground='#a1a1aa' FontSize='11' TextWrapping='Wrap' Margin='0,0,0,15'/>
                    <StackPanel Grid.Row='2' Orientation='Horizontal' HorizontalAlignment='Right'>
                        <Button Name='Yes' Content='Да' Style='{StaticResource PrimaryBtn}' Width='80' Height='28' Margin='0,0,8,0'/>
                        <Button Name='No' Content='Нет' Width='80' Height='28'/>
                    </StackPanel>
                </Grid>
            </Border>";
            xaml = xaml.Replace("{TITLE}", title).Replace("{MESSAGE}", message);
            Border root = (Border)XamlReader.Parse(xaml);
            w.Content = root;

            Button btnYes = (Button)root.FindName("Yes");
            Button btnNo = (Button)root.FindName("No");

            bool result = false;
            btnYes.Click += (s, e) => { result = true; w.DialogResult = true; w.Close(); };
            btnNo.Click += (s, e) => { result = false; w.DialogResult = false; w.Close(); };

            root.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) w.DragMove(); };

            w.ShowDialog();
            return result;
        }

        // Информационное сообщение (ОК)
        private void ShowMessageDialog(string title, string message, bool isError = false)
        {
            Window w = new Window();
            w.Title = title;
            w.Width = 360;
            w.Height = 160;
            w.WindowStyle = WindowStyle.None;
            w.AllowsTransparency = true;
            w.Background = Brushes.Transparent;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            string btnStyle = isError ? "DangerBtn" : "PrimaryBtn";

            string xaml = @"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    CornerRadius='12' Background='#121214' BorderBrush='#2e2e34' BorderThickness='1' Padding='15'>
                " + GetDialogResourcesXml() + @"
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height='Auto'/>
                        <RowDefinition Height='*'/>
                        <RowDefinition Height='Auto'/>
                    </Grid.RowDefinitions>
                    <TextBlock Text='{TITLE}' Foreground='White' FontWeight='Bold' Margin='0,0,0,10'/>
                    <TextBlock Text='{MESSAGE}' Grid.Row='1' Foreground='#a1a1aa' FontSize='11' TextWrapping='Wrap' Margin='0,0,0,15'/>
                    <StackPanel Grid.Row='2' Orientation='Horizontal' HorizontalAlignment='Right'>
                        <Button Name='Ok' Content='OK' Style='{StaticResource " + btnStyle + @"}' Width='90' Height='28'/>
                    </StackPanel>
                </Grid>
            </Border>";
            xaml = xaml.Replace("{TITLE}", title).Replace("{MESSAGE}", message);
            Border root = (Border)XamlReader.Parse(xaml);
            w.Content = root;

            Button btnOk = (Button)root.FindName("Ok");
            btnOk.Click += (s, e) => { w.DialogResult = true; w.Close(); };

            root.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) w.DragMove(); };

            w.ShowDialog();
        }
    }
}
