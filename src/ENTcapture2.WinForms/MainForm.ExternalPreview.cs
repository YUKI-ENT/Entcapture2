using ENTcapture2.Core.Models;
using ENTcapture2.WinForms.Ui;

namespace ENTcapture2.WinForms;

public partial class MainForm
{
    private readonly CheckBox _externalPreviewToggle = new();
    private readonly System.Windows.Forms.Timer _externalPreviewTimer = new() { Interval = 1000 };
    private Guid? _externalPlaybackPresetId;

    private void InitializeExternalPreview()
    {
        _externalPreviewToggle.Appearance = Appearance.Button;
        _externalPreviewToggle.Text = "拡張モニター表示 OFF";
        _externalPreviewToggle.TextAlign = ContentAlignment.MiddleCenter;
        _externalPreviewToggle.AutoSize = true;
        _externalPreviewToggle.MinimumSize = ScaleDpi(new Size(150, 24));
        _externalPreviewToggle.Padding = ScaleDpi(new Padding(8, 0, 8, 0));
        _externalPreviewToggle.Margin = ScaleDpi(new Padding(24, 1, 0, 0));
        _externalPreviewToggle.FlatStyle = FlatStyle.Flat;
        _externalPreviewToggle.FlatAppearance.BorderSize = 0;
        _externalPreviewToggle.BackColor = Theme.SurfaceRaised;
        _externalPreviewToggle.ForeColor = Theme.Text;
        _externalPreviewToggle.Cursor = Cursors.Hand;
        _sourceHeaderPanel.Controls.Add(_externalPreviewToggle);
        _externalPreviewToggle.CheckedChanged += (_, _) =>
        {
            _externalPreviewToggle.Text = _externalPreviewToggle.Checked
                ? "拡張モニター表示 ON" : "拡張モニター表示 OFF";
            _externalPreviewToggle.BackColor = _externalPreviewToggle.Checked
                ? Theme.Accent : Theme.SurfaceRaised;
            if (_externalPreviewToggle.Checked)
            {
                if (_previewZoomForm is { IsDisposed: false, IsExternalDisplay: false })
                {
                    SaveZoomWindowBounds(_previewZoomForm);
                }
                ApplyExternalPreview();
                _externalPreviewTimer.Start();
            }
            else
            {
                _externalPreviewTimer.Stop();
                if (_previewZoomForm is { IsDisposed: false, IsExternalDisplay: true })
                {
                    _previewZoomForm.Hide();
                }
            }
        };
        _externalPreviewTimer.Tick += (_, _) => ApplyExternalPreview();
        Disposed += (_, _) => _externalPreviewTimer.Dispose();
    }

    private PreviewZoomForm EnsurePreviewZoomForm()
    {
        if (_previewZoomForm is { IsDisposed: false }) return _previewZoomForm;
        var form = new PreviewZoomForm { Icon = Icon, TopMost = ShouldUsePreviewTopMost() };
        _previewZoomForm = form;
        form.PlaybackSeekRequested += PreviewZoomForm_PlaybackSeekRequested;
        form.PlaybackToggleRequested += PreviewZoomForm_PlaybackToggleRequested;
        form.PlaybackStepRequested += PreviewZoomForm_PlaybackStepRequested;
        form.FormClosed += async (_, _) =>
        {
            SaveZoomWindowBounds(form);
            if (ReferenceEquals(_previewZoomForm, form)) _previewZoomForm = null;
            if (form.IsExternalDisplay) _externalPreviewToggle.Checked = false;
            if (!_isClosing)
            {
                try
                {
                    await _settingsStore.SaveAsync(_settings);
                }
                catch (Exception exception)
                {
                    ShowError("プレビュー位置の保存に失敗しました。", exception);
                }
            }
        };
        return form;
    }

    private ExternalPreviewSettings? GetExternalPreviewSettings()
    {
        // Use current display preferences, even when playback metadata contains
        // an older snapshot of the capture preset.
        CapturePreset? preset = _playbackService.IsOpen && _externalPlaybackPresetId is { } playbackId
            ? _settings.Presets.FirstOrDefault(item => item.Id == playbackId) ?? _selectedPreset
            : _selectedPreset;
        return preset?.ExternalPreview;
    }

    private void ApplyExternalPreview()
    {
        if (_isClosing || !_externalPreviewToggle.Checked) return;
        ExternalPreviewSettings? settings = GetExternalPreviewSettings();
        Screen mainScreen = Screen.FromControl(this);
        Screen? screen = settings is null ? null :
            string.IsNullOrEmpty(settings.MonitorDeviceName)
                ? Screen.AllScreens.FirstOrDefault(item => item.DeviceName != mainScreen.DeviceName)
                : Screen.AllScreens.FirstOrDefault(item => item.DeviceName == settings.MonitorDeviceName);
        if (settings is not { Enabled: true } || screen is null)
        {
            if (_previewZoomForm is { IsDisposed: false }) _previewZoomForm.Hide();
            _externalPreviewToggle.Text = settings is not { Enabled: true }
                ? "拡張表示 ON（設定無効）" : "拡張表示 ON（モニター待ち）";
            return;
        }

        _externalPreviewToggle.Text = "拡張モニター表示 ON";
        PreviewZoomForm form = EnsurePreviewZoomForm();
        bool wasExternal = form.IsExternalDisplay;
        bool wasVisible = form.Visible;
        form.ConfigureExternalDisplay(settings,
            ExternalPreviewLayout.GetWindowBounds(screen.Bounds, settings));
        if (!wasExternal || !wasVisible)
        {
            if (_displayedImage is { } image) form.SetImage(image);
            else form.ClearImage();
        }
        form.TopMost = ShouldUsePreviewTopMost();
        if (!form.Visible) form.Show(this);
        // Show can apply DPI scaling; enforce the specified screen-pixel bounds.
        form.ConfigureExternalDisplay(settings,
            ExternalPreviewLayout.GetWindowBounds(screen.Bounds, settings));
    }

}
