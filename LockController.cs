using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class LockController
{
	private readonly AppConfig _cfg;

	private readonly List<LockScreen> _screens = new List<LockScreen>();

	private InputGuard? _guard;

	private Timer? _monitorTimer;

	private Timer? _taskbarTimer;

	private Timer? _labelTimer;

	private Timer? _cooldownTimer;

	private Timer? _autoHideTimer;

	private Timer? _focusTimer;

	private LockScreen? _primaryScreen;

	private bool _stealthHidden;

	private char[] _labels = Array.Empty<char>();

	private readonly List<int> _typed = new List<int>();

	private int _cooldown;

	private int _failures;

	private bool _alerting;

	private readonly bool _preview;

	private bool _closing;

	public IReadOnlyList<char> Labels => _labels;

	public bool HasRecoveryCode => !string.IsNullOrEmpty(_cfg.RecoveryHash);

	public bool HasSecurityAnswer => !string.IsNullOrEmpty(_cfg.SecurityAnswerHash);

	public string? SecurityQuestion => _cfg.SecurityQuestion;

	public bool IsLocked { get; private set; }

	public bool IsPreview => _preview;

	public event Action? Unlocked;

	public event Action? SettingsRequested;

	public LockController(AppConfig cfg, bool preview = false)
	{
		_cfg = cfg;
		_preview = preview;
		if (!preview)
		{
			WarmUp();
		}
	}

	private void WarmUp()
	{
		try
		{
			Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
			using LockScreen lockScreen = new LockScreen(this, _cfg, screen, primary: true);
			lockScreen.CreateControl();
			using Bitmap bitmap = new Bitmap(Math.Max(1, lockScreen.Width), Math.Max(1, lockScreen.Height));
			lockScreen.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
		}
		catch (Exception ex)
		{
			ConfigStore.Log("lock warmup failed: " + ex.Message);
		}
	}

	public void Lock()
	{
		if (IsLocked)
		{
			return;
		}
		IsLocked = true;
		_failures = 0;
		_alerting = false;
		_stealthHidden = false;
		_primaryScreen = null;
		try
		{
			if (!_preview)
			{
				try
				{
					if (_cfg.HideTaskbar)
					{
						SystemGuard.HideTaskbar();
					}
				}
				catch (Exception ex)
				{
					ConfigStore.Log("taskbar hide skipped: " + ex.Message);
				}
				try
				{
					if (_cfg.DisableTaskManager || _cfg.BlockTaskSwitch)
					{
						SystemGuard.ApplyPolicies(_cfg.DisableTaskManager, _cfg.BlockTaskSwitch);
					}
				}
				catch (Exception ex2)
				{
					ConfigStore.Log("policy hardening skipped: " + ex2.Message);
				}
				try
				{
					_guard = new InputGuard(_cfg.BlockKeyboard);
					_guard.RevealRequested += RevealFromStealth;
					_guard.FocusRequested += ForceLockFocus;
					_guard.KeyPressed += HandleGuardKey;
					_guard.Start();
				}
				catch (Exception ex3)
				{
					ConfigStore.Log("input guard skipped: " + ex3.Message);
					_guard = null;
				}
			}
			Screen[] allScreens = Screen.AllScreens;
			Screen primary = Screen.PrimaryScreen ?? allScreens[0];
			LockScreen lockScreen = null;
			foreach (Screen item in allScreens.Where((Screen s) => s.DeviceName != primary.DeviceName))
			{
				ShowScreen(item, primary: false);
			}
			lockScreen = (_primaryScreen = ShowScreen(primary, primary: true));
			RotateLabels(announce: false);
			lockScreen?.Show();
			lockScreen?.ForceFocus();
			foreach (LockScreen screen in _screens)
			{
				if (screen != lockScreen)
				{
					screen.Show();
				}
			}
			ReassertTopMost();
			int num = 60;
			if (int.TryParse(Environment.GetEnvironmentVariable("AMPCLOCK_LABEL_SECONDS"), out var result) && result >= 2 && result <= 3600)
			{
				num = result;
			}
			_labelTimer = new Timer
			{
				Interval = num * 1000
			};
			_labelTimer.Tick += delegate
			{
				RotateLabels(announce: true);
			};
			_labelTimer.Start();
			if (!_preview)
			{
				_focusTimer = new Timer
				{
					Interval = 250
				};
				_focusTimer.Tick += delegate
				{
					MaintainLockFocus();
				};
				_focusTimer.Start();
			}
			if (!_preview && _cfg.AutoHideLockSeconds > 0)
			{
				int num2 = Math.Clamp(_cfg.AutoHideLockSeconds, 1, 3600);
				_autoHideTimer = new Timer
				{
					Interval = num2 * 1000
				};
				_autoHideTimer.Tick += delegate
				{
					_autoHideTimer?.Stop();
					_autoHideTimer?.Dispose();
					_autoHideTimer = null;
					EnterStealth();
				};
				_autoHideTimer.Start();
			}
			if (!_preview && _cfg.HideTaskbar)
			{
				_taskbarTimer = new Timer
				{
					Interval = 3000
				};
				_taskbarTimer.Tick += delegate
				{
					SystemGuard.HideTaskbar();
				};
				_taskbarTimer.Start();
			}
			if (!_preview && _cfg.MonitorOffEnabled && _cfg.MonitorOffMinutes > 0)
			{
				DateTime lockedAt = DateTime.UtcNow;
				_monitorTimer = new Timer
				{
					Interval = 15000
				};
				_monitorTimer.Tick += delegate
				{
					if (DateTime.UtcNow - lockedAt >= TimeSpan.FromMinutes(_cfg.MonitorOffMinutes))
					{
						SystemGuard.MonitorOff();
					}
				};
				_monitorTimer.Start();
			}
			ConfigStore.Log("locked");
		}
		catch (Exception ex4)
		{
			ConfigStore.Log("lock failed: " + ex4);
			Unlock();
		}
	}

	private LockScreen ShowScreen(Screen screen, bool primary)
	{
		LockScreen form = new LockScreen(this, _cfg, screen, primary);
		form.FormClosed += delegate
		{
			if (IsLocked && !_closing)
			{
				ConfigStore.Log("lock screen closed unexpectedly; restoring");
				form.Show();
			}
		};
		_screens.Add(form);
		return form;
	}

	private void RotateLabels(bool announce)
	{
		if (!IsLocked)
		{
			return;
		}
		int count = _cfg.ActiveGrid * _cfg.ActiveGrid;
		char[] array = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
		for (int num = array.Length - 1; num > 0; num--)
		{
			int num2 = Random.Shared.Next(num + 1);
			ref char reference = ref array[num];
			ref char reference2 = ref array[num2];
			char c = array[num2];
			char c2 = array[num];
			reference = c;
			reference2 = c2;
		}
		_labels = array.Take(count).ToArray();
		_typed.Clear();
		SetTypedCount(0);
		foreach (LockScreen screen in _screens)
		{
			screen.SetDotLabels(_labels);
		}
		if (announce && _cooldown == 0)
		{
			SetStatus("Letters changed — draw or type the letters on the dots", wrong: false);
		}
	}

	private void EnterStealth()
	{
		if (!IsLocked || _preview || _stealthHidden)
		{
			return;
		}
		_stealthHidden = true;
		_guard?.SetStealth(hidden: true);
		foreach (LockScreen screen in _screens)
		{
			screen.SetStealthHidden(hidden: true);
		}
		ConfigStore.Log("lock screen hidden; click or press a key to show it again");
	}

	private void RevealFromStealth()
	{
		if (!IsLocked || !_stealthHidden)
		{
			return;
		}
		_stealthHidden = false;
		_guard?.SetStealth(hidden: false);
		foreach (LockScreen screen in _screens)
		{
			screen.SetStealthHidden(hidden: false);
		}
		_primaryScreen?.ForceFocus();
		ReassertTopMost();
		ConfigStore.Log("lock screen revealed");
	}

	private void ForceLockFocus()
	{
		if (IsLocked && !_stealthHidden && !_preview)
		{
			_primaryScreen?.ForceFocus();
		}
	}

	private void HandleGuardKey(uint vk)
	{
		if (vk >= 65 && vk <= 90)
		{
			HandleLetter((char)vk);
			return;
		}
		switch (vk)
		{
		case 8u:
			HandleBackspace();
			break;
		case 13u:
			SubmitTyped();
			break;
		}
	}

	private void MaintainLockFocus()
	{
		if (IsLocked && !_preview)
		{
			ReassertTopMost();
			if (!_stealthHidden && !HasOtherVisibleForm() && !IsLockScreenForeground())
			{
				_primaryScreen?.ForceFocus();
			}
		}
	}

	private bool HasOtherVisibleForm()
	{
		foreach (Form openForm in Application.OpenForms)
		{
			if (openForm.Visible && !(openForm is LockScreen))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsLockScreenForeground()
	{
		nint foregroundWindow = Native.GetForegroundWindow();
		if (foregroundWindow == IntPtr.Zero)
		{
			return true;
		}
		foreach (LockScreen screen in _screens)
		{
			if (!screen.IsDisposed && screen.Handle == foregroundWindow)
			{
				return true;
			}
		}
		return false;
	}

	private void ReassertTopMost()
	{
		if (_stealthHidden)
		{
			return;
		}
		foreach (LockScreen screen in _screens)
		{
			if (!screen.IsDisposed)
			{
				Native.SetWindowPos(screen.Handle, Native.HWND_TOPMOST, 0, 0, 0, 0, 19u);
			}
		}
	}

	public void HandleLetter(char letter)
	{
		if (!IsLocked || _cooldown > 0)
		{
			return;
		}
		int num = Array.IndexOf(_labels, char.ToUpperInvariant(letter));
		if (num >= 0)
		{
			if (_typed.Count == 0)
			{
				ClearInputs();
			}
			_typed.Add(num);
			ShowTypedProgress();
		}
	}

	public void HandleBackspace()
	{
		if (IsLocked && _cooldown <= 0 && _typed.Count != 0)
		{
			_typed.RemoveAt(_typed.Count - 1);
			ShowTypedProgress();
		}
	}

	public void SubmitTyped()
	{
		if (IsLocked && _cooldown <= 0)
		{
			if (_typed.Count < 2)
			{
				SetStatus($"Type at least {2} letters, then press Enter", wrong: false);
			}
			else
			{
				string pattern = PatternHasher.Serialize(_typed);
				_typed.Clear();
				SetTypedCount(0);
				TryPattern(pattern);
			}
		}
	}

	private void ShowTypedProgress()
	{
		int count = _typed.Count;
		SetTypedCount(count);
		SetStatus((count == 0) ? "Draw a pattern, or type the letters on the dots and press Enter" : "Enter to confirm · Backspace to undo", wrong: false);
	}

	public void TryPattern(string pattern)
	{
		if (!IsLocked || _cooldown > 0)
		{
			return;
		}
		_typed.Clear();
		SetTypedCount(0);
		if (string.IsNullOrEmpty(_cfg.PatternHash))
		{
			SetStatus("No pattern configured. Use the recovery code.", wrong: true);
			ClearInputs();
			return;
		}
		if (_cfg.VerifyPattern(pattern))
		{
			Unlock();
			return;
		}
		_failures++;
		int value = Math.Max(0, _cfg.AlertAfterFailures - _failures);
		if (_cfg.AlertAfterFailures > 0 && _failures >= _cfg.AlertAfterFailures)
		{
			_alerting = true;
			PlayAlarm();
			SetStatus(_cfg.AlertText, wrong: true);
			StartCooldown(Math.Max(1, _cfg.AlertCooldownSeconds));
		}
		else
		{
			SetStatus($"Wrong pattern — {_failures} failed attempt{((_failures == 1) ? "" : "s")}, {value} before alert", wrong: true);
		}
		ClearInputs();
	}

	public bool VerifyRecoveryOrAnswer(string code, string answer)
	{
		bool num = !string.IsNullOrEmpty(_cfg.RecoveryHash) && _cfg.VerifyRecovery(code);
		bool flag = !string.IsNullOrEmpty(_cfg.SecurityAnswerHash) && _cfg.VerifySecurityAnswer(answer);
		return num || flag;
	}

	public void ShowRecovery(IWin32Window owner)
	{
		if (!IsLocked || _cooldown > 0)
		{
			return;
		}
		using RecoveryForm recoveryForm = new RecoveryForm(this);
		if (recoveryForm.ShowDialog(owner) == DialogResult.OK)
		{
			Unlock();
			this.SettingsRequested?.Invoke();
		}
		else if (recoveryForm.CodeRejected)
		{
			SetStatus("Recovery code or answer not accepted", wrong: true);
		}
		if (IsLocked)
		{
			_primaryScreen?.ForceFocus();
		}
	}

	private void StartCooldown(int seconds)
	{
		_cooldown = seconds;
		_cooldownTimer?.Stop();
		_cooldownTimer = new Timer
		{
			Interval = 1000
		};
		_cooldownTimer.Tick += delegate
		{
			_cooldown--;
			if (_cooldown <= 0)
			{
				_cooldownTimer?.Stop();
				_cooldownTimer?.Dispose();
				_cooldownTimer = null;
				SetInputEnabled(enabled: true);
				SetStatus("Draw a pattern, or type the letters on the dots and press Enter", wrong: false);
				_alerting = false;
			}
			else
			{
				SetStatus($"{(_alerting ? _cfg.AlertText : "Too many attempts")} — retry in {_cooldown}s", wrong: true);
			}
		};
		_cooldownTimer.Start();
		SetInputEnabled(enabled: false);
	}

	public void Unlock()
	{
		if (!IsLocked)
		{
			return;
		}
		_closing = true;
		_monitorTimer?.Stop();
		_monitorTimer?.Dispose();
		_monitorTimer = null;
		_taskbarTimer?.Stop();
		_taskbarTimer?.Dispose();
		_taskbarTimer = null;
		_labelTimer?.Stop();
		_labelTimer?.Dispose();
		_labelTimer = null;
		_cooldownTimer?.Stop();
		_cooldownTimer?.Dispose();
		_cooldownTimer = null;
		_autoHideTimer?.Stop();
		_autoHideTimer?.Dispose();
		_autoHideTimer = null;
		_focusTimer?.Stop();
		_focusTimer?.Dispose();
		_focusTimer = null;
		_cooldown = 0;
		_stealthHidden = false;
		_primaryScreen = null;
		_guard?.Stop();
		_guard?.Dispose();
		_guard = null;
		if (!_preview)
		{
			if (_cfg.HideTaskbar)
			{
				SystemGuard.RestoreTaskbar();
			}
			SystemGuard.RestorePolicies(null);
			SystemGuard.ClearJournal();
		}
		foreach (LockScreen item in _screens.ToList())
		{
			item.Hide();
			item.Dispose();
		}
		_screens.Clear();
		IsLocked = false;
		_closing = false;
		ConfigStore.Log("unlocked");
		this.Unlocked?.Invoke();
	}

	private void SetStatus(string text, bool wrong)
	{
		foreach (LockScreen screen in _screens)
		{
			screen.ShowAttempt(text, wrong);
		}
	}

	private void SetInputEnabled(bool enabled)
	{
		foreach (LockScreen screen in _screens)
		{
			screen.SetInputEnabled(enabled);
		}
	}

	private void SetTypedCount(int count)
	{
		foreach (LockScreen screen in _screens)
		{
			screen.SetTypedCount(count);
		}
	}

	private void ClearInputs()
	{
		foreach (LockScreen screen in _screens)
		{
			screen.ClearPad();
		}
	}

	private void PlayAlarm()
	{
		if (!_cfg.AlertSound)
		{
			return;
		}
		try
		{
			string text = Path.Combine(ConfigStore.RootDir, "alarm.wav");
			if (File.Exists(text))
			{
				using (SoundPlayer soundPlayer = new SoundPlayer(text))
				{
					soundPlayer.Play();
					return;
				}
			}
			SystemSounds.Hand.Play();
		}
		catch (Exception ex)
		{
			ConfigStore.Log("alarm failed: " + ex.Message);
		}
	}
}
