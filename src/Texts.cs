using System.Collections.Generic;
using System.Globalization;

namespace Shrek2ModManager
{
    /// <summary>Textos da tela em ingles e portugues. O idioma vem do Windows.</summary>
    public static class T
    {
        public static string Lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? "pt" : "en";

        // chave -> { ingles, portugues }
        static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            { "title", new[] { "Shrek 2 Remastered - Mod Manager", "Shrek 2 Remastered - Gerenciador de Mods" } },
            { "game", new[] { "Game:", "Jogo:" } },
            { "browse", new[] { "Browse...", "Procurar..." } },
            { "colMod", new[] { "Mod", "Mod" } },
            { "colState", new[] { "Status", "Estado" } },
            { "refresh", new[] { "Refresh", "Atualizar" } },
            { "modsFolder", new[] { "Mods folder", "Pasta de mods" } },
            { "play", new[] { "Start game", "Iniciar jogo" } },
            { "apply", new[] { "Apply", "Aplicar" } },
            { "stInstalled", new[] { "Installed", "Instalado" } },
            { "stNotInstalled", new[] { "Not installed", "Não instalado" } },
            { "stIncompatible", new[] { "Not compatible with this game version", "Incompatível com esta versão do jogo" } },
            { "stWillInstall", new[] { "Will be installed", "Será instalado" } },
            { "stWillRemove", new[] { "Will be removed", "Será removido" } },
            { "noGame", new[] { "Shrek.exe not found. Click \"Browse...\" and select it in the game folder.", "Shrek.exe não encontrado. Clique em \"Procurar...\" e selecione-o na pasta do jogo." } },
            { "noMods", new[] { "No mods found in the \"mods\" folder.", "Nenhum mod encontrado na pasta \"mods\"." } },
            { "ready", new[] { "Tick the mods you want and click Apply.", "Marque os mods que quiser e clique em Aplicar." } },
            { "pending", new[] { "Changes not applied yet. Click Apply.", "Há mudanças ainda não aplicadas. Clique em Aplicar." } },
            { "done", new[] { "Done: {0} installed, {1} removed.", "Pronto: {0} instalado(s), {1} removido(s)." } },
            { "backupMade", new[] { " Backup saved as Shrek.exe.original.", " Backup salvo como Shrek.exe.original." } },
            { "closeGame", new[] { "The game is running. Close it before changing mods.", "O jogo está aberto. Feche-o antes de mexer nos mods." } },
            { "errIncompatible", new[] { "This mod does not match this version of the game, so nothing was changed:", "Este mod não bate com esta versão do jogo, então nada foi alterado:" } },
            { "errConflict", new[] { "These mods change the same part of the game and cannot be used together:", "Estes mods mexem no mesmo trecho do jogo e não podem ser usados juntos:" } },
            { "errWrite", new[] { "Could not write Shrek.exe. Close the game and try again.", "Não consegui gravar o Shrek.exe. Feche o jogo e tente de novo." } },
            { "errRead", new[] { "Could not read Shrek.exe:", "Não consegui ler o Shrek.exe:" } },
            { "badFiles", new[] { "Some mod files could not be read and were skipped:", "Alguns arquivos de mod não puderam ser lidos e foram ignorados:" } },
            { "reapplyTitle", new[] { "Mods were removed", "Mods foram desfeitos" } },
            { "reapply", new[] { "These mods were installed before but are no longer in the game (a Steam update or file verification undoes them):\n\n{0}\n\nInstall them again?", "Estes mods estavam instalados, mas não estão mais no jogo (atualização da Steam ou verificação de arquivos desfaz os mods):\n\n{0}\n\nInstalar de novo?" } },
            { "pickExe", new[] { "Select Shrek.exe", "Selecione o Shrek.exe" } },
            { "version", new[] { "Version", "Versão" } },
            { "madeFor", new[] { "Made for game build", "Feito para o jogo na versão" } },
            { "file", new[] { "File", "Arquivo" } }
        };

        public static string S(string key)
        {
            string[] t;
            if (!Texts.TryGetValue(key, out t)) return key;
            return Lang == "pt" ? t[1] : t[0];
        }
    }
}
