using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Shk2Modman
{
    /// <summary>A janela: lista de mods com caixinhas, descricao e os botoes.</summary>
    public class MainForm : Form
    {
        readonly Label lblGame = new Label();
        readonly Label lblLanguage = new Label();
        readonly ComboBox cmbLanguage = new ComboBox();
        readonly TextBox txtPath = new TextBox();
        readonly Button btnBrowse = new Button();
        readonly ListView list = new ListView();
        readonly TextBox txtDescription = new TextBox();
        readonly Label lblStatus = new Label();
        readonly Button btnRefresh = new Button();
        readonly Button btnModsFolder = new Button();
        readonly Button btnRestore = new Button();
        readonly Button btnPlay = new Button();
        readonly Button btnApply = new Button();

        readonly string modsFolder;
        readonly Settings settings = Settings.Load();
        List<ModDef> mods = new List<ModDef>();
        Dictionary<string, ModState> states = new Dictionary<string, ModState>();
        string exePath;
        bool loading;

        public MainForm()
        {
            modsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
            if (settings.Language == "pt" || settings.Language == "en") T.Lang = settings.Language;
            BuildLayout();
            ApplyTexts();
            Shown += delegate { LoadEverything(true); };
        }

        // ---------- montagem da tela ----------

        void BuildLayout()
        {
            Font = new Font("Segoe UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { /* sem icone: usa o padrao do Windows */ }
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(820, 520);
            MinimumSize = new Size(760, 420);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(10);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            // linha 1: caminho do jogo
            TableLayoutPanel top = NewRow(5);
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblGame.AutoSize = true;
            lblGame.Anchor = AnchorStyles.Left;
            lblGame.Margin = new Padding(0, 0, 6, 0);
            txtPath.ReadOnly = true;
            txtPath.Dock = DockStyle.Fill;
            txtPath.Margin = new Padding(0, 3, 6, 3);
            SetupButton(btnBrowse, "browse");
            btnBrowse.Click += delegate { BrowseForGame(); };
            top.Controls.Add(lblGame, 0, 0);
            top.Controls.Add(txtPath, 1, 0);
            top.Controls.Add(btnBrowse, 2, 0);
            lblLanguage.AutoSize = true;
            lblLanguage.Anchor = AnchorStyles.Left;
            lblLanguage.Margin = new Padding(14, 0, 6, 0);
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Items.Add("Português");
            cmbLanguage.Items.Add("English");
            cmbLanguage.SelectedIndex = T.Lang == "pt" ? 0 : 1;
            cmbLanguage.Width = 100;
            cmbLanguage.Anchor = AnchorStyles.Right;
            cmbLanguage.Margin = new Padding(0);
            cmbLanguage.SelectedIndexChanged += delegate { ChangeLanguage(); };
            top.Controls.Add(lblLanguage, 3, 0);
            top.Controls.Add(cmbLanguage, 4, 0);
            root.Controls.Add(top, 0, 0);

            // linha 2: lista de mods
            list.View = View.Details;
            list.CheckBoxes = true;
            list.FullRowSelect = true;
            list.HideSelection = false;
            list.MultiSelect = false;
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            list.Dock = DockStyle.Fill;
            list.Margin = new Padding(0, 8, 0, 8);
            list.Columns.Add("", 380);
            list.Columns.Add("", 300);
            list.ItemCheck += OnItemCheck;
            list.SelectedIndexChanged += delegate { ShowDescription(); };
            list.Resize += delegate { ResizeColumns(); };
            root.Controls.Add(list, 0, 1);

            // linha 3: descricao do mod selecionado
            txtDescription.Multiline = true;
            txtDescription.ReadOnly = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            txtDescription.Dock = DockStyle.Fill;
            txtDescription.BackColor = SystemColors.Window;
            txtDescription.Margin = new Padding(0, 0, 0, 8);
            root.Controls.Add(txtDescription, 0, 2);

            // linha 4: mensagem e botoes
            TableLayoutPanel bottom = NewRow(6);
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 5; i++) bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblStatus.AutoSize = false;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.AutoEllipsis = true;
            SetupButton(btnRefresh, "refresh");
            SetupButton(btnModsFolder, "modsFolder");
            SetupButton(btnRestore, "restore");
            SetupButton(btnPlay, "play");
            SetupButton(btnApply, "apply");
            btnApply.Font = new Font(Font, FontStyle.Bold);
            btnRefresh.Click += delegate { LoadEverything(false); };
            btnModsFolder.Click += delegate { OpenModsFolder(); };
            btnRestore.Click += delegate { RestoreOriginal(); };
            btnPlay.Click += delegate { StartGame(); };
            btnApply.Click += delegate { ApplyChanges(); };
            bottom.Controls.Add(lblStatus, 0, 0);
            bottom.Controls.Add(btnRefresh, 1, 0);
            bottom.Controls.Add(btnModsFolder, 2, 0);
            bottom.Controls.Add(btnRestore, 3, 0);
            bottom.Controls.Add(btnPlay, 4, 0);
            bottom.Controls.Add(btnApply, 5, 0);
            root.Controls.Add(bottom, 0, 3);

            AcceptButton = btnApply;
        }

        /// <summary>Escreve os textos fixos da tela no idioma atual.</summary>
        void ApplyTexts()
        {
            Text = T.S("title");
            lblGame.Text = T.S("game");
            lblLanguage.Text = T.S("language");
            btnBrowse.Text = T.S("browse");
            btnRefresh.Text = T.S("refresh");
            btnModsFolder.Text = T.S("modsFolder");
            btnRestore.Text = T.S("restore");
            btnPlay.Text = T.S("play");
            btnApply.Text = T.S("apply");
            list.Columns[0].Text = T.S("colMod");
            list.Columns[1].Text = T.S("colState");
        }

        void ChangeLanguage()
        {
            T.Lang = cmbLanguage.SelectedIndex == 0 ? "pt" : "en";
            settings.Language = T.Lang;
            settings.Save();
            ApplyTexts();
            // Mantem as caixinhas como estao (inclusive mudancas ainda nao aplicadas).
            loading = true;
            foreach (ListViewItem item in list.Items) item.Text = ((ModDef)item.Tag).GetName(T.Lang);
            loading = false;
            UpdateRows();
            ShowDescription();
        }

        static TableLayoutPanel NewRow(int columns)
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.AutoSize = true;
            row.ColumnCount = columns;
            row.RowCount = 1;
            row.Margin = new Padding(0);
            return row;
        }

        static void SetupButton(Button button, string textKey)
        {
            button.Text = T.S(textKey);
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Padding = new Padding(8, 3, 8, 3);
            button.Margin = new Padding(6, 0, 0, 0);
            button.Anchor = AnchorStyles.Right;
        }

        void ResizeColumns()
        {
            if (list.Columns.Count < 2) return;
            int width = list.ClientSize.Width;
            list.Columns[0].Width = (int)(width * 0.55);
            list.Columns[1].Width = width - list.Columns[0].Width;
        }

        // ---------- carregar mods e estado do jogo ----------

        void LoadEverything(bool firstTime)
        {
            List<string> errors = new List<string>();
            mods = ModLoader.LoadFolder(modsFolder, errors);

            if (exePath == null || !File.Exists(exePath))
            {
                exePath = File.Exists(settings.GameExe) ? settings.GameExe : SteamLocator.FindGameExe();
                if (exePath != null) { settings.GameExe = exePath; settings.Save(); }
            }
            txtPath.Text = exePath ?? "";

            bool ok = RefreshStates();
            if (errors.Count > 0)
                MessageBox.Show(this, T.S("badFiles") + "\n\n" + string.Join("\n", errors.ToArray()), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (ok && firstTime) OfferReapply();
        }

        /// <summary>Le o Shrek.exe e redesenha a lista. Retorna false se nao deu para ler.</summary>
        bool RefreshStates()
        {
            states = new Dictionary<string, ModState>();
            bool ok = false;
            if (exePath != null)
            {
                try { states = GameExe.ReadStates(exePath, mods); ok = true; }
                catch (Exception ex)
                {
                    MessageBox.Show(this, T.S("errRead") + "\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            string selectedId = list.SelectedItems.Count > 0 ? ((ModDef)list.SelectedItems[0].Tag).Id : null;
            loading = true;
            list.BeginUpdate();
            list.Items.Clear();
            foreach (ModDef mod in mods)
            {
                ListViewItem item = new ListViewItem(mod.GetName(T.Lang));
                item.Tag = mod;
                item.SubItems.Add("");
                item.Checked = StateOf(mod) == ModState.Installed;
                list.Items.Add(item);
                if (mod.Id == selectedId) item.Selected = true;
            }
            list.EndUpdate();
            loading = false;

            if (list.SelectedItems.Count == 0 && list.Items.Count > 0) list.Items[0].Selected = true;
            ResizeColumns();
            UpdateRows();
            ShowDescription();
            return ok;
        }

        ModState StateOf(ModDef mod)
        {
            ModState state;
            return states.TryGetValue(mod.Id, out state) ? state : ModState.Incompatible;
        }

        /// <summary>Atualiza a coluna "Estado", o botao Aplicar e a mensagem de baixo.</summary>
        void UpdateRows()
        {
            bool pending = false;
            foreach (ListViewItem item in list.Items)
            {
                ModDef mod = (ModDef)item.Tag;
                ModState state = StateOf(mod);
                string text;
                Color color = SystemColors.WindowText;
                if (exePath == null) text = "";
                else if (state == ModState.Incompatible)
                {
                    // Um mod instalado que mexe no mesmo trecho faz este parecer incompativel: explica o motivo certo.
                    ModDef blocker = InstalledOverlap(mod);
                    text = blocker == null ? T.S("stIncompatible") : string.Format(T.S("stBlocked"), blocker.GetName(T.Lang));
                    color = SystemColors.GrayText;
                }
                else if (item.Checked && state == ModState.NotInstalled) { text = T.S("stWillInstall"); pending = true; }
                else if (!item.Checked && state == ModState.Installed) { text = T.S("stWillRemove"); pending = true; }
                else text = T.S(state == ModState.Installed ? "stInstalled" : "stNotInstalled");
                item.SubItems[1].Text = text;
                item.ForeColor = color;
            }
            btnApply.Enabled = pending;
            btnPlay.Enabled = exePath != null;
            btnRestore.Enabled = GameExe.HasBackup(exePath);
            if (exePath == null) lblStatus.Text = T.S("noGame");
            else if (mods.Count == 0) lblStatus.Text = T.S("noMods");
            else lblStatus.Text = T.S(pending ? "pending" : "ready");
        }

        /// <summary>Mod instalado que altera o mesmo trecho do exe que "mod", ou null.</summary>
        ModDef InstalledOverlap(ModDef mod)
        {
            foreach (ModDef other in mods)
                if (other != mod && StateOf(other) == ModState.Installed && PatchEngine.Overlaps(mod, other)) return other;
            return null;
        }

        void ShowDescription()
        {
            if (list.SelectedItems.Count == 0) { txtDescription.Text = ""; return; }
            ModDef mod = (ModDef)list.SelectedItems[0].Tag;
            List<string> lines = new List<string>();
            lines.Add(mod.GetDescription(T.Lang).Replace("\r", "").Replace("\n", Environment.NewLine));
            lines.Add("");
            if (mod.Version != "") lines.Add(T.S("version") + ": " + mod.Version);
            if (mod.GameBuild != "") lines.Add(T.S("madeFor") + ": " + mod.GameBuild);
            lines.Add(T.S("file") + ": mods\\" + mod.File);
            txtDescription.Text = string.Join(Environment.NewLine, lines.ToArray());
        }

        // ---------- acoes ----------

        void OnItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (loading) return;
            ModDef mod = (ModDef)list.Items[e.Index].Tag;

            // Mod incompativel (ou jogo nao encontrado) nao pode ser marcado.
            if (exePath == null || StateOf(mod) == ModState.Incompatible)
            {
                e.NewValue = e.CurrentValue;
                return;
            }

            // Dois mods que mexem no mesmo trecho nao podem ficar marcados juntos.
            if (e.NewValue == CheckState.Checked)
            {
                foreach (ListViewItem other in list.Items)
                {
                    ModDef otherMod = (ModDef)other.Tag;
                    if (other.Index != e.Index && other.Checked && PatchEngine.Overlaps(mod, otherMod))
                    {
                        e.NewValue = e.CurrentValue;
                        MessageBox.Show(this, T.S("errConflict") + "\n\n" + mod.GetName(T.Lang) + "\n" + otherMod.GetName(T.Lang), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // O valor novo so vale depois que este evento termina.
            BeginInvoke((MethodInvoker)delegate { UpdateRows(); });
        }

        void ApplyChanges()
        {
            if (exePath == null) return;
            if (Process.GetProcessesByName("Shrek").Length > 0)
            {
                MessageBox.Show(this, T.S("closeGame"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> wanted = new List<string>();
            foreach (ListViewItem item in list.Items)
                if (item.Checked) wanted.Add(((ModDef)item.Tag).Id);

            string message;
            try
            {
                ApplyResult result = GameExe.ApplySelection(exePath, mods, wanted);
                message = string.Format(T.S("done"), result.Installed, result.Removed);
                if (result.BackupCreated) message += T.S("backupMade");
                settings.Enabled = wanted;
                settings.Save();
            }
            catch (ModManagerException ex)
            {
                MessageBox.Show(this, T.S(ex.Key) + "\n\n" + ex.Detail, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                message = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                message = null;
            }

            RefreshStates();
            if (message != null) lblStatus.Text = message;
        }

        /// <summary>Volta o Shrek.exe para o backup original, depois de confirmar.</summary>
        void RestoreOriginal()
        {
            if (!GameExe.HasBackup(exePath)) return;
            if (Process.GetProcessesByName("Shrek").Length > 0)
            {
                MessageBox.Show(this, T.S("closeGame"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult answer = MessageBox.Show(this, T.S("restoreAsk"), T.S("restore"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;

            bool done = false;
            try
            {
                GameExe.RestoreBackup(exePath, mods);
                settings.Enabled = new List<string>();
                settings.Save();
                done = true;
            }
            catch (ModManagerException ex)
            {
                MessageBox.Show(this, T.S(ex.Key) + "\n\n" + ex.Detail, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            RefreshStates();
            if (done) lblStatus.Text = T.S("restoreDone");
        }

        /// <summary>Se a Steam desfez mods que estavam instalados, pergunta se quer reinstalar.</summary>
        void OfferReapply()
        {
            List<ModDef> undone = new List<ModDef>();
            foreach (ModDef mod in mods)
                if (settings.Enabled.Contains(mod.Id) && StateOf(mod) == ModState.NotInstalled) undone.Add(mod);
            if (undone.Count == 0) return;

            List<string> names = new List<string>();
            foreach (ModDef mod in undone) names.Add("  - " + mod.GetName(T.Lang));
            DialogResult answer = MessageBox.Show(this, string.Format(T.S("reapply"), string.Join("\n", names.ToArray())),
                T.S("reapplyTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                // Nao quis: para de perguntar nas proximas vezes.
                foreach (ModDef mod in undone) settings.Enabled.Remove(mod.Id);
                settings.Save();
                return;
            }
            loading = true;
            foreach (ListViewItem item in list.Items)
                if (undone.Contains((ModDef)item.Tag)) item.Checked = true;
            loading = false;
            ApplyChanges();
        }

        void BrowseForGame()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = T.S("pickExe");
                dialog.Filter = "Shrek.exe|Shrek.exe";
                if (exePath != null) dialog.InitialDirectory = Path.GetDirectoryName(exePath);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                exePath = dialog.FileName;
                settings.GameExe = exePath;
                settings.Save();
                txtPath.Text = exePath;
                RefreshStates();
            }
        }

        void OpenModsFolder()
        {
            try
            {
                Directory.CreateDirectory(modsFolder);
                Process.Start("explorer.exe", "\"" + modsFolder + "\"");
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        void StartGame()
        {
            // O jogo so abre quando e a Steam que inicia.
            try { Process.Start("steam://rungameid/" + SteamLocator.AppId); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
