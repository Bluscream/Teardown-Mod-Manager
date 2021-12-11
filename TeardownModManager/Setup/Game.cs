using System;
using System.IO;
using System.Windows.Forms;
using VRCModManager.Dependencies;

namespace TeardownModManager.Setup
{
    public class PathLogic
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public DirectoryInfo GetInstallationPath()
        {
            var steam = GetSteamLocation();

            if (steam != null)
            {
                if (steam.Exists)
                {
                    if (steam.CombineFile(Teardown.Game.AppFileName).Exists)
                    {
                        return steam;
                    }
                }
            }

            Logger.Warn("Could not find {Teardown.Game.Name} path through \"steam path\".");

            var local = Utils.getOwnPath().Directory;

            if (local.CombineFile(Teardown.Game.AppFileName).Exists)
            {
                return local;
            }

            Logger.Warn($"Could not find {Teardown.Game.Name} at {local.FullName.Quote()}.");

            local = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            if (local.CombineFile(Teardown.Game.AppFileName).Exists)
            {
                return local;
            }

            Logger.Warn($"Could not find {Teardown.Game.Name} at {local.FullName.Quote()}.");

            // iterate over all drives
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed)
                {
                    var path = new DirectoryInfo(drive.Name).Combine("steam", "steamapps", "common", "Teardown");

                    if (path.CombineFile(Teardown.Game.AppFileName).Exists)
                    {
                        return path;
                    }

                    Logger.Warn($"Could not find {Teardown.Game.Name} at {path.FullName.Quote()}.");
                }
            }

            Logger.Warn($"Could not find {Teardown.Game.Name} path through \"all drives\".");

            var fallback = GetFallbackDirectory();
            return fallback;
        }

        private DirectoryInfo GetFallbackDirectory()
        {
            var folder = false;
            MessageBoxManager.Yes = "Browse exe";
            MessageBoxManager.No = "Browse dir";
            MessageBoxManager.Register();
            var result = MessageBox.Show($"We couldn't seem to find your {Teardown.Game.Name} installation, please show us where it is located.", "Error", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error);
            MessageBoxManager.Unregister();

            if (result == DialogResult.Cancel) Utils.Exit();
            else if (result == DialogResult.No) folder = true;

            return NotFoundHandler(folder);
        }

        private DirectoryInfo GetSteamLocation()
        {
            try
            {
                var steamFinder = new SteamFinder();
                if (!steamFinder.FindSteam()) return null;
                return new DirectoryInfo(steamFinder.FindGameFolder(Teardown.Game.SteamAppId));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /* Logger.Warn("Game not found, setup required!");
            var confirmResult = MessageBox.Show("VRChat.exe was not found in the same directory as this launcher.\n\nHowever it needs to be in the same folder to work properly, please select your game to move this launcher next to it and restart.", "Game not found!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.Cancel) Utils.Exit();
            var gameSelector = new OpenFileDialog();
            gameSelector.Title = "Select the VRChat.exe file";
            gameSelector.InitialDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\VRChat";
            gameSelector.Filter = "VRChat Executable|VRChat.exe|All Executables|*.exe";
            if (gameSelector.ShowDialog() == DialogResult.OK)
            {
                var Game = new FileInfo(gameSelector.FileName);
                var Launcher = Utils.Utils.getOwnPath();
                var newPath = new FileInfo(Path.Combine(Game.DirectoryName, Launcher.Name));
                Launcher.CopyTo(newPath.FullName);
                Utils.Utils.StartProcess(newPath, "--vrclauncher.keep", Launcher.FullName.Quote());
            }
            Utils.Utils.Exit();
        }
        */

        private DirectoryInfo NotFoundHandler(bool folder)
        {
            var found = string.Empty;

            while (found == string.Empty)
                if (folder) found = FindFolder();
                else found = FindFile();

            return found == string.Empty ? null : new DirectoryInfo(found);
        }

        public string FindFile()
        {
            using (var fileDialog = new OpenFileDialog())
            {
                fileDialog.Title = $"Select the {Teardown.Game.AppFileName} file";
                fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                fileDialog.Filter = $"{Teardown.Game.Name} Executables|{Teardown.Game.AppFileName}.exe|All Executables|*.exe|All Files|*";
                fileDialog.Multiselect = false;
                var result = fileDialog.ShowDialog();

                if (result == DialogResult.Cancel) Utils.Exit();
                else if (result == DialogResult.OK)
                {
                    var path = new FileInfo(fileDialog.FileName);

                    if (File.Exists(Path.Combine(path.DirectoryName, Teardown.Game.AppFileName)))
                    {
                        return path.DirectoryName;
                    }
                    else
                    {
                        MessageBox.Show($"The directory you selected doesn't contain {Teardown.Game.AppFileName}! please try again!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                return string.Empty;
            }
        }

        public string FindFolder()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = $"Select the folder in which {Teardown.Game.AppFileName} is located.";
                folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                folderDialog.ShowNewFolderButton = false;
                var result = folderDialog.ShowDialog();

                if (result == DialogResult.Cancel) Utils.Exit();
                else if (result == DialogResult.OK)
                {
                    var path = folderDialog.SelectedPath;

                    if (File.Exists(Path.Combine(path, Teardown.Game.AppFileName)))
                    {
                        return folderDialog.SelectedPath;
                    }
                    else
                    {
                        MessageBox.Show($"The directory you selected doesn't contain {Teardown.Game.AppFileName}! please try again!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            return string.Empty;
        }
    }
}