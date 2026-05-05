using h_sms_sender.Models;
using h_sms_sender.Services;

namespace h_sms_sender;

public class SettingsForm : Form
{
    private readonly AppConfig _config;
    private TextBox _cliPathBox = null!;
    private Button _browseCliBtn = null!;
    private ComboBox _deviceCombo = null!;
    private Button _refreshBtn = null!;
    private Label _fontPreview = null!;
    private Button _fontBtn = null!;
    private DataGridView _grid = null!;
    private List<KdeDevice> _devices = new();

    public SettingsForm(AppConfig config)
    {
        _config = config;
        Text = "설정";
        Width = 720;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(600, 500);
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildUi();
        LoadValues();
        Shown += async (_, _) => await RefreshDevicesAsync();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(12),
            RowCount = 6
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // CLI path
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // device
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // font
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // recipients label
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // grid
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // ok/cancel

        // CLI path
        var cliGroup = new GroupBox { Text = "KDE Connect CLI 경로", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var cliPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        cliPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cliPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        _cliPathBox = new TextBox { Dock = DockStyle.Fill };
        _browseCliBtn = new Button { Text = "찾아보기...", Dock = DockStyle.Fill };
        _browseCliBtn.Click += (_, _) => BrowseCli();
        cliPanel.Controls.Add(_cliPathBox, 0, 0);
        cliPanel.Controls.Add(_browseCliBtn, 1, 0);
        cliGroup.Controls.Add(cliPanel);

        // device
        var deviceGroup = new GroupBox { Text = "연결할 휴대폰", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var devicePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        devicePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        devicePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        _deviceCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _refreshBtn = new Button { Text = "새로고침", Dock = DockStyle.Fill };
        _refreshBtn.Click += async (_, _) => await RefreshDevicesAsync();
        devicePanel.Controls.Add(_deviceCombo, 0, 0);
        devicePanel.Controls.Add(_refreshBtn, 1, 0);
        deviceGroup.Controls.Add(devicePanel);

        // font
        var fontGroup = new GroupBox { Text = "편집창 폰트", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var fontPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        fontPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fontPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        _fontPreview = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Text = "샘플 텍스트 가나다 ABC" };
        _fontBtn = new Button { Text = "폰트 변경...", Dock = DockStyle.Fill };
        _fontBtn.Click += (_, _) => PickFont();
        fontPanel.Controls.Add(_fontPreview, 0, 0);
        fontPanel.Controls.Add(_fontBtn, 1, 0);
        fontGroup.Controls.Add(fontPanel);

        // recipients label
        var recipientsLabel = new Label { Text = "받는 사람 목록 (이름 / 전화번호)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };

        // recipients grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EditMode = DataGridViewEditMode.EditOnEnter
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "이름", FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "전화번호 (010xxxxxxxx)", FillWeight = 60 });

        // bottom buttons
        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        var cancelBtn = new Button { Text = "취소", Width = 90, Height = 32, DialogResult = DialogResult.Cancel };
        var okBtn = new Button { Text = "확인", Width = 90, Height = 32 };
        okBtn.Click += (_, _) => SaveAndClose();
        buttonPanel.Controls.Add(okBtn);
        buttonPanel.Controls.Add(cancelBtn);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;

        root.Controls.Add(cliGroup, 0, 0);
        root.Controls.Add(deviceGroup, 0, 1);
        root.Controls.Add(fontGroup, 0, 2);
        root.Controls.Add(recipientsLabel, 0, 3);
        root.Controls.Add(_grid, 0, 4);
        root.Controls.Add(buttonPanel, 0, 5);

        Controls.Add(root);
    }

    private void LoadValues()
    {
        _cliPathBox.Text = _config.KdeConnectCliPath;
        UpdateFontPreview();
        foreach (var r in _config.Recipients)
            _grid.Rows.Add(r.Name, r.Phone);
    }

    private void UpdateFontPreview()
    {
        var style = FontStyle.Regular;
        if (_config.Font.Bold) style |= FontStyle.Bold;
        if (_config.Font.Italic) style |= FontStyle.Italic;
        try
        {
            _fontPreview.Font = new Font(_config.Font.Family, _config.Font.Size, style);
            _fontPreview.Text = $"{_config.Font.Family} {_config.Font.Size}pt - 샘플 ABC 가나다";
        }
        catch
        {
            _fontPreview.Text = "(폰트 적용 실패)";
        }
    }

    private void PickFont()
    {
        using var dlg = new FontDialog { ShowEffects = false, AllowVerticalFonts = false };
        try
        {
            var style = FontStyle.Regular;
            if (_config.Font.Bold) style |= FontStyle.Bold;
            if (_config.Font.Italic) style |= FontStyle.Italic;
            dlg.Font = new Font(_config.Font.Family, _config.Font.Size, style);
        }
        catch { }

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.Font.Family = dlg.Font.FontFamily.Name;
            _config.Font.Size = dlg.Font.Size;
            _config.Font.Bold = dlg.Font.Bold;
            _config.Font.Italic = dlg.Font.Italic;
            UpdateFontPreview();
        }
    }

    private void BrowseCli()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "kdeconnect-cli.exe|kdeconnect-cli.exe|모든 파일|*.*",
            FileName = "kdeconnect-cli.exe"
        };
        try { dlg.InitialDirectory = Path.GetDirectoryName(_cliPathBox.Text) ?? ""; } catch { }
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _cliPathBox.Text = dlg.FileName;
        }
    }

    private async Task RefreshDevicesAsync()
    {
        _refreshBtn.Enabled = false;
        _deviceCombo.Items.Clear();
        _deviceCombo.Items.Add("(검색 중...)");
        _deviceCombo.SelectedIndex = 0;

        try
        {
            var svc = new KdeConnectService(_cliPathBox.Text);
            _devices = await svc.ListDevicesAsync();
        }
        catch
        {
            _devices = new List<KdeDevice>();
        }

        _deviceCombo.Items.Clear();
        if (_devices.Count == 0)
        {
            _deviceCombo.Items.Add("(검색된 기기 없음 - KDE Connect가 실행 중인지 확인)");
            _deviceCombo.SelectedIndex = 0;
        }
        else
        {
            foreach (var d in _devices)
                _deviceCombo.Items.Add(d);

            int idx = _devices.FindIndex(d => d.Id == _config.DeviceId);
            _deviceCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        _refreshBtn.Enabled = true;
    }

    private void SaveAndClose()
    {
        _config.KdeConnectCliPath = _cliPathBox.Text.Trim();

        if (_deviceCombo.SelectedItem is KdeDevice dev)
        {
            _config.DeviceId = dev.Id;
            _config.DeviceName = dev.Name;
        }

        _config.Recipients.Clear();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var name = (row.Cells["Name"].Value?.ToString() ?? "").Trim();
            var phone = (row.Cells["Phone"].Value?.ToString() ?? "").Trim();
            if (name.Length == 0 && phone.Length == 0) continue;
            _config.Recipients.Add(new Recipient { Name = name, Phone = phone });
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
