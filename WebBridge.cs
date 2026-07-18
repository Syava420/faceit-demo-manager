using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace FaceitDemoManager
{
    public class WebBridge
    {
        private readonly MainWindow _mainWindow;
        private readonly CoreWebView2 _coreWebView2;

        public WebBridge(MainWindow mainWindow, CoreWebView2 coreWebView2)
        {
            _mainWindow = mainWindow;
            _coreWebView2 = coreWebView2;
            _coreWebView2.WebMessageReceived += OnWebMessageReceived;
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string rawJson = e.WebMessageAsJson;
                using (JsonDocument doc = JsonDocument.Parse(rawJson))
                {
                    JsonElement root = doc.RootElement;
                    if (!root.TryGetProperty("action", out JsonElement actionProp)) return;

                    string action = actionProp.GetString();
                    _mainWindow.Dispatcher.Invoke(() => HandleAction(action, root));
                }
            }
            catch (Exception ex)
            {
                SendLog("Ошибка обработки IPC: " + ex.ToString());
            }
        }

        private void HandleAction(string action, JsonElement root)
        {
            switch (action)
            {
                case "initApp":
                    _mainWindow.SendAllStateToWeb();
                    break;

                case "debugLog":
                    if (root.TryGetProperty("msg", out JsonElement msgProp))
                    {
                        SendLog("[JS Web UI] " + msgProp.GetString());
                    }
                    break;

                case "minimizeWindow":
                    _mainWindow.WindowState = WindowState.Minimized;
                    break;

                case "closeWindow":
                    _mainWindow.Close();
                    break;

                case "dragWindow":
                    _mainWindow.DragWindowNative();
                    break;

                case "maximizeWindow":
                    _mainWindow.WindowState = _mainWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    break;

                case "browseDemosManual":
                    _mainWindow.BrowseDemosManual();
                    break;

                case "playDemo":
                    if (root.TryGetProperty("filePath", out JsonElement pathProp))
                    {
                        _mainWindow.PlayDemoByPath(pathProp.GetString());
                    }
                    break;

                case "browseDownloads":
                    _mainWindow.BrowseDownloadsPath();
                    break;

                case "autoDetectCS2":
                    _mainWindow.AutoDetectCS2Path();
                    break;

                case "browseCS2":
                    _mainWindow.BrowseCS2Path();
                    break;

                case "processDownloads":
                    _mainWindow.ProcessDownloadsFolder();
                    break;

                case "createCategory":
                    _mainWindow.CreateNewCategoryPrompt();
                    break;

                case "createSubfolder":
                    if (root.TryGetProperty("parent", out JsonElement parentProp))
                    {
                        _mainWindow.CreateSubfolder(parentProp.GetString());
                    }
                    break;

                case "renameCategory":
                    if (root.TryGetProperty("category", out JsonElement rNameProp))
                    {
                        _mainWindow.RenameCategory(rNameProp.GetString());
                    }
                    break;

                case "deleteCategory":
                    if (root.TryGetProperty("category", out JsonElement delCatProp))
                    {
                        _mainWindow.DeleteCategoryWeb(delCatProp.GetString());
                    }
                    break;

                case "setFolderNickname":
                    if (root.TryGetProperty("category", out JsonElement fnCatProp))
                    {
                        _mainWindow.SetFolderNickname(fnCatProp.GetString());
                    }
                    break;

                case "toggleFolderCollapse":
                    if (root.TryGetProperty("folder", out JsonElement fCollapseProp))
                    {
                        _mainWindow.ToggleFolderCollapse(fCollapseProp.GetString());
                    }
                    break;

                case "moveFolder":
                    if (root.TryGetProperty("src", out JsonElement srcProp) && root.TryGetProperty("dest", out JsonElement destProp))
                    {
                        _mainWindow.MoveFolderWeb(srcProp.GetString(), destProp.GetString());
                    }
                    break;

                case "moveDemo":
                    {
                        if (root.TryGetProperty("filePath", out JsonElement demoPathProp) && root.TryGetProperty("category", out JsonElement targetCatProp))
                        {
                            _mainWindow.MoveDemoWeb(demoPathProp.GetString(), targetCatProp.GetString());
                        }
                    }
                    break;

                case "importFilesInto":
                    if (root.TryGetProperty("category", out JsonElement importCatProp))
                    {
                        _mainWindow.ImportFilesInto(importCatProp.GetString());
                    }
                    break;

                case "selectCategory":
                    if (root.TryGetProperty("category", out JsonElement catProp))
                    {
                        _mainWindow.SelectCategory(catProp.GetString());
                    }
                    break;

                case "importFiles":
                    if (root.TryGetProperty("filePaths", out JsonElement filesProp))
                    {
                        foreach (var f in filesProp.EnumerateArray())
                        {
                            _mainWindow.ImportSingleDroppedFile(f.GetString());
                        }
                    }
                    break;

                case "saveSettings":
                    if (root.TryGetProperty("settings", out JsonElement settingsProp))
                    {
                        _mainWindow.UpdateSettingsFromJson(settingsProp);
                    }
                    break;

                case "saveBinds":
                    if (root.TryGetProperty("binds", out JsonElement bindsProp))
                    {
                        _mainWindow.UpdateBindsFromJson(bindsProp);
                    }
                    break;

                case "resetBindsToDefault":
                    _mainWindow.ResetBindsToDefault();
                    break;

                case "saveDemoMetadata":
                    {
                        if (root.TryGetProperty("filePath", out JsonElement metadataPathProp))
                        {
                            string fPath = metadataPathProp.GetString();
                            string map = root.TryGetProperty("map", out JsonElement mapP) ? mapP.GetString() : "";
                            string score = root.TryGetProperty("score", out JsonElement scoreP) ? scoreP.GetString() : "";
                            string kd = root.TryGetProperty("kd", out JsonElement kdP) ? kdP.GetString() : "";
                            string date = root.TryGetProperty("date", out JsonElement dateP) ? dateP.GetString() : "";
                            string note = root.TryGetProperty("note", out JsonElement noteP) ? noteP.GetString() : "";
                            _mainWindow.SaveDemoMetadataWeb(fPath, map, score, kd, date, note);
                        }
                    }
                    break;
                case "copyDemoConfig":
                    {
                        if (root.TryGetProperty("filePath", out JsonElement configPathProp))
                        {
                            _mainWindow.CopyDemoConfigToClipboard(configPathProp.GetString());
                        }
                    }
                    break;
                case "deleteSelectedDemos":
                    {
                        if (root.TryGetProperty("filePaths", out JsonElement pathsProp) && pathsProp.ValueKind == JsonValueKind.Array)
                        {
                            var list = new System.Collections.Generic.List<string>();
                            foreach (var p in pathsProp.EnumerateArray())
                            {
                                list.Add(p.GetString());
                            }
                            _mainWindow.DeleteDemosWeb(list);
                        }
                    }
                    break;
                case "moveSelectedDemos":
                    {
                        if (root.TryGetProperty("filePaths", out JsonElement pathsProp) && pathsProp.ValueKind == JsonValueKind.Array)
                        {
                            var list = new System.Collections.Generic.List<string>();
                            foreach (var p in pathsProp.EnumerateArray())
                            {
                                list.Add(p.GetString());
                            }
                            _mainWindow.MoveSelectedDemosWebPrompt(list);
                        }
                    }
                    break;
                case "moveDemos":
                    {
                        if (root.TryGetProperty("filePaths", out JsonElement pathsProp) && pathsProp.ValueKind == JsonValueKind.Array)
                        {
                            var list = new System.Collections.Generic.List<string>();
                            foreach (var p in pathsProp.EnumerateArray())
                            {
                                list.Add(p.GetString());
                            }
                            string cat = root.TryGetProperty("category", out JsonElement catP) ? catP.GetString() : "General";
                            _mainWindow.MoveDemosWeb(list, cat);
                        }
                        else if (root.TryGetProperty("filePath", out JsonElement singlePathProp) && root.TryGetProperty("category", out JsonElement singleCatProp))
                        {
                            _mainWindow.MoveDemoWeb(singlePathProp.GetString(), singleCatProp.GetString());
                        }
                    }
                    break;
                case "reorderDemos":
                    {
                        if (root.TryGetProperty("filePaths", out JsonElement pathsProp) && pathsProp.ValueKind == JsonValueKind.Array)
                        {
                            var list = new System.Collections.Generic.List<string>();
                            foreach (var p in pathsProp.EnumerateArray())
                            {
                                list.Add(p.GetString());
                            }
                            _mainWindow.ReorderDemosWeb(list);
                        }
                    }
                    break;
            }
        }

        public void SendToWeb(object data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data);
                _coreWebView2.PostWebMessageAsJson(json);
            }
            catch { }
        }

        public void SendLog(string message)
        {
            SendToWeb(new { type = "appendLog", text = message });
        }

        public void SendStatus(string status, double progress = 0)
        {
            SendToWeb(new { type = "updateStatus", status = status, progress = progress });
        }
    }
}
