using System.IO;
using IniParser;
using IniParser.Model;
using TeardownModManager;

namespace Teardown.Classes
{
    public class INIFile
    {
        private static FileIniDataParser parser = new FileIniDataParser();

        internal FileInfo file;
        internal IniData _IniData;
        public INIFile(FileInfo file, bool readFile = true)
        {
            this.file = file;
            if (readFile) this.Read();
        }
        public INIFile Read()
        {
            parser.Parser.Configuration.CommentChar = '#';
            parser.Parser.Configuration.AllowDuplicateKeys = true;
            parser.Parser.Configuration.SkipInvalidLines = true;
            Utils.Logger.Info(file.FullName);
            _IniData = parser.ReadFile(file.FullName);
            return this;
        }
        public void Write()
        {
            parser.WriteFile(file.FullName, _IniData);
        }
    }
}