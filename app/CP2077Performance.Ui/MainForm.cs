using System.Globalization;
using CP2077Performance.Core.Application;
using CP2077Performance.Core.Backup;
using CP2077Performance.Core.GameInstallation;
using CP2077Performance.Core.Profile;

namespace CP2077Performance.Ui;

internal partial class MainForm : Form
{
    private readonly AppState _state;

    private TextBox _gamePathBox = null!;
    private Label _statusLabel = null!;
    private Label _layersLabel = null!;
    private TabControl _tabs = null!;
    private TextBox _resultBox = null!;
    private DataGridView _profileGrid = null!;
    private DataGridView _engineGrid = null!;
    private DataGridView _menuGrid = null!;
    private DataGridView _backupGrid = null!;
    private DataGridView _logGrid = null!;
    private CheckedListBox _variantsBox = null!;
    private Button _applyButton = null!;
    private Button _simulateButton = null!;
    private Button _restoreButton = null!;
    private Button _discardButton = null!;

    public MainForm(AppState state)
    {
        _state = state;
        BuildUi();
        LoadGamePath();
        RefreshAll();
    }

    // ---------------------------------------------------------------- construcao

    private void BuildUi()
    {
        Text = Strings.Get("AppTitle");
        Width = 1100;
        Height = 720;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // --- caminho do jogo
        var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        pathRow.Controls.Add(new Label { Text = Strings.Get("GameFolder"), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 6, 0) });
        _gamePathBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 0) };
        pathRow.Controls.Add(_gamePathBox);

        var browse = new Button { Text = Strings.Get("Browse"), AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        browse.Click += OnBrowse;
        pathRow.Controls.Add(browse);

        var validate = new Button { Text = Strings.Get("Validate"), AutoSize = true };
        validate.Click += OnValidate;
        pathRow.Controls.Add(validate);

        root.Controls.Add(pathRow, 0, 0);

        _statusLabel = new Label { Dock = DockStyle.Fill, AutoSize = true, ForeColor = SystemColors.GrayText };
        root.Controls.Add(_statusLabel, 0, 1);

        _layersLabel = new Label { Dock = DockStyle.Fill, AutoSize = true };
        root.Controls.Add(_layersLabel, 0, 2);

        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(BuildProfileTab());
        _tabs.TabPages.Add(BuildEngineTab());
        _tabs.TabPages.Add(BuildMenuTab());
        _tabs.TabPages.Add(BuildBackupsTab());
        _tabs.TabPages.Add(BuildLogTab());
        root.Controls.Add(_tabs, 0, 3);

        // --- acoes
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, AutoSize = true };
        for (var i = 0; i < 5; i++)
        {
            actions.ColumnStyles.Add(i < 4
                ? new ColumnStyle(SizeType.AutoSize)
                : new ColumnStyle(SizeType.Percent, 100));
        }

        _simulateButton = new Button { Text = Strings.Get("Simulate"), AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        _simulateButton.Click += OnSimulate;

        _applyButton = new Button { Text = Strings.Get("Apply"), AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        _applyButton.Click += OnApply;

        _restoreButton = new Button { Text = Strings.Get("Restore"), AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        _restoreButton.Click += OnRestore;

        _discardButton = new Button { Text = Strings.Get("Discard"), AutoSize = true, Margin = new Padding(0, 0, 12, 0) };
        _discardButton.Click += OnDiscard;

        actions.Controls.Add(_simulateButton, 0, 0);
        actions.Controls.Add(_applyButton, 1, 0);
        actions.Controls.Add(_restoreButton, 2, 0);
        actions.Controls.Add(_discardButton, 3, 0);

        _resultBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = SystemColors.Window,
        };
        actions.Controls.Add(_resultBox, 4, 0);

        root.Controls.Add(actions, 0, 4);
        Controls.Add(root);
    }

    private TabPage BuildProfileTab()
    {
        var page = new TabPage(Strings.Get("TabProfile"));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        var nameLabel = new Label { Text = $"{Strings.Get("ProfileName")}: ", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        var nameValue = new Label { Text = _state.Profile.Name, AutoSize = true, Margin = new Padding(0, 6, 20, 0) };
        var versionLabel = new Label { Text = $"{Strings.Get("Version")}: ", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        var versionValue = new Label { Text = _state.Profile.Version, AutoSize = true, Margin = new Padding(0, 6, 20, 0) };
        header.Controls.AddRange(new Control[] { nameLabel, nameValue, versionLabel, versionValue });

        var variantsLabel = new Label { Text = Strings.Get("Variants"), AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
        header.Controls.Add(variantsLabel);

        _variantsBox = new CheckedListBox { AutoSize = true, Height = 70, Margin = new Padding(0, 2, 0, 4) };
        foreach (var (id, variant) in _state.Profile.Variants)
        {
            _variantsBox.Items.Add(new VariantItem(id, variant.Label));
        }
        _variantsBox.ItemCheck += OnVariantCheck;

        layout.Controls.Add(header, 0, 0);

        _profileGrid = CreateGrid();
        layout.Controls.Add(_profileGrid, 0, 1);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = Strings.Get("TokenWarning"),
        };
        layout.Controls.Add(note, 0, 2);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildEngineTab()
    {
        var page = new TabPage(Strings.Get("TabEngine"));
        _engineGrid = CreateGrid();
        page.Controls.Add(_engineGrid);
        return page;
    }

    private TabPage BuildMenuTab()
    {
        var page = new TabPage(Strings.Get("TabMenu"));
        _menuGrid = CreateGrid();
        page.Controls.Add(_menuGrid);
        return page;
    }

    private TabPage BuildBackupsTab()
    {
        var page = new TabPage(Strings.Get("TabBackups"));
        _backupGrid = CreateGrid();
        page.Controls.Add(_backupGrid);
        return page;
    }

    private TabPage BuildLogTab()
    {
        var page = new TabPage(Strings.Get("TabLog"));
        _logGrid = CreateGrid();
        page.Controls.Add(_logGrid);
        return page;
    }

    private static string OriginText(Core.Application.ValueOrigin origin) => origin switch
    {
        Core.Application.ValueOrigin.Variant => Strings.Get("OriginVariant"),
        Core.Application.ValueOrigin.SchemaDefault => Strings.Get("OriginDefault"),
        _ => Strings.Get("OriginProfile"),
    };

    /// <summary>
    /// Larguras deterministicas: as colunas do meio tem tamanho fixo (para o
    /// cabecalho caber) e a ultima preenche o espaco restante.
    /// </summary>
    private static void FinishColumns(DataGridView grid)
    {
        for (var i = 0; i < grid.Columns.Count; i++)
        {
            var column = grid.Columns[i];
            if (i == grid.Columns.Count - 1)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            else
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.Width = Math.Max(96, (int)(column.FillWeight * 11));
            }
        }
    }

    private static void AddColumn(DataGridView grid, string header, int weight)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = weight });
    }

    private static DataGridViewRow AddRow(DataGridView grid, params object[] values)
    {
        var index = grid.Rows.Add(values);
        return grid.Rows[index];
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
    }

    private sealed record VariantItem(string Id, string Label)
    {
        public override string ToString() => Label;
    }

    // ---------------------------------------------------------------- acoes

    private void LoadGamePath()
    {
        _gamePathBox.Text = _state.GamePath;
        if (!string.IsNullOrWhiteSpace(_state.GamePath))
        {
            _state.SetGamePath(_state.GamePath);
        }
    }

    private void OnBrowse(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Strings.Get("NoGamePath"),
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _gamePathBox.Text = dialog.SelectedPath;
            OnValidate(sender, e);
        }
    }

    private void OnValidate(object? sender, EventArgs e)
    {
        var path = _gamePathBox.Text.Trim();
        if (!_state.SetGamePath(path))
        {
            _statusLabel.Text = string.IsNullOrWhiteSpace(path)
                ? Strings.Get("NoGamePath")
                : Strings.Get("InvalidFolder");
            _layersLabel.Text = "";
            UpdateActionStates();
            RefreshGrids();
            return;
        }

        _statusLabel.Text = $"{Strings.Get("Target")}: {_state.Profile.Target}";
        UpdateLayersLabel();
        RefreshGrids();
    }

    private void UpdateLayersLabel()
    {
        var lines = new List<string> { Strings.Get("Layers") + ":" };
        foreach (var layer in _state.Layers)
        {
            var status = layer.Available ? Strings.Get("Available") : Strings.Get("Unavailable");
            lines.Add($"  {layer.DisplayName}: {status}  -  {layer.TargetPath}  ({layer.Origin})");
        }

        var leftover = _state.DetectLeftoverPanel();
        if (leftover is not null)
        {
            lines.Add("");
            lines.Add(Core.GameInstallation.LeftoverPanelDetector.NoticeMessage);
            lines.Add($"  {leftover.Path}");
        }

        _layersLabel.Text = string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Desabilita as operacoes que nao podem ser executadas agora, e diz o
    /// motivo.
    /// </summary>
    private void UpdateActionStates()
    {
        string? reason = null;

        if (_state.Locator is null)
        {
            reason = Strings.Get("NoGamePath");
        }
        else if (_state.Validator is not null && !_state.Validator.IsValidInstallation())
        {
            reason = Strings.Get("InvalidFolder");
        }
        else if (new Core.GameInstallation.GameRunningGuard().IsGameRunning())
        {
            reason = Strings.Get("GameRunning");
        }

        var enabled = reason is null;
        _applyButton.Enabled = enabled;
        _simulateButton.Enabled = enabled;

        _applyButton.Text = Strings.Get("Apply");
        _simulateButton.Text = Strings.Get("Simulate");

        if (reason is not null)
        {
            _applyButton.Text = $"{Strings.Get("Apply")} ({reason})";
            _simulateButton.Text = $"{Strings.Get("Simulate")} ({reason})";
        }

        _restoreButton.Enabled = _state.Backups.List().Count > 0;
        _discardButton.Enabled = true;
    }

    private void OnVariantCheck(object? sender, ItemCheckEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _variantsBox.Items.Count)
        {
            return;
        }

        if (_variantsBox.Items[e.Index] is not VariantItem item)
        {
            return;
        }

        if (e.NewValue is CheckState.Checked)
        {
            _state.EnabledVariants.Add(item.Id);
        }
        else
        {
            _state.EnabledVariants.Remove(item.Id);
        }

        RefreshGrids();
    }

    private void OnSimulate(object? sender, EventArgs e)
    {
        var plan = _state.Plan();
        if (plan is null)
        {
            _resultBox.Text = Strings.Get("NoGamePath");
            return;
        }

        var lines = new List<string> { Strings.Get("Simulated") };
        lines.AddRange(DescribePlan(plan));
        _resultBox.Text = string.Join(Environment.NewLine, lines);
    }

    private void OnApply(object? sender, EventArgs e)
    {
        var result = _state.Apply();
        if (result is null)
        {
            _resultBox.Text = Strings.Get("NoGamePath");
            return;
        }

        if (!result.Ok)
        {
            _resultBox.Text = string.Join(Environment.NewLine, result.Errors);
            return;
        }

        var lines = new List<string>();
        if (result.ChangedFiles.Count == 0)
        {
            lines.Add(Strings.Get("NothingToApply"));
        }
        else
        {
            lines.Add(Strings.Get("Applied"));
            lines.AddRange(result.ChangedFiles.Select(f => "  " + f));
        }

        if (result.BackedUpFiles.Count > 0)
        {
            lines.Add("");
            lines.Add(Strings.Get("BackupOf") + ":");
            lines.AddRange(result.BackedUpFiles.Select(f => "  " + f));
        }

        if (result.Errors.Count > 0)
        {
            lines.Add("");
            lines.Add(Strings.Get("Partial"));
            lines.AddRange(result.Errors);
        }

        _resultBox.Text = string.Join(Environment.NewLine, lines);
        RefreshGrids();
    }

    private void OnRestore(object? sender, EventArgs e)
    {
        if (_backupGrid.CurrentRow?.Tag is not BackupEntry entry)
        {
            _resultBox.Text = Strings.Get("BackupEmpty");
            return;
        }

        var outcome = _state.Backups.Restore(entry);
        _resultBox.Text = outcome.Ok
            ? Strings.Get("Restored") + Environment.NewLine + string.Join(Environment.NewLine, outcome.Restored)
            : Strings.Get("RestoreMissing") + Environment.NewLine + string.Join(Environment.NewLine, outcome.Missing);

        RefreshGrids();
    }

    private void OnDiscard(object? sender, EventArgs e)
    {
        _state.ResetProfile();
        RefreshGrids();
        _resultBox.Text = Strings.Get("ProfileSaved");
    }

    private static IEnumerable<string> DescribePlan(ApplyPlan plan)
    {
        foreach (var issue in plan.Validation.Issues)
        {
            yield return $"  [{issue.Severity}] {issue.Name}: {issue.Message}";
        }

        foreach (var change in plan.MenuChanges)
        {
            var origin = plan.Origins.TryGetValue(change.Option, out var o) ? o.ToString() : "";
            yield return $"  {change.Option}: {change.Before} -> {change.After} ({origin})";
        }

        foreach (var change in plan.IniChanges)
        {
            yield return $"  {change.Section}/{change.Key}: {change.Before ?? "(novo)"} -> {change.After}";
        }

        foreach (var change in plan.CetChanges)
        {
            yield return $"  {change.Patch}: {change.Before} -> {change.After}";
        }
    }

    // ---------------------------------------------------------------- atualizacao das abas

    private void RefreshAll()
    {
        UpdateLayersLabel();
        UpdateActionStates();
        RefreshGrids();
    }

    private void RefreshGrids()
    {
        FillProfileGrid();
        FillEngineGrid();
        FillMenuGrid();
        FillBackupGrid();
        FillLogGrid();
    }

    private void FillProfileGrid()
    {
        _profileGrid.Rows.Clear();
        _profileGrid.Columns.Clear();

        _profileGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Option"), FillWeight = 30 });
        _profileGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Value"), FillWeight = 20 });
        _profileGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Desired"), FillWeight = 20 });
        _profileGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Origin"), FillWeight = 30 });

        var plan = _state.Plan();
        var effective = plan is null
            ? null
            : Core.Profile.VariantResolver.Resolve(_state.Profile, _state.EnabledVariants, _state.Schema!);

        foreach (var (option, value) in _state.Profile.UserSettingsValues)
        {
            var origin = plan?.Origins.TryGetValue(option, out var o) == true ? OriginText(o) : Strings.Get("OriginProfile");
            var effectiveValue = effective?.MenuValues.TryGetValue(option, out var ev) == true ? ev.Display : value.Display;
            _profileGrid.Rows.Add(option, value.Display, effectiveValue, origin);
        }

        foreach (var (variantId, variant) in _state.Profile.Variants)
        {
            foreach (var (option, value) in variant.UserSettingsValues)
            {
                var enabled = _state.EnabledVariants.Contains(variantId);
                var origin = enabled ? Strings.Get("OriginVariant") : Strings.Get("OriginDefault");
                var effectiveValue = enabled ? value.Display : Strings.Get("InEffect");
                _profileGrid.Rows.Add(option, value.Display, effectiveValue, origin);
            }
        }
        FinishColumns(_profileGrid);
    }

    private void FillEngineGrid()
    {
        _engineGrid.Rows.Clear();
        _engineGrid.Columns.Clear();

        _engineGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Section"), FillWeight = 25 });
        _engineGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Key"), FillWeight = 25 });
        _engineGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Value"), FillWeight = 12 });
        _engineGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Description"), FillWeight = 38 });

        var ini = _state.ReadEngineIni();

        foreach (var (section, keys) in _state.Profile.UserIni)
        {
            foreach (var (key, value) in keys)
            {
                ini.TryGetValue($"{section}/{key}", out var existing);
                var description = existing?.Comment;
                if (string.IsNullOrWhiteSpace(description))
                {
                    description = Strings.Get("NoDescription");
                }

                var current = existing is null ? Strings.Get("NewKey") : existing.Value;
                _engineGrid.Rows.Add(section, key, $"{current} -> {value}", description);
            }
        }
        FinishColumns(_engineGrid);
    }

    private void FillMenuGrid()
    {
        _menuGrid.Rows.Clear();
        _menuGrid.Columns.Clear();

        _menuGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Option"), FillWeight = 34 });
        _menuGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Desired"), FillWeight = 20 });
        _menuGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("InEffect"), FillWeight = 20 });
        _menuGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Origin"), FillWeight = 26 });

        var effective = _state.ReadEffectiveMenuValues();
        var plan = _state.Plan();
        var desired = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (option, value) in _state.Profile.UserSettingsValues)
        {
            desired[option] = value.Display;
        }

        if (plan is not null)
        {
            foreach (var (option, value) in Core.Profile.VariantResolver
                         .Resolve(_state.Profile, _state.EnabledVariants, _state.Schema!)
                         .MenuValues)
            {
                desired[option] = value.Display;
            }
        }

        foreach (var (option, wanted) in desired.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var inEffect = effective.TryGetValue(option, out var current) ? current : Strings.Get("MenuMissing");
            var origin = plan?.Origins.TryGetValue(option, out var o) == true ? OriginText(o) : Strings.Get("OriginProfile");
            var row = AddRow(_menuGrid, option, wanted, inEffect, origin);
            if (inEffect != wanted)
            {
                row.Cells[2].Style.ForeColor = Color.DarkOrange;
            }
        }
        FinishColumns(_menuGrid);
    }

    private void FillBackupGrid()
    {
        _backupGrid.Rows.Clear();
        _backupGrid.Columns.Clear();

        _backupGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("BackupOf"), FillWeight = 30 });
        _backupGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Files"), FillWeight = 70 });

        foreach (var entry in _state.Backups.List())
        {
            var row = AddRow(_backupGrid, entry.Label, string.Join(", ", entry.Files));
            row.Tag = entry;
        }
        FinishColumns(_backupGrid);
    }

    private void FillLogGrid()
    {
        _logGrid.Rows.Clear();
        _logGrid.Columns.Clear();

        _logGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("When"), FillWeight = 25 });
        _logGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Files"), FillWeight = 30 });
        _logGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Get("Changes"), FillWeight = 45 });

        foreach (var entry in _state.Log.Read())
        {
            _logGrid.Rows.Add(
                entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                string.Join(", ", entry.Files),
                string.Join("; ", entry.Details));
        }
        FinishColumns(_logGrid);
    }
}
