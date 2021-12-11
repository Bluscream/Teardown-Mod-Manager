using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeardownModManager;

namespace Teardown.Classes
{
    public class ModInfoFile : INIFile
    {
        // name = YLVF (YuLun's Vehicle Frame) 0.6.4
        // author = YuLun
        // description = [noupload]A mod which supports extended vehicle features
        // tags = Gameplay, Global
        public FileInfo File;
        public string Name = "<UNKOWN>";
        public string Author = "<UNKOWN>";
        public string Description = "<UNKOWN>";
        public HashSet<string> Tags = new HashSet<string>();

        internal ModInfoFile(FileInfo file, bool readFile = true) : base(file, readFile)
        {
            this.File = file;
            if (readFile) Read();
        }
        public ModInfoFile Read()
        {
            base.Read();
            foreach (var thing in _IniData.Global)
            {
                var _name = thing.KeyName.ToLowerInvariant();
                switch (_name)
                {
                    case "name":
                        Name = thing.Value;
                        break;
                    case "author":
                        Author = thing.Value;
                        break;
                    case "description":
                        Description = thing.Value;
                        break;
                    case "tags":
                        Tags = new HashSet<string>(thing.Value.Split(',').Select(x => x.Trim()));
                        break;
                }
            }
            Utils.Logger.Debug($"ModInfoFile read: {this.ToJson(true)}");
            return this;
        }
    }
}
