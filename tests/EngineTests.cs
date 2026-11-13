using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Shk2Modman;

// Teste do motor de patches, sem tela. Uso:
//   EngineTests.exe <pasta mods> <copia do Shrek.exe ORIGINAL>
// Trabalha so na copia passada; nunca toca no jogo de verdade.
static class EngineTests
{
    static int failures;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "ok    " : "FALHA ") + what);
        if (!ok) failures++;
    }

    static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").Substring(0, 12);
    }

    static int Main(string[] args)
    {
        string modsFolder = args[0], exe = args[1];
        List<string> errors = new List<string>();
        List<ModDef> mods = ModLoader.LoadFolder(modsFolder, errors);
        Check(errors.Count == 0, "todos os .json carregam (" + string.Join("; ", errors.ToArray()) + ")");
        Check(mods.Count == 5, "5 mods carregados (" + mods.Count + ")");
        // o mod do Remote Play e uma alternativa ao do teclado: e testado separado, no fim
        ModDef remote = mods.Find(delegate(ModDef m) { return m.Id == "keyboard-controller-remote-play"; });
        ModDef keyboard = mods.Find(delegate(ModDef m) { return m.Id == "keyboard-controller"; });
        Check(remote != null && keyboard != null && PatchEngine.Overlaps(remote, keyboard), "mod do Remote Play conflita com o do teclado");
        ModDef shared = mods.Find(delegate(ModDef m) { return m.Id == "remote-play-shared-player1"; });
        Check(shared != null && PatchEngine.Overlaps(shared, remote) && PatchEngine.Overlaps(shared, keyboard), "os tres mods de teclado conflitam entre si");
        List<ModDef> all = mods;
        mods = mods.FindAll(delegate(ModDef m) { return m != remote && m != shared; });
        string original = Hash(exe);

        Dictionary<string, ModState> states = GameExe.ReadStates(exe, mods);
        foreach (ModDef m in mods) Check(states[m.Id] == ModState.NotInstalled, m.Id + " comeca nao instalado");
        for (int i = 0; i < mods.Count; i++)
            for (int j = i + 1; j < mods.Count; j++)
                Check(!PatchEngine.Overlaps(mods[i], mods[j]), mods[i].Id + " nao conflita com " + mods[j].Id);

        // instala um por vez e imprime o hash para comparar com o patch.py
        List<string> wanted = new List<string>();
        foreach (ModDef m in mods)
        {
            wanted.Add(m.Id);
            ApplyResult r = GameExe.ApplySelection(exe, mods, wanted);
            Check(r.Installed == 1 && r.Removed == 0, "instala " + m.Id);
            Console.WriteLine("HASH " + string.Join("+", wanted.ToArray()) + " " + Hash(exe));
        }
        Check(File.Exists(exe + ".original") && Hash(exe + ".original") == original, "backup criado a partir do exe limpo");
        Check(GameExe.ApplySelection(exe, mods, wanted).Installed == 0, "aplicar de novo nao muda nada");

        // remove so o do meio
        wanted.RemoveAt(1);
        ApplyResult removed = GameExe.ApplySelection(exe, mods, wanted);
        states = GameExe.ReadStates(exe, mods);
        Check(removed.Removed == 1 && states[mods[1].Id] == ModState.NotInstalled
              && states[mods[0].Id] == ModState.Installed && states[mods[2].Id] == ModState.Installed, "remove so um mod e mantem os outros");

        // remove tudo: volta ao original byte a byte
        GameExe.ApplySelection(exe, mods, new List<string>());
        Check(Hash(exe) == original, "remover tudo devolve o exe original");

        // restaurar o backup remove todos os mods de uma vez
        GameExe.ApplySelection(exe, mods, new List<string> { mods[0].Id, mods[2].Id });
        Check(GameExe.HasBackup(exe), "backup existe");
        GameExe.RestoreBackup(exe, mods);
        Check(Hash(exe) == original, "restaurar o backup devolve o exe original");

        // mod do Remote Play: instala sozinho, recusa junto com o do teclado, e sai limpo
        ApplyResult rp = GameExe.ApplySelection(exe, all, new List<string> { remote.Id, mods[0].Id });
        Check(rp.Installed == 2, "instala o mod do Remote Play com outro mod");
        Console.WriteLine("HASH remote-play " + Hash(exe));
        bool clash2 = false;
        try { GameExe.ApplySelection(exe, all, new List<string> { remote.Id, keyboard.Id }); }
        catch (ModManagerException ex) { clash2 = ex.Key == "errConflict"; }
        Check(clash2, "recusa Remote Play + teclado juntos");
        GameExe.ApplySelection(exe, all, new List<string>());
        Check(Hash(exe) == original, "remover o mod do Remote Play devolve o original");

        // variante "teclado ou controle no Jogador 1": instala, troca pela outra variante e sai limpo
        Check(GameExe.ApplySelection(exe, all, new List<string> { shared.Id }).Installed == 1, "instala a variante Jogador 1 compartilhado");
        Console.WriteLine("HASH shared-player1 " + Hash(exe));
        ApplyResult swap = GameExe.ApplySelection(exe, all, new List<string> { remote.Id });
        Check(swap.Removed == 1 && swap.Installed == 1 && GameExe.ReadStates(exe, all)[remote.Id] == ModState.Installed, "troca uma variante pela outra em um Aplicar");
        GameExe.ApplySelection(exe, all, new List<string>());
        Check(Hash(exe) == original, "remover as variantes devolve o original");

        // backup de outra versao do jogo: nao restaura, e e trocado na proxima instalacao
        byte[] stale = File.ReadAllBytes(exe + ".original");
        stale[stale.Length - 1] ^= 0xFF;
        File.WriteAllBytes(exe + ".original", stale);
        bool refusedStale = false;
        try { GameExe.RestoreBackup(exe, all); }
        catch (ModManagerException ex) { refusedStale = ex.Key == "errStaleBackup"; }
        Check(refusedStale && Hash(exe) == original, "nao restaura backup de outra versao");
        Check(GameExe.ApplySelection(exe, all, new List<string> { mods[0].Id }).BackupCreated && Hash(exe + ".original") == original, "backup antigo e substituido ao instalar");
        GameExe.ApplySelection(exe, all, new List<string>());
        Check(!GameExe.HasBackup(exe + ".nao-existe"), "sem backup, HasBackup e falso");

        // exe de outra versao: recusa e nao grava nada
        byte[] bytes = File.ReadAllBytes(exe);
        int offset = PatchEngine.RvaToOffset(bytes, mods[0].Patches[0].Rva);
        bytes[offset] ^= 0xFF;
        File.WriteAllBytes(exe, bytes);
        string before = Hash(exe);
        Check(GameExe.ReadStates(exe, mods)[mods[0].Id] == ModState.Incompatible, "exe alterado vira incompativel");
        bool refused = false;
        try { GameExe.ApplySelection(exe, mods, new List<string> { mods[0].Id, mods[1].Id }); }
        catch (ModManagerException ex) { refused = ex.Key == "errIncompatible"; }
        Check(refused && Hash(exe) == before, "recusa mod incompativel sem gravar nada");

        // mod que mexe no mesmo trecho de outro: conflito
        ModDef clash = ModLoader.Parse("{\"id\":\"clash\",\"patches\":[{\"rva\":\"0x244985\",\"original\":\"f8\",\"patched\":\"00\"}]}", "clash.json");
        ModDef ultrawide = mods.Find(delegate(ModDef m) { return m.Id == "ultrawide"; });
        Check(ultrawide != null && PatchEngine.Overlaps(clash, ultrawide), "detecta conflito entre mods");

        bool badJson = false;
        try { ModLoader.Parse("{\"id\":\"x\",\"patches\":[{\"rva\":\"0x10\",\"original\":\"00\",\"patched\":\"0000\"}]}", "x.json"); }
        catch (FormatException) { badJson = true; }
        Check(badJson, "recusa patch com tamanhos diferentes");

        Console.WriteLine(failures == 0 ? "TUDO OK" : failures + " FALHA(S)");
        return failures == 0 ? 0 : 1;
    }
}
