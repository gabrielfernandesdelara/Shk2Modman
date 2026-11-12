using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace Shk2Modman
{
    /// <summary>O que o programa lembra entre uma abertura e outra (%AppData%\Shk2Modman\settings.json).</summary>
    public class Settings
    {
        public string GameExe = "";
        /// <summary>"pt" ou "en"; vazio = seguir o idioma do Windows.</summary>
        public string Language = "";
        /// <summary>Mods que o usuario deixou instalados. Serve para avisar quando a Steam desfaz os patches.</summary>
        public List<string> Enabled = new List<string>();

        static string FilePath
        {
            get
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shk2Modman");
                return Path.Combine(folder, "settings.json");
            }
        }

        public static Settings Load()
        {
            Settings s = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                Dictionary<string, object> root = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath)) as Dictionary<string, object>;
                if (root == null) return s;
                object value;
                if (root.TryGetValue("gameExe", out value) && value != null) s.GameExe = Convert.ToString(value);
                if (root.TryGetValue("language", out value) && value != null) s.Language = Convert.ToString(value);
                if (root.TryGetValue("enabled", out value) && value is object[])
                    foreach (object id in (object[])value) s.Enabled.Add(Convert.ToString(id));
            }
            catch (Exception) { /* configuracao corrompida: comeca do zero */ }
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["gameExe"] = GameExe;
                root["language"] = Language;
                root["enabled"] = Enabled;
                File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(root));
            }
            catch (Exception) { /* nao conseguir salvar a preferencia nao deve travar o programa */ }
        }
    }
}
