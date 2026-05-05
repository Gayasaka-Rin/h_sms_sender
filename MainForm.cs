using h_sms_sender.Models;
using h_sms_sender.Services;

namespace h_sms_sender;

public class MainForm : Form
{
    private AppConfig _config = null!;
    private SplitContainer _split = null!;
    private CheckedListBox _recipientList = null!;
    private TextBox _editor = null!;
    private Button _sendButton = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private MenuStrip _menu = null!;

    public MainForm()
    {
        Text = "편지 보내기";
        Width = 1000;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 500);

        BuildMenu();
        BuildBody();
        BuildStatusBar();

        Load += (_, _) => ReloadConfig();
    }

    private void BuildMenu()
    {
        _menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("파일(&F)");
        var newItem = new ToolStripMenuItem("새 편지(&N)", null, (_, _) => ClearEditor()) { ShortcutKeys = Keys.Control | Keys.N };
        var exitItem = new ToolStripMenuItem("종료(&X)", null, (_, _) => Close());
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] { newItem, new ToolStripSeparator(), exitItem });

        var settingsMenu = new ToolStripMenuItem("설정(&S)");
        var openSettings = new ToolStripMenuItem("설정 열기...", null, (_, _) => OpenSettings());
        settingsMenu.DropDownItems.Add(openSettings);

        var historyMenu = new ToolStripMenuItem("기록(&H)");
        var openHistory = new ToolStripMenuItem("발송 기록 보기...", null, (_, _) => OpenHistory()) { ShortcutKeys = Keys.Control | Keys.H };
        historyMenu.DropDownItems.Add(openHistory);

        _menu.Items.AddRange(new ToolStripItem[] { fileMenu, settingsMenu, historyMenu });
        MainMenuStrip = _menu;
        Controls.Add(_menu);
    }

    private void BuildBody()
    {
        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 160
        };

        var leftLabel = new Label
        {
            Text = "받는 사람 (체크해서 선택)",
            Dock = DockStyle.Top,
            Padding = new Padding(8, 8, 8, 4),
            Height = 28
        };
        _recipientList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            Font = new Font("맑은 고딕", 11f)
        };
        _split.Panel1.Controls.Add(_recipientList);
        _split.Panel1.Controls.Add(leftLabel);

        var rightContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        rightContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        _editor = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            AcceptsReturn = true,
            AcceptsTab = true,
            WordWrap = true,
            Font = new Font("맑은 고딕", 16f),
            BorderStyle = BorderStyle.None,
            Margin = new Padding(8)
        };

        var bottomPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        _sendButton = new Button
        {
            Text = "발송",
            Dock = DockStyle.Right,
            Width = 140,
            Height = 44,
            Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _sendButton.FlatAppearance.BorderSize = 0;
        _sendButton.Click += async (_, _) => await SendAsync();
        bottomPanel.Controls.Add(_sendButton);

        rightContainer.Controls.Add(_editor, 0, 0);
        rightContainer.Controls.Add(bottomPanel, 0, 1);

        _split.Panel2.Controls.Add(rightContainer);

        Controls.Add(_split);
        _split.BringToFront();
        HandleCreated += (_, _) => { try { _split.SplitterDistance = 220; } catch { } };
    }

    private void BuildStatusBar()
    {
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("준비");
        _statusStrip.Items.Add(_statusLabel);
        Controls.Add(_statusStrip);
    }

    private void ReloadConfig()
    {
        _config = Storage.LoadConfig();
        ApplyFont();
        RefreshRecipients();
        UpdateStatus();
        _ = WakeDeviceAsync();
    }

    private async Task WakeDeviceAsync()
    {
        if (string.IsNullOrEmpty(_config.DeviceId)) return;
        var prev = _statusLabel.Text;
        _statusLabel.Text = "휴대폰 깨우는 중...";
        try
        {
            var svc = new KdeConnectService(_config.KdeConnectCliPath);
            await svc.PingAsync(_config.DeviceId);
        }
        catch { }
        if (_statusLabel.Text == "휴대폰 깨우는 중...") _statusLabel.Text = prev;
    }

    private void ApplyFont()
    {
        var style = FontStyle.Regular;
        if (_config.Font.Bold) style |= FontStyle.Bold;
        if (_config.Font.Italic) style |= FontStyle.Italic;
        try
        {
            _editor.Font = new Font(_config.Font.Family, _config.Font.Size, style);
        }
        catch
        {
            _editor.Font = new Font("맑은 고딕", 16f);
        }
    }

    private void RefreshRecipients()
    {
        _recipientList.Items.Clear();
        foreach (var r in _config.Recipients)
            _recipientList.Items.Add(r, false);
    }

    private void UpdateStatus()
    {
        var device = string.IsNullOrEmpty(_config.DeviceName) ? "(기기 미설정)" : _config.DeviceName;
        var recipientCount = _config.Recipients.Count;
        _statusLabel.Text = $"기기: {device}   ·   등록된 수신자: {recipientCount}명";
    }

    private void ClearEditor()
    {
        if (_editor.Text.Length > 0)
        {
            var r = MessageBox.Show("작성 중인 내용을 비울까요?", "새 편지",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
        }
        _editor.Clear();
        for (int i = 0; i < _recipientList.Items.Count; i++)
            _recipientList.SetItemChecked(i, false);
        _editor.Focus();
    }

    private void OpenSettings()
    {
        using var f = new SettingsForm(_config);
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            Storage.SaveConfig(_config);
            ReloadConfig();
        }
    }

    private void OpenHistory()
    {
        using var f = new HistoryForm();
        f.ShowDialog(this);
    }

    private async Task SendAsync()
    {
        var body = _editor.Text;
        if (string.IsNullOrWhiteSpace(body))
        {
            MessageBox.Show("본문이 비어있어요.", "발송", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selected = new List<Recipient>();
        for (int i = 0; i < _recipientList.Items.Count; i++)
            if (_recipientList.GetItemChecked(i))
                selected.Add((Recipient)_recipientList.Items[i]!);

        if (selected.Count == 0)
        {
            MessageBox.Show("받는 사람을 한 명 이상 선택해주세요.", "발송", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrEmpty(_config.DeviceId))
        {
            MessageBox.Show("연결할 휴대폰이 설정되지 않았어요. 메뉴 > 설정에서 기기를 선택해주세요.",
                "발송", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var preview = body.Length > 100 ? body[..100] + "…" : body;
        var names = string.Join(", ", selected.Select(r => r.Name));
        var confirm = MessageBox.Show(
            $"다음 분들에게 발송할까요?\n\n받는 사람: {names}\n\n[본문 미리보기]\n{preview}",
            "발송 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _sendButton.Enabled = false;
        _statusLabel.Text = "발송 중...";

        var svc = new KdeConnectService(_config.KdeConnectCliPath);
        var results = new List<SendResult>();
        foreach (var r in selected)
        {
            _statusLabel.Text = $"{r.Name}에게 발송 중...";
            var res = await svc.SendSmsAsync(_config.DeviceId, r.Name, r.Phone, body);
            results.Add(res);
        }

        var failures = results.Where(r => !r.Success).ToList();
        var entry = new HistoryEntry
        {
            Timestamp = DateTime.Now,
            RecipientNames = selected.Select(r => r.Name).ToList(),
            RecipientPhones = selected.Select(r => r.Phone).ToList(),
            Body = body,
            Failures = failures.Select(f => $"{f.RecipientName}: {f.Message}").ToList()
        };
        Storage.AppendHistory(entry);

        _sendButton.Enabled = true;
        UpdateStatus();

        if (failures.Count == 0)
        {
            MessageBox.Show($"{selected.Count}명에게 모두 발송되었습니다.", "발송 완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            _editor.Clear();
            for (int i = 0; i < _recipientList.Items.Count; i++)
                _recipientList.SetItemChecked(i, false);
        }
        else
        {
            var ok = results.Count - failures.Count;
            var lines = string.Join("\n", failures.Select(f => $"  · {f.RecipientName}: {f.Message}"));
            MessageBox.Show(
                $"성공: {ok}명, 실패: {failures.Count}명\n\n실패 내역:\n{lines}",
                "발송 결과", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
