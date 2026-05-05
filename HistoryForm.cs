using h_sms_sender.Models;
using h_sms_sender.Services;

namespace h_sms_sender;

public class HistoryForm : Form
{
    private ListBox _list = null!;
    private TextBox _preview = null!;
    private Label _meta = null!;
    private List<HistoryEntry> _entries = new();

    public HistoryForm()
    {
        Text = "발송 기록";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(600, 400);

        BuildUi();
        LoadEntries();
    }

    private void BuildUi()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1
        };
        HandleCreated += (_, _) => { try { split.SplitterDistance = 320; } catch { } };

        _list = new ListBox { Dock = DockStyle.Fill, Font = new Font("맑은 고딕", 10f) };
        _list.SelectedIndexChanged += (_, _) => UpdatePreview();
        split.Panel1.Controls.Add(_list);

        var rightContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        rightContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        rightContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _meta = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold)
        };

        _preview = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Font = new Font("맑은 고딕", 12f),
            BorderStyle = BorderStyle.FixedSingle
        };

        rightContainer.Controls.Add(_meta, 0, 0);
        rightContainer.Controls.Add(_preview, 0, 1);
        split.Panel2.Controls.Add(rightContainer);

        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(8) };
        var closeBtn = new Button { Text = "닫기", Width = 90, Dock = DockStyle.Right, DialogResult = DialogResult.Cancel };
        var deleteBtn = new Button { Text = "선택 삭제", Width = 100, Dock = DockStyle.Left };
        deleteBtn.Click += (_, _) => DeleteSelected();
        bottomPanel.Controls.Add(closeBtn);
        bottomPanel.Controls.Add(deleteBtn);
        CancelButton = closeBtn;

        Controls.Add(split);
        Controls.Add(bottomPanel);
    }

    private void LoadEntries()
    {
        _entries = Storage.LoadHistory();
        _list.Items.Clear();
        foreach (var e in _entries)
            _list.Items.Add(e.DisplayLabel);
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        else
        {
            _meta.Text = "기록이 없습니다.";
            _preview.Text = "";
        }
    }

    private void UpdatePreview()
    {
        var idx = _list.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count) return;
        var e = _entries[idx];
        var names = string.Join(", ", e.RecipientNames);
        var failNote = e.Failures.Count > 0 ? $"  (실패 {e.Failures.Count}건)" : "";
        _meta.Text = $"{e.Timestamp:yyyy-MM-dd HH:mm:ss}   →   {names}{failNote}";
        _preview.Text = e.Body;
        if (e.Failures.Count > 0)
        {
            _preview.Text += "\r\n\r\n--- 실패 내역 ---\r\n" + string.Join("\r\n", e.Failures);
        }
    }

    private void DeleteSelected()
    {
        var idx = _list.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count) return;

        var r = MessageBox.Show("선택된 기록을 삭제할까요?", "기록 삭제",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r != DialogResult.Yes) return;

        _entries.RemoveAt(idx);
        Storage.SaveHistory(_entries);
        LoadEntries();
    }
}
