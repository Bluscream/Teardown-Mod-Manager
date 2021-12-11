using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TeardownModManager;

namespace Teardown.Classes
{
    public class ModsFile : XMLFile
    {
        public Version version;
        public HashSet<ModsFileEntry> entries;
        internal ModsFile(FileInfo file, bool readFile = true) : base(file, readFile)
        {
            Utils.Logger.Debug("internal ModsFile(FileInfo file, bool readFile = true) : base(file, readFile)");
            if (readFile) Read();
        }
        public ModsFile Read()
        {
            Utils.Logger.Debug("internal ModsFile Read()");
            base.Read();
            var root = _XmlDocument.DocumentElement;
            if (root.Name != "mods")
            {
                throw new Exception("Invalid mods file! (Could not find root node named \"mods\")");
            }
            version = Version.Parse(root.Attributes["version"].Value);
            entries = new HashSet<ModsFileEntry>();
            foreach (XmlNode node in root.ChildNodes)
            {
                var entry = node.Parse();
                if (entry != null) entries.Add(entry);
            }
            Utils.Logger.Info($"Loaded {file.FullName.Quote()} (v{version}) with {entries.Count} mods.");
            return this;
        }

        public ModsFileEntry getMod(string id, ModType type = ModType.Unknown)
        {
            foreach (var entry in entries)
            {
                if (entry.Id == id && entry.Type == type)
                {
                    return entry;
                }
            }
            return null;
        }
    }
    public enum ModType
    {
        Unknown,
        BuiltIn,
        Steam,
        Local
    }
    public class ModsFileEntry
    {
        public XmlNode Node;
        public string Fullname = "<UNKOWN>";
        public string Id = "<UNKNOWN>";
        public bool Active = false;
        public DateTimeOffset? LastEnabled;
        public DateTimeOffset? LastSubscribed;
        public ModType Type = ModType.Unknown;

    }
    public static class ModsFileEntryExtensions
    {
        public static ModsFileEntry Parse(this XmlNode node)
        {
            if (node.Name != "mod") return null;
            var entry = new ModsFileEntry();
            var _name = node.Attributes["id"];
            if (_name != null)
            {
                entry.Fullname = _name.Value;
                if (entry.Fullname.Contains("-"))
                {
                    var name = entry.Fullname.ToLowerInvariant();
                    if (name.StartsWith("steam-"))
                    {
                        entry.Type = ModType.Steam;
                        entry.Id = entry.Fullname.ReplaceFirst("steam-", "");
                    }
                    else if (name.StartsWith("local-"))
                    {
                        entry.Type = ModType.Local;
                        entry.Id = entry.Fullname.ReplaceFirst("local-", "");
                    }
                    else if (name.StartsWith("builtin-"))
                    {
                        entry.Type = ModType.BuiltIn;
                        entry.Id = entry.Fullname.ReplaceFirst("builtin-", "");
                    }
                    else
                    {
                        entry.Id = entry.Fullname;
                    }
                    // entry.id = entry.fullname.Split(new string[] { " - " }, 2, StringSplitOptions.None)[1];
                }
            }
            if (node.Attributes["active"] != null) entry.Node = node;
            if (node.Attributes["seltime"] != null)
            {
                var val = node.Attributes["seltime"].Value;
                try
                {
                    entry.LastEnabled = DateTimeOffset.FromUnixTimeSeconds(long.Parse(val));
                }
                catch (FormatException ex) { Utils.Logger.Error($"Could not parse \"seltime={val}\" of mod {entry.Fullname}: {ex.Message}"); }
            }
            if (node.Attributes["subtime"] != null)
            {
                var val = node.Attributes["subtime"].Value;
                try
                {
                    entry.LastSubscribed = DateTimeOffset.FromUnixTimeSeconds(long.Parse(val));
                }
                catch (FormatException ex) { Utils.Logger.Error($"Could not parse \"subtime={val}\" of mod {entry.Fullname}: {ex.Message}"); }
            }
            return entry;
        }
    }
}
