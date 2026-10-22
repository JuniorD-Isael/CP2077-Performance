using System.Globalization;
using CP2077Performance.Core.GameInstallation;
using CP2077Performance.Ui.Theme;

namespace CP2077Performance.Ui;

internal partial class MainForm : Form
{
    /// <summary>Largura preferida da janela, em pixels logicos.</summary>
    private const int DefaultWidth = 880;

    /// <summary>Altura preferida da janela enxuta, em pixels logicos.</summary>
    private const int DefaultHeight = 450;

    private readonly AppState _state;
    private readonly CyberpunkPalette _palette;

    private TableLayoutPanel _root = null!;
    private TextBox _gamePathBox = null!;
    private Panel _orientationPanel = null!;
    private Label _orientationLabel = null!;
    private Panel _statusCard = null!;
    private Label _effectLabel = null!;
    private Label _stateLabel = null!;
    private Button _applyButton = null!;
    private Button _restoreButton = null!;
    private Panel _resultPanel = null!;
    private Label _resultLabel = null!;

    /// <summary>Motivo pelo qual a aplicacao esta bloqueada, quando esta.</summary>
    private string? _blockReason;

    public MainForm(AppState state)
    {
        _state = state;
        _palette = ThemeHelper.Palette;

        AutoScaleMode = AutoScaleMode.Dpi;

        Text = Strings.Get("AppTitle");
        Width = DefaultWidth;
        Height = DefaultHeight;
        MinimumSize = new Size(760, 380);
        StartPosition = FormStartPosition.CenterScreen;

        MaterialTheme.Apply(this, _palette);

        BuildUi();
        LoadGamePath();
        RefreshAll();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        WindowState = FormWindowState.Normal;
        Size = new Size(DefaultWidth, DefaultHeight);
    }

    // ---------------------------------------------------------------- construcao

    private void BuildUi()
    {
        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(MaterialTheme.Scale(18)),
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (var i = 0; i < 6; i++)
        {
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        // linha final absorve o espaco vertical excedente quando a janela e maximizada
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _root.Controls.Add(BuildHeader(), 0, 0);
        _root.Controls.Add(BuildPathRow(), 0, 1);
        _root.Controls.Add(BuildOrientation(), 0, 2);
        _root.Controls.Add(BuildStatusCard(), 0, 3);
        _root.Controls.Add(BuildActions(), 0, 4);
        _root.Controls.Add(BuildResult(), 0, 5);

        Controls.Add(_root);
    }

    private Control BuildHeader() =>
        MaterialTheme.MakeEdgerunnersHeader(
            Strings.Get("AppTitle"),
            Strings.Get("HeaderSubtitle"),
            _palette);

    private Control BuildPathRow()
    {
        var card = MaterialTheme.MakeCard(_palette);
        card.Dock = DockStyle.Fill;
        card.AutoSize = true;
        card.Margin = new Padding(0, 0, 0, MaterialTheme.Scale(10));
        card.Padding = new Padding(
            MaterialTheme.Scale(14),
            MaterialTheme.Scale(10),
            MaterialTheme.Scale(14),
            MaterialTheme.Scale(10));

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            AutoSize = true,
            BackColor = Color.Transparent,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var caption = MaterialTheme.MakeLabel(Strings.Get("GameFolder"), LabelRole.Body, _palette);
        caption.AutoSize = true;
        caption.Anchor = AnchorStyles.Left;
        caption.Margin = new Padding(0, MaterialTheme.Scale(6), MaterialTheme.Scale(10), 0);
        row.Controls.Add(caption, 0, 0);

        _gamePathBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, MaterialTheme.Scale(3), MaterialTheme.Scale(10), 0),
            BackColor = _palette.SurfaceRaised,
            ForeColor = _palette.OnSurface,
            BorderStyle = BorderStyle.FixedSingle,
        };
        row.Controls.Add(_gamePathBox, 1, 0);

        var browse = MaterialTheme.MakeButton(Strings.Get("Browse"), ButtonVariant.Outlined, _palette);
        browse.Click += OnBrowse;
        row.Controls.Add(browse, 2, 0);

        var validate = MaterialTheme.MakeButton(Strings.Get("Validate"), ButtonVariant.Tonal, _palette);
        validate.Margin = new Padding(0);
        validate.Click += OnValidate;
        row.Controls.Add(validate, 3, 0);

