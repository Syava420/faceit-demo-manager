using System;
using System.Windows;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private string GetXamlString()
        {
            return @"<Border xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    CornerRadius=""12"" Background=""#121214"" BorderBrush=""#2e2e34"" BorderThickness=""1"">
                <Border.Resources>
                    <Style x:Key=""ModernBtn"" TargetType=""Button"">
                        <Setter Property=""Background"" Value=""#8b5cf6""/>
                        <Setter Property=""Foreground"" Value=""White""/>
                        <Setter Property=""FontWeight"" Value=""Bold""/>
                        <Setter Property=""Padding"" Value=""15,8""/>
                        <Setter Property=""BorderThickness"" Value=""0""/>
                        <Setter Property=""Cursor"" Value=""Hand""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""Button"">
                                    <Border x:Name=""border"" CornerRadius=""15"" Background=""{TemplateBinding Background}"">
                                        <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsMouseOver"" Value=""True"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#a78bfa""/>
                                        </Trigger>
                                        <Trigger Property=""IsEnabled"" Value=""False"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#4b5563""/>
                                            <Setter Property=""Foreground"" Value=""#9ca3af""/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                    <Style x:Key=""TabBtn"" TargetType=""Button"">
                        <Setter Property=""Background"" Value=""Transparent""/>
                        <Setter Property=""Foreground"" Value=""Gray""/>
                        <Setter Property=""FontWeight"" Value=""SemiBold""/>
                        <Setter Property=""Padding"" Value=""15,0""/>
                        <Setter Property=""BorderThickness"" Value=""0""/>
                        <Setter Property=""Cursor"" Value=""Hand""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""Button"">
                                    <Border Name=""border"" CornerRadius=""8"" Background=""{TemplateBinding Background}"">
                                        <ContentPresenter HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Margin=""{TemplateBinding Padding}""/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <!-- Hover when inactive (Background is Transparent) -->
                                        <MultiDataTrigger>
                                            <MultiDataTrigger.Conditions>
                                                <Condition Binding=""{Binding RelativeSource={RelativeSource Self}, Path=IsMouseOver}"" Value=""True""/>
                                                <Condition Binding=""{Binding RelativeSource={RelativeSource Self}, Path=Background}"" Value=""Transparent""/>
                                            </MultiDataTrigger.Conditions>
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#27272a""/>
                                            <Setter Property=""Foreground"" Value=""White""/>
                                        </MultiDataTrigger>
                                        <!-- Hover when active (Background is #8b5cf6) -->
                                        <MultiDataTrigger>
                                            <MultiDataTrigger.Conditions>
                                                <Condition Binding=""{Binding RelativeSource={RelativeSource Self}, Path=IsMouseOver}"" Value=""True""/>
                                                <Condition Binding=""{Binding RelativeSource={RelativeSource Self}, Path=Background}"" Value=""#8b5cf6""/>
                                            </MultiDataTrigger.Conditions>
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#a78bfa""/>
                                            <Setter Property=""Foreground"" Value=""White""/>
                                        </MultiDataTrigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                    <Style x:Key=""RedBtn"" TargetType=""Button"" BasedOn=""{StaticResource ModernBtn}"">
                        <Setter Property=""Background"" Value=""#ef4444""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""Button"">
                                    <Border x:Name=""border"" CornerRadius=""15"" Background=""{TemplateBinding Background}"">
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
                    <Style x:Key=""GreenBtn"" TargetType=""Button"" BasedOn=""{StaticResource ModernBtn}"">
                        <Setter Property=""Background"" Value=""#10b981""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""Button"">
                                    <Border x:Name=""border"" CornerRadius=""15"" Background=""{TemplateBinding Background}"">
                                        <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsMouseOver"" Value=""True"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#34d399""/>
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
                    <Style TargetType=""ComboBox"">
                        <Setter Property=""Background"" Value=""#27272a""/>
                        <Setter Property=""Foreground"" Value=""White""/>
                        <Setter Property=""BorderBrush"" Value=""#3f3f46""/>
                        <Setter Property=""BorderThickness"" Value=""1""/>
                        <Setter Property=""Padding"" Value=""6,4""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""ComboBox"">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""*""/>
                                            <ColumnDefinition Width=""Auto""/>
                                        </Grid.ColumnDefinitions>
                                        <ToggleButton Name=""ToggleButton"" Grid.ColumnSpan=""2"" Focusable=""False"" ClickMode=""Press"" IsChecked=""{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}"">
                                            <ToggleButton.Template>
                                                <ControlTemplate TargetType=""ToggleButton"">
                                                    <Border Name=""MainBorder"" CornerRadius=""8"" Background=""#27272a"" BorderBrush=""#3f3f46"" BorderThickness=""1"">
                                                        <Grid>
                                                            <Grid.ColumnDefinitions>
                                                                <ColumnDefinition Width=""*""/>
                                                                <ColumnDefinition Width=""30""/>
                                                            </Grid.ColumnDefinitions>
                                                            <Path Grid.Column=""1"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Fill=""Gray"" Data=""M 0 0 L 4 4 L 8 0 Z""/>
                                                        </Grid>
                                                    </Border>
                                                    <ControlTemplate.Triggers>
                                                        <Trigger Property=""IsMouseOver"" Value=""True"">
                                                            <Setter TargetName=""MainBorder"" Property=""BorderBrush"" Value=""#8b5cf6""/>
                                                        </Trigger>
                                                        <Trigger Property=""IsChecked"" Value=""True"">
                                                            <Setter TargetName=""MainBorder"" Property=""BorderBrush"" Value=""#8b5cf6""/>
                                                        </Trigger>
                                                        <Trigger Property=""IsEnabled"" Value=""False"">
                                                            <Setter TargetName=""MainBorder"" Property=""Background"" Value=""#18181b""/>
                                                            <Setter TargetName=""MainBorder"" Property=""Opacity"" Value=""0.5""/>
                                                        </Trigger>
                                                    </ControlTemplate.Triggers>
                                                </ControlTemplate>
                                            </ToggleButton.Template>
                                        </ToggleButton>
                                        <ContentPresenter IsHitTestVisible=""False"" Content=""{TemplateBinding SelectionBoxItem}"" ContentTemplate=""{TemplateBinding SelectionBoxItemTemplate}"" ContentTemplateSelector=""{TemplateBinding ItemTemplateSelector}"" Margin=""{TemplateBinding Padding}"" VerticalAlignment=""Center"" HorizontalAlignment=""Left""/>
                                        <Popup x:Name=""PART_Popup"" AllowsTransparency=""True"" Grid.ColumnSpan=""2"" Placement=""Bottom"" IsOpen=""{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}"" Focusable=""False"" PopupAnimation=""Slide"">
                                            <Border x:Name=""DropDownBorder"" Background=""#1c1c1f"" BorderBrush=""#2e2e34"" BorderThickness=""1"" CornerRadius=""8"" MinWidth=""{TemplateBinding ActualWidth}"" MaxHeight=""{TemplateBinding MaxDropDownHeight}"" Margin=""0,2,0,0"">
                                                <ScrollViewer x:Name=""DropDownScrollViewer"" CanContentScroll=""True"" Padding=""2"">
                                                    <ItemsPresenter KeyboardNavigation.DirectionalNavigation=""Contained""/>
                                                </ScrollViewer>
                                            </Border>
                                        </Popup>
                                    </Grid>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsEnabled"" Value=""False"">
                                            <Setter Property=""Foreground"" Value=""#52525b""/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                    <Style TargetType=""CheckBox"">
                        <Setter Property=""Foreground"" Value=""White""/>
                        <Setter Property=""Cursor"" Value=""Hand""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""CheckBox"">
                                    <Grid Background=""Transparent"">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""Auto""/>
                                            <ColumnDefinition Width=""*""/>
                                        </Grid.ColumnDefinitions>
                                        <Border x:Name=""border"" Grid.Column=""0"" Width=""16"" Height=""16"" CornerRadius=""4"" Background=""#27272a"" BorderBrush=""#3f3f46"" BorderThickness=""1"" SnapsToDevicePixels=""True"" VerticalAlignment=""Center"" HorizontalAlignment=""Center"">
                                            <Path x:Name=""checkmark"" Data=""M 3 8 L 7 11 L 13 4"" Stroke=""White"" StrokeThickness=""2"" StrokeStartLineCap=""Round"" StrokeEndLineCap=""Round"" StrokeLineJoin=""Round"" Visibility=""Collapsed"" HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                        </Border>
                                        <ContentPresenter Grid.Column=""1"" Margin=""8,0,0,0"" VerticalAlignment=""Center"" HorizontalAlignment=""Left""/>
                                    </Grid>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsChecked"" Value=""True"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#8b5cf6""/>
                                            <Setter TargetName=""border"" Property=""BorderBrush"" Value=""#8b5cf6""/>
                                            <Setter TargetName=""checkmark"" Property=""Visibility"" Value=""Visible""/>
                                        </Trigger>
                                        <Trigger Property=""IsMouseOver"" Value=""True"">
                                            <Setter TargetName=""border"" Property=""BorderBrush"" Value=""#a78bfa""/>
                                        </Trigger>
                                        <Trigger Property=""IsEnabled"" Value=""False"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#18181b""/>
                                            <Setter TargetName=""border"" Property=""Opacity"" Value=""0.5""/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>

                    <!-- DataGrid Style -->
                    <Style TargetType=""DataGrid"">
                        <Setter Property=""Background"" Value=""#18181b""/>
                        <Setter Property=""RowBackground"" Value=""#18181b""/>
                        <Setter Property=""Foreground"" Value=""White""/>
                        <Setter Property=""BorderThickness"" Value=""0""/>
                        <Setter Property=""GridLinesVisibility"" Value=""Horizontal""/>
                        <Setter Property=""HorizontalGridLinesBrush"" Value=""#27272a""/>
                        <Setter Property=""VerticalGridLinesBrush"" Value=""Transparent""/>
                        <Setter Property=""RowHeaderWidth"" Value=""0""/>
                        <Setter Property=""CanUserAddRows"" Value=""False""/>
                        <Setter Property=""CanUserDeleteRows"" Value=""False""/>
                        <Setter Property=""CanUserReorderColumns"" Value=""False""/>
                        <Setter Property=""CanUserResizeRows"" Value=""False""/>
                        <Setter Property=""IsReadOnly"" Value=""True""/>
                        <Setter Property=""SelectionMode"" Value=""Extended""/>
                    </Style>

                    <!-- DataGrid ColumnHeader Style -->
                    <Style TargetType=""DataGridColumnHeader"">
                        <Setter Property=""Background"" Value=""#0b0b0d""/>
                        <Setter Property=""Foreground"" Value=""#a1a1aa""/>
                        <Setter Property=""FontWeight"" Value=""SemiBold""/>
                        <Setter Property=""Padding"" Value=""10,8""/>
                        <Setter Property=""BorderThickness"" Value=""0,0,0,1""/>
                        <Setter Property=""BorderBrush"" Value=""#27272a""/>
                        <Setter Property=""FontSize"" Value=""11""/>
                    </Style>

                    <!-- DataGrid Cell Style -->
                    <Style TargetType=""DataGridCell"">
                        <Setter Property=""Padding"" Value=""10,8""/>
                        <Setter Property=""BorderThickness"" Value=""0""/>
                        <Setter Property=""Background"" Value=""Transparent""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""DataGridCell"">
                                    <Border Padding=""{TemplateBinding Padding}"" Background=""{TemplateBinding Background}"" SnapsToDevicePixels=""True"">
                                        <ContentPresenter SnapsToDevicePixels=""True"" VerticalAlignment=""Center""/>
                                    </Border>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                        <Style.Triggers>
                            <Trigger Property=""IsSelected"" Value=""True"">
                                <Setter Property=""Background"" Value=""#201b30""/>
                                <Setter Property=""Foreground"" Value=""#c084fc""/>
                            </Trigger>
                        </Style.Triggers>
                    </Style>

                    <!-- DataGrid Row Style -->
                    <Style TargetType=""DataGridRow"">
                        <Setter Property=""BorderThickness"" Value=""0""/>
                        <Setter Property=""Margin"" Value=""0,1""/>
                        <Style.Triggers>
                            <Trigger Property=""IsSelected"" Value=""True"">
                                <Setter Property=""Background"" Value=""#201b30""/>
                            </Trigger>
                            <Trigger Property=""IsMouseOver"" Value=""True"">
                                <Setter Property=""Background"" Value=""#1c1924""/>
                            </Trigger>
                        </Style.Triggers>
                    </Style>
                </Border.Resources>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height=""45""/>
                        <RowDefinition Height=""*""/>
                        <RowDefinition Height=""30""/>
                    </Grid.RowDefinitions>
                    
                    <!-- TitleBar -->
                    <Grid Grid.Row=""0"" Name=""TitleBar"" Background=""#0f0f11"" Cursor=""SizeAll"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*"" />
                            <ColumnDefinition Width=""Auto"" />
                        </Grid.ColumnDefinitions>
                        <StackPanel Orientation=""Horizontal"" Grid.Column=""0"" Margin=""15,0,0,0"" VerticalAlignment=""Center"">
                            <TextBlock Text=""🎯"" Foreground=""#8b5cf6"" FontWeight=""Bold"" FontSize=""16"" Margin=""0,0,8,0""/>
                            <TextBlock Text=""FACEIT Demo Hub"" Foreground=""#a78bfa"" FontWeight=""Bold"" FontSize=""15""/>
                        </StackPanel>
                        <StackPanel Grid.Column=""1"" Orientation=""Horizontal"" Margin=""0,0,10,0"" VerticalAlignment=""Center"">
                            <Button Name=""BtnMinimize"" Content=""—"" Width=""30"" Height=""25"" Background=""Transparent"" Foreground=""Gray"" BorderThickness=""0"" Cursor=""Hand"" Margin=""0,0,5,0""/>
                            <Button Name=""BtnClose"" Content=""✕"" Width=""30"" Height=""25"" Background=""Transparent"" Foreground=""Gray"" BorderThickness=""0"" Cursor=""Hand""/>
                        </StackPanel>
                    </Grid>
                    
                    <!-- Main Body Layout -->
                    <Grid Grid.Row=""1"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""220""/>
                            <ColumnDefinition Width=""*""/>
                        </Grid.ColumnDefinitions>
                        
                        <!-- Left Navigation Sidebar -->
                        <Border Grid.Column=""0"" Background=""#0b0b0d"" BorderBrush=""#1f1f23"" BorderThickness=""0,0,1,0"">
                            <Grid Margin=""15,20,15,15"">
                                <Grid.RowDefinitions>
                                    <RowDefinition Height=""Auto""/> <!-- TabImport (Row 0) -->
                                    <RowDefinition Height=""5""/>
                                    <RowDefinition Height=""Auto""/> <!-- TabLibrary (Row 2) -->
                                    <RowDefinition Height=""5""/>
                                    <RowDefinition Height=""Auto""/> <!-- TabBinds (Row 4) -->
                                    <RowDefinition Height=""15""/>
                                    <RowDefinition Height=""Auto""/> <!-- Section Header (Row 6) -->
                                    <RowDefinition Height=""5""/>
                                    <RowDefinition Height=""*""/>    <!-- ListBox (Row 8) -->
                                </Grid.RowDefinitions>
                                
                                <Button Name=""TabImport"" Grid.Row=""0"" Content=""⚡ Распаковка демок"" Style=""{StaticResource TabBtn}"" Background=""#8b5cf6"" Foreground=""White"" Height=""35"" Padding=""15,0""/>
                                <Button Name=""TabLibrary"" Grid.Row=""2"" Content=""📚 Библиотека матчей"" Style=""{StaticResource TabBtn}"" Background=""Transparent"" Foreground=""Gray"" Height=""35"" Padding=""15,0""/>
                                <Button Name=""TabBinds"" Grid.Row=""4"" Content=""⌨️ Бинды для CS2"" Style=""{StaticResource TabBtn}"" Background=""Transparent"" Foreground=""Gray"" Height=""35"" Padding=""15,0""/>
                                
                                <!-- Folder Categories Section Header (only relevant for Library tab) -->
                                <TextBlock Name=""SidebarCategoriesHeader"" Grid.Row=""6"" Text=""ИГРОКИ / КАТЕГОРИИ"" Foreground=""#52525b"" FontSize=""10"" FontWeight=""Bold"" Visibility=""Collapsed""/>
                                
                                <!-- Folder Categories list & action controls -->
                                <Grid Name=""SidebarLibraryControls"" Grid.Row=""8"" Visibility=""Collapsed"">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height=""*""/>
                                        <RowDefinition Height=""10""/>
                                        <RowDefinition Height=""Auto""/>
                                    </Grid.RowDefinitions>
                                    
                                    <ListBox Name=""LstFolders"" Grid.Row=""0"" Background=""Transparent"" BorderThickness=""0"" ScrollViewer.HorizontalScrollBarVisibility=""Disabled"">
                                        <ListBox.ItemContainerStyle>
                                            <Style TargetType=""ListBoxItem"">
                                                <Setter Property=""Foreground"" Value=""Gray""/>
                                                <Setter Property=""Padding"" Value=""10,8""/>
                                                <Setter Property=""Margin"" Value=""0,2""/>
                                                <Setter Property=""Cursor"" Value=""Hand""/>
                                                <Setter Property=""Template"">
                                                    <Setter.Value>
                                                        <ControlTemplate TargetType=""ListBoxItem"">
                                                            <Border Name=""Border"" CornerRadius=""8"" Background=""Transparent"" Padding=""{TemplateBinding Padding}"">
                                                                <ContentPresenter VerticalAlignment=""Center""/>
                                                            </Border>
                                                            <ControlTemplate.Triggers>
                                                                <Trigger Property=""IsMouseOver"" Value=""True"">
                                                                    <Setter TargetName=""Border"" Property=""Background"" Value=""#18181b""/>
                                                                    <Setter Property=""Foreground"" Value=""White""/>
                                                                </Trigger>
                                                                <Trigger Property=""IsSelected"" Value=""True"">
                                                                    <Setter TargetName=""Border"" Property=""Background"" Value=""#27272a""/>
                                                                    <Setter Property=""Foreground"" Value=""#a78bfa""/>
                                                                </Trigger>
                                                            </ControlTemplate.Triggers>
                                                        </ControlTemplate>
                                                    </Setter.Value>
                                                </Setter>
                                            </Style>
                                        </ListBox.ItemContainerStyle>
                                    </ListBox>
                                    
                                    <Grid Grid.Row=""2"">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""*""/>
                                            <ColumnDefinition Width=""8""/>
                                            <ColumnDefinition Width=""*""/>
                                        </Grid.ColumnDefinitions>
                                        <Button Name=""BtnNewCategory"" Grid.Column=""0"" Content=""📁 + Папка"" Style=""{StaticResource ModernBtn}"" Background=""#27272a"" Foreground=""White"" Height=""30""/>
                                        <Button Name=""BtnDeleteCategory"" Grid.Column=""2"" Content=""🗑️ - Папка"" Style=""{StaticResource RedBtn}"" Height=""30""/>
                                    </Grid>
                                </Grid>
                            </Grid>
                        </Border>
                        
                        <!-- Right Tab Contents -->
                        <Grid Grid.Column=""1"">
                            
                            <!-- Tab 1: Unpack and Import Content -->
                            <Grid Name=""GridImportTab"" Margin=""20"" Visibility=""Visible"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""20""/>
                                    <ColumnDefinition Width=""380""/>
                                </Grid.ColumnDefinitions>
                                
                                <!-- Left Column of Tab 1: Drag & Drop + Log Console -->
                                <Grid Grid.Column=""0"">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height=""200""/>
                                        <RowDefinition Height=""25""/>
                                        <RowDefinition Height=""*""/>
                                    </Grid.RowDefinitions>
                                    
                                    <Border Name=""DragDropZone"" Grid.Row=""0"" BorderBrush=""#8b5cf6"" BorderThickness=""2"" CornerRadius=""15"" Background=""#18181b"" Cursor=""Hand"">
                                        <StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center"">
                                            <TextBlock Text=""📥"" FontSize=""45"" HorizontalAlignment=""Center"" Foreground=""#8b5cf6"" Margin=""0,0,0,10""/>
                                            <TextBlock Text=""Перетащите сюда демку .dem.zst или .dem"" FontSize=""15"" FontWeight=""Bold"" Foreground=""White"" HorizontalAlignment=""Center""/>
                                            <TextBlock Text=""или нажмите сюда для ручного выбора файла"" FontSize=""11"" Foreground=""#71717a"" HorizontalAlignment=""Center"" Margin=""0,5,0,0""/>
                                        </StackPanel>
                                    </Border>
                                    
                                    <TextBlock Grid.Row=""1"" Text=""КОНСОЛЬ ЛОГОВ (процесс распаковки)"" Foreground=""#71717a"" FontSize=""10"" FontWeight=""Bold"" VerticalAlignment=""Bottom"" Margin=""0,0,0,4""/>
                                    
                                    <Border Grid.Row=""2"" Background=""#0b0b0d"" BorderBrush=""#27272a"" BorderThickness=""1"" CornerRadius=""12"" Padding=""10"">
                                        <TextBox Name=""TxtLogConsole"" Background=""Transparent"" Foreground=""#10b981"" FontFamily=""Consolas"" BorderThickness=""0"" VerticalScrollBarVisibility=""Auto"" IsReadOnly=""True"" AcceptsReturn=""True"" TextWrapping=""Wrap""/>
                                    </Border>
                                </Grid>
                                
                                <!-- Right Column of Tab 1: Paths & Mode settings -->
                                <Grid Grid.Column=""2"">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height=""*"" />
                                        <RowDefinition Height=""Auto"" />
                                    </Grid.RowDefinitions>
                                    
                                    <Border Grid.Row=""0"" Background=""#121214"" BorderBrush=""#1f1f23"" BorderThickness=""1"" CornerRadius=""12"" Padding=""15"">
                                        <StackPanel>
                                            <TextBlock Text=""НАСТРОЙКИ И ИМПОРТ"" Foreground=""#8b5cf6"" FontSize=""13"" FontWeight=""Bold"" Margin=""0,0,0,15""/>
                                            
                                            <TextBlock Text=""Папка загрузок (Downloads)"" Foreground=""#a1a1aa"" FontSize=""10"" FontWeight=""Bold"" Margin=""0,0,0,2""/>
                                            <Grid Margin=""0,0,0,10"">
                                                <Grid.ColumnDefinitions>
                                                    <ColumnDefinition Width=""*"" />
                                                    <ColumnDefinition Width=""35"" />
                                                </Grid.ColumnDefinitions>
                                                <TextBox Name=""TxtDownloads"" Height=""25"" VerticalContentAlignment=""Center""/>
                                                <Button Name=""BtnBrowseDownloads"" Grid.Column=""1"" Content=""..."" Style=""{StaticResource ModernBtn}"" Height=""25"" Margin=""5,0,0,0""/>
                                            </Grid>
                                            
                                            <TextBlock Text=""Путь к CS2 (game\csgo)"" Foreground=""#a1a1aa"" FontSize=""10"" FontWeight=""Bold"" Margin=""0,0,0,2""/>
                                            <Grid Margin=""0,0,0,10"">
                                                <Grid.ColumnDefinitions>
                                                    <ColumnDefinition Width=""*"" />
                                                    <ColumnDefinition Width=""Auto"" />
                                                    <ColumnDefinition Width=""35"" />
                                                </Grid.ColumnDefinitions>
                                                <TextBox Name=""TxtCS2"" Grid.Column=""0"" Height=""25"" VerticalContentAlignment=""Center""/>
                                                <Button Name=""BtnAutoCS2"" Grid.Column=""1"" Content=""Авто-поиск"" Style=""{StaticResource ModernBtn}"" Height=""25"" Padding=""8,2"" Margin=""5,0,0,0""/>
                                                <Button Name=""BtnBrowseCS2"" Grid.Column=""2"" Content=""..."" Style=""{StaticResource ModernBtn}"" Height=""25"" Margin=""5,0,0,0""/>
                                            </Grid>
                                            
                                            <TextBlock Text=""Никнейм на Faceit (для парсинга K/D)"" Foreground=""#a1a1aa"" FontSize=""10"" FontWeight=""Bold"" Margin=""0,0,0,2""/>
                                            <TextBox Name=""TxtNickname"" Height=""25"" VerticalContentAlignment=""Center"" Margin=""0,0,0,15""/>
                                            
                                            <!-- Параметры импорта -->
                                            <TextBlock Text=""Режим авто-импорта демок:"" Foreground=""#a1a1aa"" FontSize=""10"" FontWeight=""Bold"" Margin=""0,0,0,2""/>
                                            <ComboBox Name=""CboImportMode"" Height=""25"" Margin=""0,0,0,8"" Background=""#27272a"" Foreground=""White"">
                                                <ComboBoxItem Content=""Всегда в общую (General)"" Tag=""General"" IsSelected=""True""/>
                                                <ComboBoxItem Content=""Спрашивать при импорте"" Tag=""Ask""/>
                                                <ComboBoxItem Content=""В выбранную папку..."" Tag=""Specific""/>
                                            </ComboBox>
                                            
                                            <TextBlock Text=""Выбранная папка для импорта:"" Foreground=""#a1a1aa"" FontSize=""10"" FontWeight=""Bold"" Margin=""0,0,0,2""/>
                                            <ComboBox Name=""CboImportFolder"" Height=""25"" Margin=""0,0,0,15"" Background=""#27272a"" Foreground=""White"" IsEnabled=""False""/>
                                            
                                            <CheckBox Name=""ChkWatchFolder"" Content=""Авто-сканирование загрузок"" Margin=""0,0,0,8""/>
                                            <CheckBox Name=""ChkTray"" Content=""Сворачивать в трей"" Margin=""0,0,0,8""/>
                                            <CheckBox Name=""ChkVoiceInDemos"" Content=""Включить войс-чат в демках""/>
                                        </StackPanel>
                                    </Border>
                                    
                                    <Button Name=""BtnProcess"" Grid.Row=""1"" Content=""🔄 Запустить ручное сканирование"" Style=""{StaticResource GreenBtn}"" Height=""35"" Margin=""0,15,0,0""/>
                                </Grid>
                            </Grid>
                            
                            <!-- Tab 2: Demo Library Content -->
                            <Grid Name=""GridLibraryTab"" Margin=""20"" Visibility=""Collapsed"">
                                <Grid.RowDefinitions>
                                    <RowDefinition Height=""40""/>
                                    <RowDefinition Height=""*""/>
                                    <RowDefinition Height=""Auto""/>
                                </Grid.RowDefinitions>
                                
                                <!-- Top Search & Controls Row -->
                                <Grid Grid.Row=""0"" Margin=""0,0,0,10"">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width=""200""/>
                                        <ColumnDefinition Width=""*""/>
                                        <ColumnDefinition Width=""Auto""/>
                                    </Grid.ColumnDefinitions>
                                    
                                    <StackPanel Grid.Column=""0"" Orientation=""Horizontal"" VerticalAlignment=""Center"">
                                        <TextBlock Text=""🔍"" Foreground=""#71717a"" VerticalAlignment=""Center"" Margin=""0,0,5,0""/>
                                        <TextBox Name=""TxtSearch"" Width=""170"" Height=""25"" VerticalContentAlignment=""Center""/>
                                    </StackPanel>
                                    
                                    <StackPanel Grid.Column=""2"" Orientation=""Horizontal"">
                                        <Button Name=""BtnPlay"" Content=""▶  Запустить в CS2"" Style=""{StaticResource GreenBtn}"" Width=""160"" Height=""30"" Margin=""0,0,8,0""/>
                                        <Button Name=""BtnMoveDemo"" Content=""📁 Перенести"" Style=""{StaticResource ModernBtn}"" Width=""120"" Height=""30"" Margin=""0,0,8,0""/>
                                        <Button Name=""BtnDeleteDemo"" Content=""🗑️ Удалить"" Style=""{StaticResource RedBtn}"" Width=""90"" Height=""30""/>
                                    </StackPanel>
                                </Grid>
                                
                                <!-- Matches DataGrid -->
                                <Border Grid.Row=""1"" Background=""#18181b"" CornerRadius=""12"" BorderThickness=""1"" BorderBrush=""#27272a"" Padding=""5"" Margin=""0,0,0,12"">
                                    <DataGrid Name=""DgvDemos"" AutoGenerateColumns=""False"" HeadersVisibility=""Column"" SelectionMode=""Extended"">
                                        <DataGrid.Columns>
                                            <!-- Результат / Счет -->
                                            <DataGridTemplateColumn Header=""Результат"" Width=""80"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <Border CornerRadius=""6"" Padding=""6,3"" HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Margin=""3,0,0,0"">
                                                            <Border.Style>
                                                                <Style TargetType=""Border"">
                                                                    <Style.Triggers>
                                                                        <DataTrigger Binding=""{Binding IsWin}"" Value=""True"">
                                                                            <Setter Property=""Background"" Value=""#166534""/>
                                                                        </DataTrigger>
                                                                        <DataTrigger Binding=""{Binding IsWin}"" Value=""False"">
                                                                            <Setter Property=""Background"" Value=""#991b1b""/>
                                                                        </DataTrigger>
                                                                    </Style.Triggers>
                                                                </Style>
                                                            </Border.Style>
                                                            <TextBlock Text=""{Binding ScoreText}"" FontWeight=""Bold"" FontSize=""11"" HorizontalAlignment=""Center"">
                                                                <TextBlock.Style>
                                                                    <Style TargetType=""TextBlock"">
                                                                        <Setter Property=""Foreground"" Value=""White""/>
                                                                        <Style.Triggers>
                                                                            <DataTrigger Binding=""{Binding IsWin}"" Value=""True"">
                                                                                <Setter Property=""Foreground"" Value=""#4ade80""/>
                                                                            </DataTrigger>
                                                                            <DataTrigger Binding=""{Binding IsWin}"" Value=""False"">
                                                                                <Setter Property=""Foreground"" Value=""#f87171""/>
                                                                            </DataTrigger>
                                                                        </Style.Triggers>
                                                                    </Style>
                                                                </TextBlock.Style>
                                                            </TextBlock>
                                                        </Border>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- Карта -->
                                            <DataGridTemplateColumn Header=""Карта"" Width=""100"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text=""{Binding Map}"" FontWeight=""SemiBold"" Foreground=""White"" VerticalAlignment=""Center"" Margin=""5,0,0,0""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- K/D Ratio -->
                                            <DataGridTemplateColumn Header=""K/D"" Width=""60"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text=""{Binding KDRatio}"" FontWeight=""Bold"" FontSize=""12"" VerticalAlignment=""Center"" Margin=""5,0,0,0"">
                                                            <TextBlock.Style>
                                                                <Style TargetType=""TextBlock"">
                                                                    <Setter Property=""Foreground"" Value=""White""/>
                                                                    <Style.Triggers>
                                                                        <DataTrigger Binding=""{Binding KDStatus}"" Value=""High"">
                                                                            <Setter Property=""Foreground"" Value=""#eab308""/>
                                                                        </DataTrigger>
                                                                        <DataTrigger Binding=""{Binding KDStatus}"" Value=""Low"">
                                                                            <Setter Property=""Foreground"" Value=""#a1a1aa""/>
                                                                        </DataTrigger>
                                                                    </Style.Triggers>
                                                                </Style>
                                                            </TextBlock.Style>
                                                        </TextBlock>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- K/D/A -->
                                            <DataGridTemplateColumn Header=""K / D / A"" Width=""85"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text=""{Binding KDA}"" Foreground=""#e4e4e7"" FontSize=""11"" VerticalAlignment=""Center"" Margin=""5,0,0,0""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- ADR -->
                                            <DataGridTemplateColumn Header=""ADR"" Width=""55"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text=""{Binding ADR}"" Foreground=""#a1a1aa"" FontSize=""11"" VerticalAlignment=""Center"" Margin=""5,0,0,0""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- Дата и время -->
                                            <DataGridTemplateColumn Header=""Дата игры"" Width=""95"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text=""{Binding DateFormatted}"" Foreground=""#a1a1aa"" FontSize=""10"" VerticalAlignment=""Center"" Margin=""5,0,0,0"" LineHeight=""14""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- Категория (Папка) -->
                                            <DataGridTemplateColumn Header=""Папка"" Width=""80"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <Border Background=""#27272a"" CornerRadius=""4"" Padding=""6,2"" HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Margin=""5,0,0,0"">
                                                            <TextBlock Text=""{Binding Folder}"" Foreground=""#e4e4e7"" FontSize=""10"" VerticalAlignment=""Center""/>
                                                        </Border>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- Заметка -->
                                            <DataGridTemplateColumn Header=""Заметка"" Width=""*"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text=""{Binding Note}"" Foreground=""#71717a"" FontSize=""11"" TextTrimming=""CharacterEllipsis"" VerticalAlignment=""Center"" Margin=""5,0,0,0""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>
                                        </DataGrid.Columns>
                                    </DataGrid>
                                </Border>
                                
                                <!-- Bottom Stats Editor row -->
                                <StackPanel Grid.Row=""2"">
                                    <TextBlock Text=""РЕДАКТИРОВАНИЕ МАТЧА (выберите матч в таблице для редактирования)"" Foreground=""#71717a"" FontSize=""10"" FontWeight=""Bold"" Margin=""0,0,0,4""/>
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""120""/>
                                            <ColumnDefinition Width=""10""/>
                                            <ColumnDefinition Width=""100""/>
                                            <ColumnDefinition Width=""10""/>
                                            <ColumnDefinition Width=""100""/>
                                            <ColumnDefinition Width=""10""/>
                                            <ColumnDefinition Width=""110""/>
                                            <ColumnDefinition Width=""20""/>
                                            <ColumnDefinition Width=""*""/>
                                        </Grid.ColumnDefinitions>
                                        
                                        <StackPanel Grid.Column=""0"">
                                            <TextBlock Text=""Карта"" Foreground=""#a1a1aa"" FontSize=""9"" Margin=""0,0,0,2""/>
                                            <TextBox Name=""TxtEditMap"" Height=""25"" VerticalContentAlignment=""Center""/>
                                        </StackPanel>
                                        
                                        <StackPanel Grid.Column=""2"">
                                            <TextBlock Text=""Счет"" Foreground=""#a1a1aa"" FontSize=""9"" Margin=""0,0,0,2""/>
                                            <TextBox Name=""TxtEditScore"" Height=""25"" VerticalContentAlignment=""Center""/>
                                        </StackPanel>
                                        
                                        <StackPanel Grid.Column=""4"">
                                            <TextBlock Text=""K/D"" Foreground=""#a1a1aa"" FontSize=""9"" Margin=""0,0,0,2""/>
                                            <TextBox Name=""TxtEditKD"" Height=""25"" VerticalContentAlignment=""Center""/>
                                        </StackPanel>
                                        
                                        <StackPanel Grid.Column=""6"">
                                            <TextBlock Text=""Дата"" Foreground=""#a1a1aa"" FontSize=""9"" Margin=""0,0,0,2""/>
                                            <TextBox Name=""TxtEditDate"" Height=""25"" VerticalContentAlignment=""Center""/>
                                        </StackPanel>
                                        
                                        <StackPanel Grid.Column=""8"">
                                            <TextBlock Text=""Заметка к матчу"" Foreground=""#a1a1aa"" FontSize=""9"" Margin=""0,0,0,2""/>
                                            <TextBox Name=""TxtNoteEdit"" Height=""25"" VerticalContentAlignment=""Center""/>
                                        </StackPanel>
                                    </Grid>
                                </StackPanel>
                            </Grid>
                            
                            <!-- Tab 3: Demo Binds Content -->
                            <Grid Name=""GridBindsTab"" Margin=""20"" Visibility=""Collapsed"">
                                <Grid.RowDefinitions>
                                    <RowDefinition Height=""Auto""/>
                                    <RowDefinition Height=""10""/>
                                    <RowDefinition Height=""*""/>
                                    <RowDefinition Height=""Auto""/>
                                </Grid.RowDefinitions>
                                
                                <TextBlock Grid.Row=""0"" Text=""УПРАВЛЕНИЕ БИНДАМИ В CS2"" Foreground=""#8b5cf6"" FontSize=""15"" FontWeight=""Bold""/>
                                <TextBlock Grid.Row=""1"" Text=""Настройте горячие клавиши для управления просмотром демок. Бинды автоматически применятся при запуске игры."" Foreground=""#71717a"" FontSize=""11"" TextWrapping=""Wrap""/>
                                
                                <Border Grid.Row=""2"" Background=""#18181b"" CornerRadius=""12"" BorderThickness=""1"" BorderBrush=""#27272a"" Padding=""5"" Margin=""0,10,0,15"">
                                    <DataGrid Name=""DgvBinds"" AutoGenerateColumns=""False"" HeadersVisibility=""Column"" SelectionMode=""Single"">
                                        <DataGrid.Columns>
                                            <!-- Включен ли бинд -->
                                            <DataGridTemplateColumn Header=""Вкл"" Width=""50"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <CheckBox IsChecked=""{Binding IsEnabled, UpdateSourceTrigger=PropertyChanged}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- Название действия -->
                                            <DataGridTemplateColumn Header=""Действие в игре"" Width=""250"" IsReadOnly=""True"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text=""{Binding ActionName}"" Foreground=""White"" FontWeight=""SemiBold"" VerticalAlignment=""Center"" Margin=""5,0,0,0""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>

                                            <!-- Клавиша (Hotkey) -->
                                            <DataGridTemplateColumn Header=""Клавиша (например: p, j, leftarrow)"" Width=""*"" IsReadOnly=""False"">
                                                <DataGridTemplateColumn.CellTemplate>
                                                    <DataTemplate>
                                                        <TextBox Text=""{Binding Key, UpdateSourceTrigger=PropertyChanged}"" Height=""25"" VerticalContentAlignment=""Center"" Margin=""2""/>
                                                    </DataTemplate>
                                                </DataGridTemplateColumn.CellTemplate>
                                            </DataGridTemplateColumn>
                                        </DataGrid.Columns>
                                    </DataGrid>
                                </Border>
                                
                                <Grid Grid.Row=""3"">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width=""*""/>
                                        <ColumnDefinition Width=""Auto""/>
                                    </Grid.ColumnDefinitions>
                                    <CheckBox Name=""ChkAutoApplyBinds"" Content=""Автоматически применять бинды при запуске демок"" IsChecked=""True"" VerticalAlignment=""Center""/>
                                    <Button Name=""BtnResetBinds"" Grid.Column=""1"" Content=""🔄 Сбросить бинды по умолчанию"" Style=""{StaticResource ModernBtn}"" Height=""32"" Padding=""15,0""/>
                                </Grid>
                            </Grid>
                        </Grid>
                    </Grid>
                    
                    <!-- Footer Status Bar -->
                    <Grid Grid.Row=""2"" Background=""#0f0f11"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""150""/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Name=""LblStatus"" Grid.Column=""0"" Text=""Готов к работе"" Foreground=""#a1a1aa"" VerticalAlignment=""Center"" Margin=""15,0,0,0""/>
                        <ProgressBar Name=""PrgBar"" Grid.Column=""1"" Height=""8"" Foreground=""#8b5cf6"" Background=""#27272a"" BorderThickness=""0"" VerticalAlignment=""Center"" Margin=""0,0,15,0""/>
                    </Grid>
                </Grid>
            </Border>";
        }
    }
}
