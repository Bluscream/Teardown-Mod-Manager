using System.IO;
using System.Xml;

namespace Teardown.Classes
{
    public class XMLFile
    {
        internal FileInfo file;
        internal XmlDocument _XmlDocument;

        public XMLFile(FileInfo file, bool readFile = true)
        {
            TeardownModManager.Utils.Logger.Debug("public XMLFile(FileInfo file, bool readFile = true)");
            this.file = file;
            _XmlDocument = new XmlDocument();
            if (readFile) Read();
        }

        public XMLFile Read()
        {
            TeardownModManager.Utils.Logger.Debug("public XMLFile Read(FileInfo file)");
            _XmlDocument.Load(file.FullName);
            return this;
        }

        public void Write()
        {
            TeardownModManager.Utils.Logger.Debug("public void Write(FileInfo file)");
            _XmlDocument.Save(file.FullName);
        }
    }
}