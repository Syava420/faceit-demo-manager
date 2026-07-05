using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

class Program
{
    static void Main()
    {
        Console.Title = "Установка FACEIT Demo Hub";
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("==================================================");
        Console.WriteLine("       УСТАНОВКА FACEIT DEMO HUB");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.WriteLine();
        
        string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FaceitDemoHub");
        
        try
        {
            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
                Console.WriteLine("Создана папка установки: " + installDir);
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Распаковка файлов программы...");
            Console.ResetColor();
            
            ExtractResource("FaceitDemoManager.exe", Path.Combine(installDir, "FaceitDemoManager.exe"));
            Console.WriteLine(" -> FaceitDemoManager.exe [OK]");
            
            ExtractResource("zstd.exe", Path.Combine(installDir, "zstd.exe"));
            Console.WriteLine(" -> zstd.exe [OK]");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Создание ярлыка на Рабочем столе...");
            Console.ResetColor();
            CreateShortcut(Path.Combine(installDir, "FaceitDemoManager.exe"));
            Console.WriteLine(" -> Ярлык на рабочем столе создан [OK]");
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Установка успешно завершена!");
            Console.WriteLine("Запуск FACEIT Demo Hub...");
            Console.ResetColor();
            
            Process.Start(Path.Combine(installDir, "FaceitDemoManager.exe"));
            
            // Wait 2 seconds before closing console
            Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Критическая ошибка при установке: " + ex.Message);
            Console.ResetColor();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
    
    static void ExtractResource(string resourceName, string destPath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new Exception("Ресурс " + resourceName + " не найден в установщике!");
            }
            using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fs);
            }
        }
    }
    
    static void CreateShortcut(string targetPath)
    {
        try
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktop, "FACEIT Demo Hub.lnk");
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Description = "FACEIT Demo Hub - Менеджер демок Faceit";
            shortcut.Save();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Предупреждение: Не удалось создать ярлык: " + ex.Message);
        }
    }
}
