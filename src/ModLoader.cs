using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace Shrek2ModManager
{
    /// <summary>Le os arquivos .json da pasta "mods" e transforma em ModDef.</summary>
    public static class ModLoader
    {
        /// <summary>Carrega todos os mods da pasta. Arquivos com erro vao para "errors" e sao ignorados.</summary>
        public static List<ModDef> LoadFolder(string folder, List<string> errors)
        {
            List<ModDef> mods = new List<ModDef>();
            if (!Directory.Exists(folder)) return mods;
            string[] files = Directory.GetFiles(folder, "*.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                try
                {
                    ModDef mod = Parse(File.ReadAllText(file), Path.GetFileName(file));
                    bool duplicated = false;
                    foreach (ModDef other in mods) if (other.Id == mod.Id) duplicated = true;
                    if (duplicated) errors.Add(Path.GetFileName(file) + ": id repetido (" + mod.Id + ")");
                    else mods.Add(mod);
                }
                catch (Exception ex)
                {
                    errors.Add(Path.GetFileName(file) + ": " + ex.Message);
                }
            }
            return mods;
        }

        public static ModDef Parse(string json, string fileName)
        {
            Dictionary<string, object> root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (root == null) throw new FormatException("o arquivo nao e um objeto JSON");

            ModDef mod = new ModDef();
            mod.File = fileName;
            mod.Id = GetString(root, "id");
            if (string.IsNullOrEmpty(mod.Id)) mod.Id = Path.GetFileNameWithoutExtension(fileName);
            mod.Version = GetString(root, "version");
            mod.GameBuild = GetString(root, "gameBuild");
            mod.Name = GetTexts(root, "name");
            mod.Description = GetTexts(root, "description");

            object[] patches = root.ContainsKey("patches") ? root["patches"] as object[] : null;
            if (patches == null || patches.Length == 0) throw new FormatException("sem a lista \"patches\"");
            foreach (object item in patches)
            {
                Dictionary<string, object> p = item as Dictionary<string, object>;
                if (p == null) throw new FormatException("patch invalido");
                Patch patch = new Patch();
                patch.Rva = Convert.ToInt32(GetString(p, "rva"), 16);
                patch.Original = PatchEngine.HexToBytes(GetString(p, "original"));
                patch.Patched = PatchEngine.HexToBytes(GetString(p, "patched"));
                patch.Note = GetString(p, "note");
                if (patch.Original.Length != patch.Patched.Length)
                    throw new FormatException("patch em " + GetString(p, "rva") + ": \"original\" e \"patched\" tem tamanhos diferentes");
                mod.Patches.Add(patch);
            }
            return mod;
        }

        static string GetString(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return "";
            return Convert.ToString(value);
        }

        // Aceita tanto "name": "Texto" quanto "name": { "en": "...", "pt": "..." }.
        static Dictionary<string, string> GetTexts(Dictionary<string, object> obj, string key)
        {
            Dictionary<string, string> texts = new Dictionary<string, string>();
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return texts;
            Dictionary<string, object> byLang = value as Dictionary<string, object>;
            if (byLang == null) { texts["en"] = Convert.ToString(value); return texts; }
            foreach (KeyValuePair<string, object> kv in byLang) texts[kv.Key] = Convert.ToString(kv.Value);
            return texts;
        }
    }
}
