using System;
using System.Collections.Generic;
using System.IO;

namespace Shk2Modman
{
    /// <summary>Um trecho do Shrek.exe: onde fica, os bytes originais e os bytes do mod.</summary>
    public class Patch
    {
        public int Rva;
        public byte[] Original;
        public byte[] Patched;
        public string Note;
    }

    /// <summary>Um mod = um arquivo .json da pasta "mods".</summary>
    public class ModDef
    {
        public string Id;
        public string File;
        public string Version = "";
        public string GameBuild = "";
        public Dictionary<string, string> Name = new Dictionary<string, string>();
        public Dictionary<string, string> Description = new Dictionary<string, string>();
        public List<Patch> Patches = new List<Patch>();

        public string GetName(string lang) { return Pick(Name, lang, Id); }
        public string GetDescription(string lang) { return Pick(Description, lang, ""); }

        static string Pick(Dictionary<string, string> texts, string lang, string fallback)
        {
            string s;
            if (texts.TryGetValue(lang, out s)) return s;
            if (texts.TryGetValue("en", out s)) return s;
            foreach (string v in texts.Values) return v;
            return fallback;
        }
    }

    public enum ModState
    {
        /// <summary>Todos os trechos estao com os bytes originais do jogo.</summary>
        NotInstalled,
        /// <summary>Todos os trechos estao com os bytes do mod.</summary>
        Installed,
        /// <summary>Os bytes nao batem com nenhum dos dois: outra versao do jogo ou exe alterado.</summary>
        Incompatible
    }

    /// <summary>Le e altera os bytes do Shrek.exe. Nao tem nada de tela aqui.</summary>
    public static class PatchEngine
    {
        public static byte[] HexToBytes(string hex)
        {
            if (hex == null) throw new FormatException("hex vazio");
            hex = hex.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");
            if (hex.Length == 0 || hex.Length % 2 != 0) throw new FormatException("hex com tamanho invalido");
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return result;
        }

        /// <summary>
        /// Converte um RVA (endereco do codigo na memoria) na posicao dentro do arquivo,
        /// usando a tabela de secoes do proprio exe. Retorna -1 se o RVA nao existe.
        /// </summary>
        public static int RvaToOffset(byte[] exe, int rva)
        {
            if (exe.Length < 0x40) return -1;
            int pe = BitConverter.ToInt32(exe, 0x3C);
            if (pe <= 0 || pe + 24 > exe.Length) return -1;
            int sectionCount = BitConverter.ToUInt16(exe, pe + 6);
            int optionalSize = BitConverter.ToUInt16(exe, pe + 20);
            int table = pe + 24 + optionalSize;
            for (int i = 0; i < sectionCount; i++)
            {
                int s = table + i * 40;
                if (s + 40 > exe.Length) return -1;
                int virtualSize = BitConverter.ToInt32(exe, s + 8);
                int virtualAddress = BitConverter.ToInt32(exe, s + 12);
                int rawSize = BitConverter.ToInt32(exe, s + 16);
                int rawPointer = BitConverter.ToInt32(exe, s + 20);
                if (rva >= virtualAddress && rva < virtualAddress + Math.Max(virtualSize, rawSize))
                    return rva - virtualAddress + rawPointer;
            }
            return -1;
        }

        static bool Matches(byte[] exe, int offset, byte[] expected)
        {
            if (offset < 0 || offset + expected.Length > exe.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (exe[offset + i] != expected[i]) return false;
            return true;
        }

        public static ModState GetState(byte[] exe, ModDef mod)
        {
            int original = 0, patched = 0;
            foreach (Patch p in mod.Patches)
            {
                int offset = RvaToOffset(exe, p.Rva);
                if (Matches(exe, offset, p.Original)) original++;
                if (Matches(exe, offset, p.Patched)) patched++;
            }
            if (mod.Patches.Count > 0 && original == mod.Patches.Count) return ModState.NotInstalled;
            if (mod.Patches.Count > 0 && patched == mod.Patches.Count) return ModState.Installed;
            return ModState.Incompatible;
        }

        /// <summary>Grava os bytes do mod (install = true) ou os originais (install = false).</summary>
        public static void Write(byte[] exe, ModDef mod, bool install)
        {
            foreach (Patch p in mod.Patches)
            {
                byte[] bytes = install ? p.Patched : p.Original;
                Buffer.BlockCopy(bytes, 0, exe, RvaToOffset(exe, p.Rva), bytes.Length);
            }
        }

        /// <summary>Dois mods conflitam quando mexem no mesmo trecho do exe.</summary>
        public static bool Overlaps(ModDef a, ModDef b)
        {
            foreach (Patch x in a.Patches)
                foreach (Patch y in b.Patches)
                    if (x.Rva < y.Rva + y.Original.Length && y.Rva < x.Rva + x.Original.Length)
                        return true;
            return false;
        }
    }

