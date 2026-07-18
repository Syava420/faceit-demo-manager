using System;
using System.IO;
using System.Collections.Generic;

namespace FaceitDemoManager
{
    public static class LibraryFileService
    {
        public static List<string> GetCategoryFolders(string baseDir)
        {
            var list = new List<string>();
            list.Add("General");
            if (Directory.Exists(baseDir))
            {
                foreach (string dir in Directory.GetDirectories(baseDir))
                {
                    string name = Path.GetFileName(dir);
                    if (!name.Equals("General", StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(name);
                    }
                }
            }
            return list;
        }
        public static void LoadSubfoldersRecursive(
            string baseDir, 
            string relativePath, 
            int depth, 
            List<FolderItem> result, 
            HashSet<string> collapsedFolders)
        {
            string currentFullDir = string.IsNullOrEmpty(relativePath) ? baseDir : Path.Combine(baseDir, relativePath);
            if (!Directory.Exists(currentFullDir)) return;

            string[] subdirs;
            try
            {
                subdirs = Directory.GetDirectories(currentFullDir);
            }
            catch
            {
                return;
            }

            Array.Sort(subdirs);

            foreach (string subdir in subdirs)
            {
                string dirName = Path.GetFileName(subdir);
                string subRelPath = string.IsNullOrEmpty(relativePath) ? dirName : relativePath + "/" + dirName;
                
                // Check if this subdirectory itself has subfolders
                bool hasChildren = false;
                try
                {
                    hasChildren = Directory.GetDirectories(subdir).Length > 0;
                }
                catch { }

                // Check if any parent folder of this item is collapsed
                bool parentCollapsed = false;
                if (!string.IsNullOrEmpty(relativePath))
                {
                    string[] parts = relativePath.Split('/');
                    string currentParent = "";
                    foreach (string part in parts)
                    {
                        currentParent = string.IsNullOrEmpty(currentParent) ? part : currentParent + "/" + part;
                        if (collapsedFolders.Contains(currentParent))
                        {
                            parentCollapsed = true;
                            break;
                        }
                    }
                }

                if (parentCollapsed)
                {
                    continue; // Skip rendering this item because its parent is collapsed!
                }

                // Determine expand/collapse symbol
                string indicator = "";
                if (hasChildren)
                {
                    indicator = collapsedFolders.Contains(subRelPath) ? "▶ " : "▼ ";
                }
                else if (depth > 0)
                {
                    indicator = "└─ ";
                }

                // Construct display name with narrower spacing (3 spaces per depth)
                string prefix = "";
                if (depth > 0)
                {
                    prefix = new string(' ', (depth - 1) * 3);
                }
                
                result.Add(new FolderItem 
                { 
                    DisplayName = prefix + indicator + dirName, 
                    RelativePath = subRelPath, 
                    Depth = depth 
                });

                // Only recurse if this folder itself is NOT collapsed!
                if (!collapsedFolders.Contains(subRelPath))
                {
                    LoadSubfoldersRecursive(baseDir, subRelPath, depth + 1, result, collapsedFolders);
                }
            }
        }

        public static bool CreateFolder(string baseDir, string relativePath)
        {
            try
            {
                string fullPath = Path.Combine(baseDir, relativePath);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool DeleteFolder(string baseDir, string relativePath)
        {
            try
            {
                string fullPath = Path.Combine(baseDir, relativePath);
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool RenameFolder(string baseDir, string oldRelativePath, string newRelativePath)
        {
            try
            {
                string oldPath = Path.Combine(baseDir, oldRelativePath);
                string newPath = Path.Combine(baseDir, newRelativePath);
                if (Directory.Exists(oldPath) && !Directory.Exists(newPath))
                {
                    Directory.Move(oldPath, newPath);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool MoveDemoFile(string baseDir, string demoFilePath, string destFolderName, out string newRelativePath)
        {
            newRelativePath = null;
            try
            {
                string fileName = Path.GetFileName(demoFilePath);
                string destDir = Path.Combine(baseDir, destFolderName);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                string destPath = Path.Combine(destDir, fileName);
                if (demoFilePath.Equals(destPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (File.Exists(demoFilePath))
                {
                    if (File.Exists(destPath)) File.Delete(destPath);
                    File.Move(demoFilePath, destPath);
                    newRelativePath = Path.Combine(destFolderName, fileName).Replace('\\', '/');
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool DeleteCategoryFolder(string baseDir, string relativePath, Dictionary<string, DemoMetadata> metadataDb)
        {
            string targetDir = Path.Combine(baseDir, relativePath);
            string genDir = Path.Combine(baseDir, "General");

            try
            {
                if (Directory.Exists(targetDir))
                {
                    // Move files to General folder to avoid deleting demos
                    foreach (string file in Directory.GetFiles(targetDir, "*.dem", SearchOption.AllDirectories))
                    {
                        string dest = Path.Combine(genDir, Path.GetFileName(file));
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(file, dest);
                        
                        // Update metadata path
                        string relPath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                        string newRel = "General/" + Path.GetFileName(file);
                        DemoMetadata dm;
                        if (metadataDb.TryGetValue(relPath, out dm))
                        {
                            metadataDb.Remove(relPath);
                            DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, newRel, dm);
                        }
                    }
                    Directory.Delete(targetDir, true);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
