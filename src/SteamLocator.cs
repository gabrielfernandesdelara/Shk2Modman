using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Shrek2ModManager
{
    /// <summary>Acha o Shrek.exe olhando as bibliotecas da Steam.</summary>
    public static class SteamLocator
    {
        public const string AppId = "2523740";
        const string RelativeExe = @"steamapps\common\Shrek2Remastered\Shrek.exe";

        public static string FindGameExe()
        {
            foreach (string library in FindLibraries())
            {
                string exe = Path.Combine(library, RelativeExe);
                if (File.Exists(exe)) return exe;
            }
            return null;
        }

        static List<string> FindLibraries()
        {
            List<string> libraries = new List<string>();
            foreach (string steam in SteamInstallPaths())
            {
                AddUnique(libraries, steam);
                // libraryfolders.vdf lista as outras pastas de biblioteca (outros discos).
                string vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;
                try
                {
                    foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                        AddUnique(libraries, m.Groups[1].Value.Replace(@"\\", @"\"));
                }
                catch (IOException) { }
            }
            return libraries;
        }

        static IEnumerable<string> SteamInstallPaths()
        {
            string[][] keys = new string[][]
            {
                new string[] { @"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath" },
                new string[] { @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath" },
                new string[] { @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath" }
            };
            foreach (string[] key in keys)
            {
                string path = null;
                try { path = Registry.GetValue(key[0], key[1], null) as string; }
                catch (Exception) { }
                if (!string.IsNullOrEmpty(path)) yield return path.Replace('/', '\\');
            }
        }

        static void AddUnique(List<string> list, string path)
        {
            foreach (string existing in list)
                if (string.Equals(existing.TrimEnd('\\'), path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) return;
            list.Add(path);
        }
    }
}
