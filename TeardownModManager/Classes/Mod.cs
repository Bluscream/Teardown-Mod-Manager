using Newtonsoft.Json;
using Steam.Classes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Teardown.Classes;
using TeardownModManager;

namespace Teardown
{
    /// <summary>
    ///
    /// </summary>
    public class Mod
    {
        [JsonIgnore]
        private Game Game { get; set; }

        public DirectoryInfo Directory { get; set; }
        public ModsFileEntry ModsFileEntry { get; set; }
        public ModInfoFile ModInfoFile { get; set; }

        public string Name
        {
            get
            {
                if (ModInfoFile != null) return ModInfoFile.Name;
                if (Details != null) return Details.title;
                return "<UNKNOWN>";
            }
        }

        public string Author
        {
            get
            {
                if (ModInfoFile != null) return ModInfoFile.Author;
                if (Details != null) return Details.creator;
                return "<UNKNOWN>";
            }
        }

        public string Description
        {
            get
            {
                if (ModInfoFile != null) return ModInfoFile.Description;
                if (Details != null) return Details.description;
                return "<UNKNOWN>";
            }
        }

        public string SteamWorkshopId
        {
            get
            {
                if (Type != ModType.Steam) return null;
                if (Details != null) return Details.publishedfileid;
                if (ModsFileEntry != null) return ModsFileEntry.Id;
                return null;
            }
        }

        public ModType Type { get; set; }
        public bool Disabled => ModsFileEntry?.Active ?? true;
        public Publishedfiledetail Details { get; set; }

        public HashSet<string> Tags = new HashSet<string>();

        /// <summary>
        ///
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public Mod(Game game, DirectoryInfo directory, ModType type = ModType.Unknown)
        {
            Directory = directory;
            Game = game;
            var infoFile = directory.CombineFile("info.txt");

            if (infoFile.Exists)
            {
                ModInfoFile = new ModInfoFile(infoFile);
            }

            Type = type;

            if (Type == ModType.Unknown)
            {
                if (directory.Parent == game.ModDirs[ModType.Steam]) Type = ModType.Steam;
                else if (directory.Parent == game.ModDirs[ModType.Local]) Type = ModType.Local;
                else if (directory.Parent == game.ModDirs[ModType.BuiltIn]) Type = ModType.BuiltIn;
            }

            ModsFileEntry = game.ModsXML.getMod(Directory.Name, Type);
            // generate tags from infofile and steam
            Tags = new HashSet<string>();

            if (ModInfoFile != null)
            {
                foreach (var tag in ModInfoFile.Tags)
                    Tags.Add(tag);
            }

            if (Details != null && Details.tags is null)
            {
                foreach (var tag in Details.tags)
                    Tags.Add(tag.tag);
            }

            Utils.Logger.Debug($"New Mod: {this.ToJson(true)}");
        }

        public async Task<Publishedfiledetail> UpdateModDetailsAsync(HttpClient webClient)
        {
            if (string.IsNullOrWhiteSpace(SteamWorkshopId)) return null;
            var parsedResponse = await Steam.Utils.GetPublishedFileDetailsAsync(webClient, SteamWorkshopId);
            // Console.WriteLine(parsedResponse.ToJson());
            Details = parsedResponse.response.publishedfiledetails.First();
            return Details;
        }

        public override string ToString() => Name;
    }
}