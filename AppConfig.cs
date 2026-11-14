using System;
using System.Text.Json.Serialization;

namespace AMPCLock;

public sealed class AppConfig
{
	public int GridSize { get; set; } = 3;


	public int PatternGridSize { get; set; } = 3;


	public string? PatternHash { get; set; }

	public string? PatternSalt { get; set; }

	public int PatternIterations { get; set; } = 120000;


	public string? RecoveryHash { get; set; }

	public string? RecoverySalt { get; set; }

	public string? SecurityQuestion { get; set; }

	public string? SecurityAnswerHash { get; set; }

	public string? SecurityAnswerSalt { get; set; }

	public string AlertText { get; set; } = "Do not touch my computer!";


	public int AlertAfterFailures { get; set; } = 3;


	public bool AlertSound { get; set; } = true;


	public int AlertCooldownSeconds { get; set; } = 5;


	public string BackgroundColor { get; set; } = "#0E1018";


	public string? BackgroundImage { get; set; }

	public string BackgroundMode { get; set; } = "fill";


	public bool TransparentLockBackground { get; set; }

	public int AutoHideLockSeconds { get; set; }

	public int AutoLockMinutes { get; set; } = 5;


	public bool LockOnStartup { get; set; }

	public bool StartWithWindows { get; set; }

	public bool BlockKeyboard { get; set; } = true;


	public bool HideTaskbar { get; set; } = true;


	public bool DisableTaskManager { get; set; } = true;


	public bool BlockTaskSwitch { get; set; } = true;


	public bool MonitorOffEnabled { get; set; }

	public int MonitorOffMinutes { get; set; } = 5;


	public string Hotkey { get; set; } = "Ctrl+Alt+L";


	[JsonIgnore]
	public string BackgroundModeNormalized
	{
		get
		{
			bool flag;
			switch (BackgroundMode)
			{
			case "fill":
			case "fit":
			case "stretch":
			case "center":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				return "fill";
			}
			return BackgroundMode;
		}
	}

	[JsonIgnore]
	public int ActiveGrid
	{
		get
		{
			int patternGridSize = PatternGridSize;
			if (patternGridSize < 3 || patternGridSize > 5)
			{
				return Math.Clamp(GridSize, 3, 5);
			}
			return PatternGridSize;
		}
	}
}
