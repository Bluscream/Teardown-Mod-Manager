using Steam.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using Teardown.Classes;
using TeardownModManager;

namespace Teardown
{
    public enum Architecture
    {
        [Description("x64")]
        WIN_64,

        [Description("x86")]
        WIN_32
    }

    public class Binary
    {
        public string FileName = "";
        public List<Process> Processes { get; set; } = new List<Process>();

        public FileInfo File(Game game, Architecture arch) => game.BasePath.CombineFile(FileName);

        public bool Running()
        {
            var pName = Path.GetFileNameWithoutExtension(FileName);
            Processes = Process.GetProcessesByName(pName).ToList();
            if (Processes.Count == 0) return false;
            return true;
        }
    }

    public class Binaries
    {
        public Binary Main = new Binary() { FileName = Game.AppFileName };
    }

    public class Game
    {
        public const string Name = "Teardown";
        public const int SteamAppId = 1167630;
        public const string AppFileName = "teardown.exe";

        public List<Process> Processes => Binaries.Main.Processes;
        public Binaries Binaries { get; set; } = new Binaries();

        public DirectoryInfo BasePath { get; set; }
        public DirectoryInfo LocalAppdataPath = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).Combine("Teardown");
        public DirectoryInfo DocumentsPath = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).Combine("Teardown");
        public DirectoryInfo WorkshopPath;

        public Dictionary<ModType, DirectoryInfo> ModDirs { get; set; }

        public ModsFile ModsXML;

        public List<Mod> Mods { get; set; } = new List<Mod>();

        public delegate void DetailsLoadedEventHandler(object sender);

        public event DetailsLoadedEventHandler OnDetailsLoaded;

        public Game(DirectoryInfo basePath)
        {
            BasePath = basePath;
            WorkshopPath = BasePath.Parent.Parent.Combine("workshop", "content", SteamAppId.ToString());
            ModsXML = new ModsFile(LocalAppdataPath.CombineFile("mods.xml"));
            // Utils.Logger.Info(ModsXML.ToJson(true));
            ModDirs = new Dictionary<ModType, DirectoryInfo> {
                { ModType.Local, LocalAppdataPath.Combine("mods") },
                { ModType.Steam, WorkshopPath },
                { ModType.BuiltIn, BasePath.Combine("mods") }
            };
            // Utils.Logger.Info(ModDirs.ToJson(true));
            foreach (var modDir in ModDirs)

                Mods.AddRange(LoadMods(modDir.Value, modDir.Key));


            Utils.Logger.Info(Mods.ToJson(true));
        }

        public bool Running() => (Binaries.Main.Running());

        public List<Mod> LoadMods(DirectoryInfo modsDir, ModType type = ModType.Unknown)
        {
            var mods = new List<Mod>();

            if (!modsDir.Exists)
            {
                Utils.Logger.Warn($"Mods directory {modsDir.FullName} does not exist");
                return mods;
            }

            foreach (var modDir in Directory.GetDirectories(modsDir.FullName))
            {
                var mod = new Mod(this, new DirectoryInfo(modDir), type);
                if (mod.SteamWorkshopId != "386670448") mods.Add(mod);
            }

            return mods;
        }

        public async System.Threading.Tasks.Task<GetPublishedFileDetailsResponse> UpdateModDetailsAsync(HttpClient webClient)
        {
            /*SteamRequest request = new SteamRequest("ISteamRemoteStorage/GetPublishedFileDetails/v1/");
            request.AddParameter("itemcount", Mods.Count);*/
            var fileIds = Mods.Select(t => t.SteamWorkshopId).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (fileIds.Count < 1) return null;
            /*request.AddParameter("publishedfileids", fileIds);
			var response = steam.Execute(request);
            Console.WriteLine(response.Content);
            */
            var parsedResponse = await Steam.Utils.GetPublishedFileDetailsAsync(webClient, fileIds);

            foreach (var details in parsedResponse.response.publishedfiledetails)
            {
                Mods.Where(t => t.SteamWorkshopId == details.publishedfileid).First().Details = details;
            }

            try
            {
                OnDetailsLoaded?.Invoke(this);
            }
            catch (Exception ex) { Console.WriteLine("[ERROR] UpdateModDetailsAsync: {0}", ex.Message); }

            return parsedResponse;
        }
    }
}