    public class ApplyResult
    {
        public int Installed;
        public int Removed;
        public bool BackupCreated;
    }

    /// <summary>Erro esperado (jogo aberto, versao diferente...), com a chave do texto a mostrar.</summary>
    public class ModManagerException : Exception
    {
        public string Key;
        public string Detail;
        public ModManagerException(string key, string detail) : base(key + ": " + detail) { Key = key; Detail = detail; }
    }

    /// <summary>Operacoes no arquivo Shrek.exe: ler o estado e aplicar a selecao de mods.</summary>
    public static class GameExe
    {
        public const string BackupSuffix = ".original";

        public static bool HasBackup(string exePath)
        {
            return exePath != null && File.Exists(exePath + BackupSuffix);
        }

        /// <summary>Copia o Shrek.exe.original por cima do Shrek.exe, desfazendo qualquer alteracao.</summary>
        public static void RestoreBackup(string exePath)
        {
            try { File.Copy(exePath + BackupSuffix, exePath, true); }
            catch (IOException ex) { throw new ModManagerException("errWrite", ex.Message); }
            catch (UnauthorizedAccessException ex) { throw new ModManagerException("errWrite", ex.Message); }
        }

        public static Dictionary<string, ModState> ReadStates(string exePath, IList<ModDef> mods)
        {
            byte[] exe = File.ReadAllBytes(exePath);
            Dictionary<string, ModState> states = new Dictionary<string, ModState>();
            foreach (ModDef m in mods) states[m.Id] = PatchEngine.GetState(exe, m);
            return states;
        }

        /// <summary>
        /// Deixa instalados exatamente os mods de "wanted" (os outros conhecidos sao removidos).
        /// Nada e gravado se algum mod que precisa mudar for incompativel.
        /// </summary>
        public static ApplyResult ApplySelection(string exePath, IList<ModDef> mods, ICollection<string> wanted)
        {
            byte[] exe = File.ReadAllBytes(exePath);
            ApplyResult result = new ApplyResult();
            List<ModDef> toInstall = new List<ModDef>();
            List<ModDef> toRemove = new List<ModDef>();
            bool anyInstalled = false;

            foreach (ModDef m in mods)
            {
                ModState state = PatchEngine.GetState(exe, m);
                bool want = wanted.Contains(m.Id);
                if (state == ModState.Installed) anyInstalled = true;
                if (want && state == ModState.NotInstalled) toInstall.Add(m);
                else if (!want && state == ModState.Installed) toRemove.Add(m);
                else if (want && state == ModState.Incompatible) throw new ModManagerException("errIncompatible", m.Id);
            }

            for (int i = 0; i < toInstall.Count; i++)
            {
                for (int j = i + 1; j < toInstall.Count; j++)
                    if (PatchEngine.Overlaps(toInstall[i], toInstall[j]))
                        throw new ModManagerException("errConflict", toInstall[i].Id + " + " + toInstall[j].Id);
                foreach (ModDef other in mods)
                    if (other != toInstall[i] && wanted.Contains(other.Id) && !toInstall.Contains(other)
                        && PatchEngine.Overlaps(toInstall[i], other))
                        throw new ModManagerException("errConflict", toInstall[i].Id + " + " + other.Id);
            }

            if (toInstall.Count == 0 && toRemove.Count == 0) return result;

            // O backup so e criado a partir de um exe sem nenhum mod conhecido aplicado.
            string backup = exePath + BackupSuffix;
            if (!anyInstalled && !File.Exists(backup))
            {
                File.Copy(exePath, backup);
                result.BackupCreated = true;
            }

            // Remove primeiro e instala depois, sempre na copia em memoria.
            foreach (ModDef m in toRemove) { PatchEngine.Write(exe, m, false); result.Removed++; }
            foreach (ModDef m in toInstall) { PatchEngine.Write(exe, m, true); result.Installed++; }

            try { File.WriteAllBytes(exePath, exe); }
            catch (IOException ex) { throw new ModManagerException("errWrite", ex.Message); }
            catch (UnauthorizedAccessException ex) { throw new ModManagerException("errWrite", ex.Message); }
            return result;
        }
    }
}
