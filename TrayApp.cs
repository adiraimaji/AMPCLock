using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class TrayApp : ApplicationContext
{
	private readonly AppConfig _cfg;

	private readonly LockController _lock;

	private readonly NotifyIcon _tray;

	private readonly HotkeyWindow _hotkey = new HotkeyWindow();

	private readonly System.Windows.Forms.Timer _timer;

	private bool _pendingLock;

	private bool _pendingSettings;

	private bool _signalLock;

	private bool _needSetup;

	private DateTime _mediaLastSeenUtc = DateTime.MinValue;

	public TrayApp(AppConfig cfg, bool startLocked, bool openSettingsOnStart = false)
	{
		_cfg = cfg;
		_lock = new LockController(cfg);
		_lock.SettingsRequested += delegate
		{
			OpenSettings(force: true, skipIdentity: true);
		};
		_tray = new NotifyIcon
		{
			Icon = AppIcon.Create(),
			Visible = true,
			Text = "AM PC Lock"
		};
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip
		{
			Items = 
			{
				{
					"Lock now  (Ctrl+Alt+L / Win+Ctrl+A)",
					(Image?)null,
					(EventHandler?)delegate
					{
						LockNow();
					}
				},
				{
					"Settings…  (Win+Ctrl+G)",
					(Image?)null,
					(EventHandler?)delegate
					{
						OpenSettings(force: false);
					}
				},
				(ToolStripItem)new ToolStripSeparator(),
				{
					"Exit",
					(Image?)null,
					(EventHandler?)delegate
					{
						ExitApp();
					}
				}
			}
		};
		_tray.ContextMenuStrip = contextMenuStrip;
		_tray.DoubleClick += delegate
		{
			LockNow();
		};
		_tray.BalloonTipClicked += delegate
		{
			LockNow();
		};
		_hotkey.Register(1, 3u, 76u, LockNow);
		if (!_hotkey.Register(2, 10u, 65u, LockNow))
		{
			ConfigStore.Log("Win+Ctrl+A unavailable; falling back to Ctrl+Alt+A");
			if (!_hotkey.Register(2, 3u, 65u, LockNow))
			{
				ConfigStore.Log("Ctrl+Alt+A registration also failed");
			}
		}
		if (!_hotkey.Register(3, 10u, 71u, delegate
		{
			OpenSettings(force: false);
		}))
		{
			ConfigStore.Log("Win+Ctrl+G unavailable; falling back to Ctrl+Alt+G");
			if (!_hotkey.Register(3, 3u, 71u, delegate
			{
				OpenSettings(force: false);
			}))
			{
				ConfigStore.Log("Ctrl+Alt+G registration also failed");
			}
		}
		ThreadPool.RegisterWaitForSingleObject(Program.LockSignal, delegate
		{
			_signalLock = true;
		}, null, -1, executeOnlyOnce: false);
		_pendingLock = startLocked;
		_pendingSettings = openSettingsOnStart;
		_needSetup = !openSettingsOnStart && string.IsNullOrEmpty(cfg.PatternHash);
		_timer = new System.Windows.Forms.Timer
		{
			Interval = 1000
		};
		_timer.Tick += OnTick;
		_timer.Start();
		_tray.ShowBalloonTip(4000, "AM PC Lock", "Running in the notification area. Ctrl+Alt+L or Win+Ctrl+A locks this PC.", ToolTipIcon.Info);
	}

	private void OnTick(object? sender, EventArgs e)
	{
		if (_signalLock)
		{
			_signalLock = false;
			LockNow();
		}
		else if (_pendingLock)
		{
			_pendingLock = false;
			LockNow();
		}
		else if (_pendingSettings)
		{
			_pendingSettings = false;
			OpenSettings(force: true);
		}
		else if (_needSetup)
		{
			_needSetup = false;
			OpenSettings(force: true);
		}
		else if (!_lock.IsLocked && _cfg.AutoLockMinutes > 0 && !string.IsNullOrEmpty(_cfg.PatternHash))
		{
			TimeSpan timeSpan = TimeSpan.FromMinutes(_cfg.AutoLockMinutes);
			if (MediaGuard.IsForegroundMediaPlaying())
			{
				_mediaLastSeenUtc = DateTime.UtcNow;
			}
			if (IdleTime() >= timeSpan && DateTime.UtcNow - _mediaLastSeenUtc >= timeSpan)
			{
				_lock.Lock();
			}
		}
	}

	private static TimeSpan IdleTime()
	{
		Native.LASTINPUTINFO lASTINPUTINFO = default(Native.LASTINPUTINFO);
		lASTINPUTINFO.cbSize = (uint)Marshal.SizeOf<Native.LASTINPUTINFO>();
		Native.LASTINPUTINFO plii = lASTINPUTINFO;
		if (!Native.GetLastInputInfo(ref plii))
		{
			return TimeSpan.Zero;
		}
		return TimeSpan.FromMilliseconds((uint)Environment.TickCount - plii.dwTime);
	}

	private void LockNow()
	{
		if (!_lock.IsLocked)
		{
			if (string.IsNullOrEmpty(_cfg.PatternHash))
			{
				MessageBox.Show("Set an unlock pattern first: AM PC Lock cannot lock the PC without one.", "AM PC Lock", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				OpenSettings(force: true);
			}
			else
			{
				_lock.Lock();
			}
		}
	}

	private void OpenSettings(bool force, bool skipIdentity = false)
	{
		if (_lock.IsLocked)
		{
			return;
		}
		if (!skipIdentity && !string.IsNullOrEmpty(_cfg.PatternHash))
		{
			using VerifyIdentityForm verifyIdentityForm = new VerifyIdentityForm(_cfg);
			if (verifyIdentityForm.ShowDialog() != DialogResult.OK)
			{
				ConfigStore.Log("settings access cancelled at identity verification");
				return;
			}
			skipIdentity = true;
		}
		using SettingsForm settingsForm = new SettingsForm(_cfg, skipIdentity);
		if (settingsForm.ShowDialog() != DialogResult.OK && force && string.IsNullOrEmpty(_cfg.PatternHash))
		{
			_tray.ShowBalloonTip(5000, "AM PC Lock is not configured", "Set an unlock pattern from the tray menu before locking.", ToolTipIcon.Warning);
		}
	}

	private void ExitApp()
	{
		if (!_lock.IsLocked)
		{
			_timer.Stop();
			_timer.Dispose();
			_hotkey.Dispose();
			_tray.Visible = false;
			_tray.Dispose();
			ExitThread();
		}
	}
}