        card.Controls.Add(row);
        return card;
    }

    private Control BuildOrientation()
    {
        _orientationPanel = MaterialTheme.MakeCard(_palette);
        _orientationPanel.AutoSize = true;
        _orientationPanel.Dock = DockStyle.Fill;
        _orientationPanel.Margin = new Padding(0, 0, 0, MaterialTheme.Scale(10));
        _orientationPanel.Padding = new Padding(MaterialTheme.Scale(16));

        _orientationLabel = MaterialTheme.MakeLabel("", LabelRole.Body, _palette);
        _orientationLabel.Dock = DockStyle.Fill;
        _orientationLabel.AutoSize = true;
        _orientationPanel.Controls.Add(_orientationLabel);

        return _orientationPanel;
    }

    private Control BuildStatusCard()
    {
        _statusCard = MaterialTheme.MakeCard(_palette);
        _statusCard.Dock = DockStyle.Fill;
        _statusCard.AutoSize = true;
        _statusCard.Margin = new Padding(0, 0, 0, MaterialTheme.Scale(12));
        _statusCard.Padding = new Padding(
            MaterialTheme.Scale(16),
            MaterialTheme.Scale(14),
            MaterialTheme.Scale(16),
            MaterialTheme.Scale(14));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            BackColor = Color.Transparent,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = MaterialTheme.MakeLabel(Strings.Get("StatusHeading"), LabelRole.Title, _palette);
        heading.AutoSize = true;
        heading.Margin = new Padding(0, 0, 0, MaterialTheme.Scale(6));
        layout.Controls.Add(heading, 0, 0);

        _effectLabel = MaterialTheme.MakeLabel("", LabelRole.Body, _palette);
        _effectLabel.AutoSize = true;
        layout.Controls.Add(_effectLabel, 0, 1);

        _stateLabel = MaterialTheme.MakeLabel("", LabelRole.Caption, _palette);
        _stateLabel.AutoSize = true;
        _stateLabel.Margin = new Padding(0, MaterialTheme.Scale(8), 0, 0);
        layout.Controls.Add(_stateLabel, 0, 2);

        _statusCard.Resize += (_, _) =>
        {
            var maxW = Math.Max(200, _statusCard.ClientSize.Width - _statusCard.Padding.Horizontal - MaterialTheme.Scale(8));
            _effectLabel.MaximumSize = new Size(maxW, 0);
            _stateLabel.MaximumSize = new Size(maxW, 0);
        };

        _statusCard.Controls.Add(layout);
        return _statusCard;
    }

    private Control BuildActions()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, MaterialTheme.Scale(10)),
        };

        _applyButton = MaterialTheme.MakeButton(Strings.Get("Apply"), ButtonVariant.Filled, _palette);
        _applyButton.Click += OnApply;
        row.Controls.Add(_applyButton);

        _restoreButton = MaterialTheme.MakeButton(Strings.Get("Restore"), ButtonVariant.Tonal, _palette);
        _restoreButton.Click += OnRestoreLatest;
        row.Controls.Add(_restoreButton);

        return row;
    }

    private Control BuildResult()
    {
        _resultPanel = MaterialTheme.MakeCard(_palette);
        _resultPanel.Dock = DockStyle.Fill;
        _resultPanel.AutoSize = true;
        _resultPanel.Margin = new Padding(0, 0, 0, MaterialTheme.Scale(6));
        _resultPanel.Padding = new Padding(MaterialTheme.Scale(14));
        _resultPanel.Visible = false;

        _resultLabel = MaterialTheme.MakeLabel("", LabelRole.Body, _palette);
        _resultLabel.AutoSize = true;
        _resultPanel.Resize += (_, _) =>
        {
            var maxW = Math.Max(200, _resultPanel.ClientSize.Width - _resultPanel.Padding.Horizontal - MaterialTheme.Scale(8));
            _resultLabel.MaximumSize = new Size(maxW, 0);
        };
        _resultPanel.Controls.Add(_resultLabel);

        return _resultPanel;
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
            ShowResult(
                string.IsNullOrWhiteSpace(path) ? Strings.Get("NoGamePath") : Strings.Get("InvalidFolder"),
                ResultKind.Error);
            RefreshAll();
            return;
        }

        RefreshAll();
    }

    private void OnApply(object? sender, EventArgs e)
    {
        var result = _state.Apply();
        if (result is null)
        {
            ShowResult(Strings.Get("NoGamePath"), ResultKind.Error);
            return;
        }

        if (!result.Ok)
        {
            ShowResult(string.Join(Environment.NewLine, result.Errors), ResultKind.Error);
            RefreshAll();
            return;
        }

        if (result.ChangedFiles.Count == 0)
        {
            ShowResult(Strings.Get("NothingToApply"), ResultKind.Neutral);
            RefreshAll();
            return;
        }

        var lines = new List<string> { Strings.Get("Applied") };
        lines.AddRange(result.ChangedFiles.Select(f => "  " + f));

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

        ShowResult(
            string.Join(Environment.NewLine, lines),
            result.Errors.Count > 0 ? ResultKind.Partial : ResultKind.Success);
        RefreshAll();
    }

    private void OnRestoreLatest(object? sender, EventArgs e)
    {
        var outcome = _state.RestoreLatest();
        if (outcome is null)
        {
            ShowResult(Strings.Get("BackupEmpty"), ResultKind.Neutral);
            return;
        }

        ShowResult(
            outcome.Ok
                ? Strings.Get("Restored") + Environment.NewLine + string.Join(Environment.NewLine, outcome.Restored)
                : Strings.Get("RestoreMissing") + Environment.NewLine + string.Join(Environment.NewLine, outcome.Missing),
            outcome.Ok ? ResultKind.Success : ResultKind.Error);
        RefreshAll();
    }

    // ---------------------------------------------------------------- resultado

    private enum ResultKind
    {
        Neutral,
        Success,
        Partial,
        Error,
    }

    private void ShowResult(string message, ResultKind kind)
    {
        var color = kind switch
        {
            ResultKind.Success => _palette.Accent,
            ResultKind.Partial => _palette.Error,
            ResultKind.Error => _palette.Error,
            _ => _palette.OnSurface,
        };

        _resultPanel.BackColor = kind == ResultKind.Neutral
            ? _palette.Surface
            : CyberpunkPalette.Mix(_palette.Surface, color, 0.14);

        _resultLabel.ForeColor = color;
        _resultLabel.Text = message;
        _resultPanel.Visible = true;
    }

    // ---------------------------------------------------------------- atualizacao

    private void RefreshAll()
    {
        RefreshOrientation();
        RefreshEffect();
        RefreshState();
        UpdateActionStates();
    }

    private void RefreshOrientation()
    {
        var valid = _state.Locator is not null && _state.Validator?.IsValidInstallation() == true;
        _orientationPanel.Visible = !valid;

        if (valid)
        {
            return;
        }

        _orientationLabel.Text = string.Join(
            Environment.NewLine,
            new[]
            {
                Strings.Get("FirstRunTitle"),
                string.Empty,
                Strings.Get("FirstRunSteam"),
                Strings.Get("FirstRunEpic"),
                Strings.Get("FirstRunGog"),
                string.Empty,
                Strings.Get("FirstRunSave"),
            });
    }

    private void RefreshEffect()
    {
        var lines = new List<string>
        {
            Strings.Get("BaseProfileEffect"),
            string.Empty,
            string.Format(CultureInfo.CurrentCulture, Strings.Get("SettingsToChange"), _state.CountPendingChanges()),
        };

        _effectLabel.Text = string.Join(Environment.NewLine, lines);
    }

    private void RefreshState()
    {
        if (_blockReason is not null)
        {
            _stateLabel.ForeColor = _palette.PrimaryText;
            _stateLabel.Text = _blockReason;
            return;
        }

        var state = _state.ReadAppliedState();
        var leftoverHudMod = _state.DetectLeftoverHudMod();
        var hudModSuffix = leftoverHudMod is not null
            ? Environment.NewLine + Strings.Get("LegacyHudModNotice")
            : "";

        if (state.Total == 0)
        {
            _stateLabel.ForeColor = _palette.OnSurfaceMuted;
            _stateLabel.Text = leftoverHudMod is not null ? Strings.Get("LegacyHudModNotice") : "";
            return;
        }

        if (state.Applied)
        {
            _stateLabel.ForeColor = _palette.Accent;
            _stateLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                Strings.Get("StateApplied"),
                state.AppliedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture) ?? "") + hudModSuffix;
            return;
        }

        _stateLabel.ForeColor = _palette.OnSurfaceMuted;

        if (state.AppliedAt is null)
        {
            _stateLabel.Text = Strings.Get("StateNeverApplied") + hudModSuffix;
            return;
        }

        _stateLabel.Text = string.Format(
            CultureInfo.CurrentCulture,
            Strings.Get("StateDiverging"),
            state.Diverging) + hudModSuffix;
    }

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
        else if (new GameRunningGuard().IsGameRunning())
        {
            reason = Strings.Get("GameRunning");
        }

        _blockReason = reason;

        var enabled = reason is null;
        _applyButton.Enabled = enabled;
        _applyButton.Text = Strings.Get("Apply").ToUpperInvariant();
        _restoreButton.Enabled = _state.Backups.List().Count > 0;

        RefreshState();
    }
}
