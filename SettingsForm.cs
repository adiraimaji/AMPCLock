using System;
using System.Drawing;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class SettingsForm : Form
{
	private readonly AppConfig _cfg;

	private readonly Label _patternState = new Label();

	private readonly Label _recoveryState = new Label();

	private readonly NumericUpDown _autoLock = new NumericUpDown();

	private readonly NumericUpDown _alertAfter = new NumericUpDown();

	private readonly NumericUpDown _cooldown = new NumericUpDown();

	private readonly NumericUpDown _monitorMinutes = new NumericUpDown();

	private readonly NumericUpDown _grid = new NumericUpDown();

	private readonly TextBox _alertText = new TextBox();

	private readonly TextBox _imagePath = new TextBox();

	private readonly ComboBox _imageMode = new ComboBox();

	private readonly CheckBox _sound = new CheckBox();

	private readonly CheckBox _hideTaskbar = new CheckBox();

	private readonly CheckBox _disableTaskManager = new CheckBox();

	private readonly CheckBox _blockKeyboard = new CheckBox();

	private readonly CheckBox _blockTaskSwitch = new CheckBox();

	private readonly CheckBox _lockOnStartup = new CheckBox();

	private readonly CheckBox _startWithWindows = new CheckBox();

	private readonly CheckBox _monitorOff = new CheckBox();

	private readonly CheckBox _transparentLock = new CheckBox();

	private readonly NumericUpDown _autoHideSeconds = new NumericUpDown();

	private readonly TextBox _securityQuestion = new TextBox();

	private readonly TextBox _securityAnswer = new TextBox();

	private readonly bool _skipIdentityCheck;

	private bool _identityVerified;

	private string _color;

	public SettingsForm(AppConfig cfg, bool skipIdentityCheck = false)
	{
		_cfg = cfg;
		_skipIdentityCheck = skipIdentityCheck;
		_color = cfg.BackgroundColor;
		Text = "AM PC Lock — Settings";
		base.StartPosition = FormStartPosition.CenterScreen;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.ClientSize = new Size(580, 900);
		BackColor = Color.FromArgb(30, 33, 44);
		ForeColor = Color.Gainsboro;
		Font = new Font("Segoe UI", 9f);
		int y = 12;
		Panel panel = Group("Unlock pattern", ref y, 120);
		_patternState.Text = (string.IsNullOrEmpty(cfg.PatternHash) ? "No pattern set — press “Set pattern”." : $"Pattern set ({cfg.ActiveGrid}×{cfg.ActiveGrid} dots).");
		_patternState.SetBounds(20, 24, 320, 20);
		panel.Controls.Add(_patternState);
		_recoveryState.Text = (string.IsNullOrEmpty(cfg.RecoveryHash) ? "No recovery code yet." : "Recovery code is set.");
		_recoveryState.ForeColor = Color.FromArgb(150, 165, 195);
		_recoveryState.SetBounds(20, 62, 180, 20);
		panel.Controls.Add(_recoveryState);
		Button button = new Button
		{
			Text = "Set pattern…",
			Bounds = new Rectangle(360, 20, 176, 30)
		};
		button.Click += delegate
		{
			EnrollPattern();
		};
		panel.Controls.Add(button);
		Button button2 = new Button
		{
			Text = "New recovery code…",
			Bounds = new Rectangle(360, 56, 176, 28)
		};
		button2.Click += delegate
		{
			GenerateRecovery();
		};
		panel.Controls.Add(button2);
		Button resetButton = new Button
		{
			Text = "Reset password data…",
			Bounds = new Rectangle(360, 88, 176, 26),
			ForeColor = Color.FromArgb(255, 150, 150)
		};
		resetButton.Click += delegate
		{
			ResetPasswordData();
		};
		panel.Controls.Add(resetButton);
		Label value = new Label
		{
			Text = "Grid:",
			Bounds = new Rectangle(20, 94, 40, 20)
		};
		panel.Controls.Add(value);
		_grid.SetBounds(60, 90, 60, 24);
		_grid.Minimum = 3m;
		_grid.Maximum = 5m;
		_grid.Value = Math.Clamp(cfg.GridSize, 3, 5);
		panel.Controls.Add(_grid);
		panel.Controls.Add(LabelAt("Used when you next set a pattern.", 130, 94, 220));
		Panel panel2 = Group("Behaviour", ref y, 132);
		panel2.Controls.Add(LabelAt("Autolock after (minutes, 0 = off):", 20, 24, 240));
		_autoLock.SetBounds(270, 21, 60, 24);
		_autoLock.Maximum = 120m;
		_autoLock.Value = Math.Clamp(cfg.AutoLockMinutes, 0, 120);
		panel2.Controls.Add(_autoLock);
		panel2.Controls.Add(LabelAt("Hotkeys: Ctrl+Alt+L / Win+Ctrl+A lock  ·  Win+Ctrl+G settings", 20, 52, 520));
		panel2.Controls.Add(LabelAt("Win+Ctrl combos fall back to Ctrl+Alt+A / Ctrl+Alt+G when already taken.", 20, 72, 520));
		_lockOnStartup.Text = "Lock the workstation when AM PC Lock starts";
		_lockOnStartup.SetBounds(20, 96, 400, 22);
		_lockOnStartup.Checked = cfg.LockOnStartup;
		panel2.Controls.Add(_lockOnStartup);
		_startWithWindows.Text = "Start AM PC Lock with Windows";
		_startWithWindows.SetBounds(20, 118, 400, 22);
		_startWithWindows.Checked = SystemGuard.IsAutoStartEnabled();
		panel2.Controls.Add(_startWithWindows);
		Panel panel3 = Group("While locked", ref y, 152);
		_hideTaskbar.Text = "Hide the taskbar";
		_hideTaskbar.SetBounds(20, 24, 240, 22);
		_hideTaskbar.Checked = cfg.HideTaskbar;
		panel3.Controls.Add(_hideTaskbar);
		_disableTaskManager.Text = "Disable Task Manager and fast user switching";
		_disableTaskManager.SetBounds(20, 48, 400, 22);
		_disableTaskManager.Checked = cfg.DisableTaskManager;
		panel3.Controls.Add(_disableTaskManager);
		_blockTaskSwitch.Text = "Block logoff/shutdown shell commands";
		_blockTaskSwitch.SetBounds(20, 72, 400, 22);
		_blockTaskSwitch.Checked = cfg.BlockTaskSwitch;
		panel3.Controls.Add(_blockTaskSwitch);
		_blockKeyboard.Text = "Block keyboard input (Ctrl+Alt+Del always works)";
		_blockKeyboard.SetBounds(20, 96, 440, 22);
		_blockKeyboard.Checked = cfg.BlockKeyboard;
		panel3.Controls.Add(_blockKeyboard);
		_monitorOff.Text = "Turn the monitor off after";
		_monitorOff.SetBounds(20, 120, 200, 22);
		_monitorOff.Checked = cfg.MonitorOffEnabled;
		panel3.Controls.Add(_monitorOff);
		_monitorMinutes.SetBounds(224, 118, 60, 24);
		_monitorMinutes.Minimum = 1m;
		_monitorMinutes.Maximum = 120m;
		_monitorMinutes.Value = Math.Clamp(cfg.MonitorOffMinutes, 1, 120);
		panel3.Controls.Add(_monitorMinutes);
		panel3.Controls.Add(LabelAt("minutes", 290, 122, 60));
		Panel panel4 = Group("Wrong-pattern alert", ref y, 112);
		panel4.Controls.Add(LabelAt("Alert after failures:", 20, 24, 150));
		_alertAfter.SetBounds(180, 21, 60, 24);
		_alertAfter.Minimum = 1m;
		_alertAfter.Maximum = 50m;
		_alertAfter.Value = Math.Clamp(cfg.AlertAfterFailures, 1, 50);
		panel4.Controls.Add(_alertAfter);
		panel4.Controls.Add(LabelAt("Cooldown (seconds):", 270, 24, 150));
		_cooldown.SetBounds(430, 21, 60, 24);
		_cooldown.Maximum = 300m;
		_cooldown.Value = Math.Clamp(cfg.AlertCooldownSeconds, 0, 300);
		panel4.Controls.Add(_cooldown);
		panel4.Controls.Add(LabelAt("Alert text:", 20, 58, 70));
		_alertText.SetBounds(95, 55, 400, 24);
		_alertText.Text = cfg.AlertText;
		panel4.Controls.Add(_alertText);
		_sound.Text = "Play alarm sound";
		_sound.SetBounds(20, 84, 200, 22);
		_sound.Checked = cfg.AlertSound;
		panel4.Controls.Add(_sound);
		Panel panel5 = Group("Security question (shown on the lock screen)", ref y, 100);
		panel5.Controls.Add(LabelAt("Question:", 20, 24, 70));
		_securityQuestion.SetBounds(100, 22, 436, 24);
		_securityQuestion.Text = cfg.SecurityQuestion ?? "";
		panel5.Controls.Add(_securityQuestion);
		panel5.Controls.Add(LabelAt("Answer:", 20, 56, 70));
		_securityAnswer.SetBounds(100, 54, 436, 24);
		_securityAnswer.UseSystemPasswordChar = true;
		panel5.Controls.Add(_securityAnswer);
		panel5.Controls.Add(LabelAt(string.IsNullOrEmpty(cfg.SecurityAnswerHash) ? "Set an answer so you can unlock without the pattern or recovery code." : "Answer already set — type a new one to replace it, or leave blank to keep it.", 20, 78, 520));
		Panel panel6 = Group("Lock screen background and visibility", ref y, 156);
		_imagePath.SetBounds(20, 22, 340, 24);
		_imagePath.Text = cfg.BackgroundImage ?? "";
		panel6.Controls.Add(_imagePath);
		Button button3 = new Button
		{
			Text = "Image…",
			Bounds = new Rectangle(370, 21, 70, 26)
		};
		button3.Click += delegate
		{
			BrowseImage();
		};
		panel6.Controls.Add(button3);
		Button button4 = new Button
		{
			Text = "Clear",
			Bounds = new Rectangle(448, 21, 62, 26)
		};
		button4.Click += delegate
		{
			_imagePath.Text = "";
		};
		panel6.Controls.Add(button4);
		Button colorButton = new Button
		{
			Text = "Backdrop colour…",
			Bounds = new Rectangle(20, 58, 140, 26)
		};
		colorButton.Click += delegate
		{
			PickColor(colorButton);
		};
		panel6.Controls.Add(colorButton);
		_imageMode.SetBounds(176, 59, 120, 24);
		_imageMode.DropDownStyle = ComboBoxStyle.DropDownList;
		_imageMode.Items.AddRange(new object[4] { "fill", "fit", "stretch", "center" });
		_imageMode.SelectedItem = cfg.BackgroundModeNormalized;
		panel6.Controls.Add(_imageMode);
		panel6.Controls.Add(LabelAt("Image scaling", 304, 62, 120));
		_transparentLock.Text = "Transparent background (see the desktop behind the lock screen)";
		_transparentLock.SetBounds(20, 90, 520, 22);
		_transparentLock.Checked = cfg.TransparentLockBackground;
		panel6.Controls.Add(_transparentLock);
		panel6.Controls.Add(LabelAt("Auto-hide the lock screen after", 20, 120, 180));
		_autoHideSeconds.SetBounds(210, 117, 70, 24);
		_autoHideSeconds.Minimum = 0m;
		_autoHideSeconds.Maximum = 3600m;
		_autoHideSeconds.Value = Math.Clamp(cfg.AutoHideLockSeconds, 0, 3600);
		panel6.Controls.Add(_autoHideSeconds);
		panel6.Controls.Add(LabelAt("seconds (0 = never) — click/key to show", 290, 121, 260));
		Button button5 = new Button
		{
			Text = "Save",
			Bounds = new Rectangle(base.ClientSize.Width - 190, base.ClientSize.Height - 44, 80, 30)
		};
		button5.Click += delegate
		{
			SaveAndClose();
		};
		base.Controls.Add(button5);
		Button button6 = new Button
		{
			Text = "Cancel",
			Bounds = new Rectangle(base.ClientSize.Width - 96, base.ClientSize.Height - 44, 80, 30),
			DialogResult = DialogResult.Cancel
		};
		base.Controls.Add(button6);
		base.CancelButton = button6;
		base.AcceptButton = button5;
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		if (!EnsureIdentity())
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
			return;
		}
		if (string.IsNullOrEmpty(_cfg.PatternHash))
		{
			BeginInvoke(EnrollPattern);
		}
	}

	private Panel Group(string title, ref int y, int height)
	{
		int num = y;
		Panel panel = new Panel
		{
			Bounds = new Rectangle(12, num, base.ClientSize.Width - 24, height),
			BackColor = Color.FromArgb(37, 41, 54)
		};
		Label value = new Label
		{
			Text = title,
			Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
			ForeColor = Color.FromArgb(120, 180, 255),
			Bounds = new Rectangle(10, 2, 300, 20)
		};
		panel.Controls.Add(value);
		base.Controls.Add(panel);
		y = num + height + 8;
		return panel;
	}

	private static Label LabelAt(string text, int x, int y, int width)
	{
		return new Label
		{
			Text = text,
			Bounds = new Rectangle(x, y, width, 20)
		};
	}

	private bool EnsureIdentity()
	{
		if (_identityVerified || string.IsNullOrEmpty(_cfg.PatternHash))
		{
			return true;
		}
		if (_skipIdentityCheck)
		{
			_identityVerified = true;
			return true;
		}
		using VerifyIdentityForm verifyIdentityForm = new VerifyIdentityForm(_cfg);
		if (verifyIdentityForm.ShowDialog(this) != DialogResult.OK)
		{
			return false;
		}
		_identityVerified = true;
		return true;
	}

	private void EnrollPattern()
	{
		if (!EnsureIdentity())
		{
			return;
		}
		using PatternEnrollForm patternEnrollForm = new PatternEnrollForm(Math.Clamp((int)_grid.Value, 3, 5));
		if (patternEnrollForm.ShowDialog(this) == DialogResult.OK)
		{
			_cfg.GridSize = Math.Clamp((int)_grid.Value, 3, 5);
			_cfg.PatternGridSize = _cfg.GridSize;
			_cfg.SetPattern(patternEnrollForm.Pattern);
			_patternState.Text = $"Pattern set ({_cfg.ActiveGrid}×{_cfg.ActiveGrid} dots).";
			if (string.IsNullOrEmpty(_cfg.RecoveryHash))
			{
				GenerateRecoveryCore();
			}
		}
	}

	private void GenerateRecovery()
	{
		if (EnsureIdentity())
		{
			GenerateRecoveryCore();
		}
	}

	private void GenerateRecoveryCore()
	{
		string code = PatternHasher.NewRecoveryCode();
		_cfg.SetRecovery(code);
		_recoveryState.Text = "Recovery code is set.";
		using RecoveryCodeForm recoveryCodeForm = new RecoveryCodeForm(code);
		recoveryCodeForm.ShowDialog(this);
	}

	private void BrowseImage()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Choose a lock screen background",
			Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
			CheckFileExists = true
		};
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			_imagePath.Text = openFileDialog.FileName;
		}
	}

	private void PickColor(Button button)
	{
		using ColorDialog colorDialog = new ColorDialog
		{
			Color = ColorTranslator.FromHtml(_color),
			FullOpen = true
		};
		if (colorDialog.ShowDialog(this) == DialogResult.OK)
		{
			_color = $"#{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}";
			button.Text = "Colour " + _color;
		}
	}

	private void ResetPasswordData()
	{
		if (!EnsureIdentity())
		{
			return;
		}
		if (MessageBox.Show(this, "This removes the unlock pattern, recovery code and security answer, and turns off lock-on-startup.\r\n\r\nAM PC Lock will return to first-run setup. Continue?", "Reset stored password data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		string? patternHash = _cfg.PatternHash;
		string? patternSalt = _cfg.PatternSalt;
		int patternGridSize = _cfg.PatternGridSize;
		string? recoveryHash = _cfg.RecoveryHash;
		string? recoverySalt = _cfg.RecoverySalt;
		string? securityQuestion = _cfg.SecurityQuestion;
		string? securityAnswerHash = _cfg.SecurityAnswerHash;
		string? securityAnswerSalt = _cfg.SecurityAnswerSalt;
		bool lockOnStartup = _cfg.LockOnStartup;
		_cfg.ResetCredentials();
		_cfg.LockOnStartup = false;
		try
		{
			ConfigStore.Save(_cfg);
		}
		catch (Exception ex)
		{
			_cfg.PatternHash = patternHash;
			_cfg.PatternSalt = patternSalt;
			_cfg.PatternGridSize = patternGridSize;
			_cfg.RecoveryHash = recoveryHash;
			_cfg.RecoverySalt = recoverySalt;
			_cfg.SecurityQuestion = securityQuestion;
			_cfg.SecurityAnswerHash = securityAnswerHash;
			_cfg.SecurityAnswerSalt = securityAnswerSalt;
			_cfg.LockOnStartup = lockOnStartup;
			MessageBox.Show(this, "Password data could not be reset: " + ex.Message, "AM PC Lock", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		try
		{
			SystemGuard.SetAutoStart(_cfg.StartWithWindows, _cfg.LockOnStartup);
		}
		catch (Exception ex)
		{
			ConfigStore.Log("auto-start update after credential reset failed: " + ex.Message);
		}
		ConfigStore.Log("stored password data reset from settings");
		MessageBox.Show(this, "Stored password data has been reset.\r\n\r\nAutolock is paused and lock-on-startup is off until you create a new unlock pattern. Open Settings again to set one.", "AM PC Lock", MessageBoxButtons.OK, MessageBoxIcon.Information);
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void SaveAndClose()
	{		if (string.IsNullOrEmpty(_cfg.PatternHash))
		{
			MessageBox.Show(this, "Set an unlock pattern before saving.", "AM PC Lock", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		_cfg.GridSize = Math.Clamp((int)_grid.Value, 3, 5);
		_cfg.AutoLockMinutes = (int)_autoLock.Value;
		_cfg.AlertAfterFailures = (int)_alertAfter.Value;
		_cfg.AlertCooldownSeconds = (int)_cooldown.Value;
		_cfg.AlertText = (string.IsNullOrWhiteSpace(_alertText.Text) ? "Do not touch my computer!" : _alertText.Text.Trim());
		_cfg.AlertSound = _sound.Checked;
		_cfg.HideTaskbar = _hideTaskbar.Checked;
		_cfg.DisableTaskManager = _disableTaskManager.Checked;
		_cfg.BlockTaskSwitch = _blockTaskSwitch.Checked;
		_cfg.BlockKeyboard = _blockKeyboard.Checked;
		_cfg.MonitorOffEnabled = _monitorOff.Checked;
		_cfg.MonitorOffMinutes = (int)_monitorMinutes.Value;
		_cfg.LockOnStartup = _lockOnStartup.Checked;
		_cfg.StartWithWindows = _startWithWindows.Checked;
		_cfg.BackgroundImage = (string.IsNullOrWhiteSpace(_imagePath.Text) ? null : _imagePath.Text.Trim());
		_cfg.BackgroundMode = (_imageMode.SelectedItem as string) ?? "fill";
		_cfg.BackgroundColor = _color;
		_cfg.TransparentLockBackground = _transparentLock.Checked;
		_cfg.AutoHideLockSeconds = (int)_autoHideSeconds.Value;
		string text = _securityQuestion.Text.Trim();
		string text2 = _securityAnswer.Text.Trim();
		if (text2.Length > 0)
		{
			if (text.Length == 0)
			{
				MessageBox.Show(this, "Enter a security question for the answer.", "AM PC Lock", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			_cfg.SecurityQuestion = text;
			_cfg.SetSecurityAnswer(text2);
		}
		else if (text.Length > 0)
		{
			_cfg.SecurityQuestion = text;
		}
		try
		{
			ConfigStore.Save(_cfg);
			SystemGuard.SetAutoStart(_cfg.StartWithWindows, _cfg.LockOnStartup);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, "Saving settings failed: " + ex.Message, "AM PC Lock", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		base.DialogResult = DialogResult.OK;
		Close();
	}
}
