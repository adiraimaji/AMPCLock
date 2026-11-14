using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AMPCLock;

internal static class SystemGuard
{
	private const string PolicySystem = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";

	private const string PolicyExplorer = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";

	private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string RunValueName = "AM PC Lock";

	private static SystemState _state = new SystemState();

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	public static SystemState CurrentState => _state;

	public static void HideTaskbar()
	{
		foreach (nint item in FindTaskbars())
		{
			Native.ShowWindow(item, 0);
		}
		if (!_state.TaskbarHidden)
		{
			_state.TaskbarHidden = true;
			Persist();
		}
	}

	public static void RestoreTaskbar()
	{
		foreach (nint item in FindTaskbars())
		{
			Native.ShowWindow(item, 5);
		}
		_state.TaskbarHidden = false;
		Persist();
	}

	public static void ApplyPolicies(bool disableTaskManager, bool blockTaskSwitch)
	{
		List<(string, string)> list = new List<(string, string)>();
		if (disableTaskManager)
		{
			list.Add(("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableTaskmgr"));
			list.Add(("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "HideFastUserSwitching"));
		}
		if (blockTaskSwitch)
		{
			list.Add(("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLogoff"));
			list.Add(("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoClose"));
		}
		foreach (var item2 in list)
		{
			var (path, name) = item2;
			if (_state.Policies.Any((RegistrySnapshot p) => p.Path == path && p.Name == name))
			{
				continue;
			}
			RegistrySnapshot item;
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(path, writable: false);
				object obj = registryKey?.GetValue(name, null);
				item = new RegistrySnapshot
				{
					Path = path,
					Name = name,
					Existed = (obj != null),
					Value = ((obj is int num) ? num : 0)
				};
			}
			catch (Exception ex)
			{
				ConfigStore.Log("policy snapshot skipped for " + name + ": " + ex.Message);
				continue;
			}
			try
			{
				using RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey(path, writable: true);
				registryKey2.SetValue(name, 1, RegistryValueKind.DWord);
				_state.Policies.Add(item);
			}
			catch (Exception ex2)
			{
				ConfigStore.Log("policy write skipped for " + name + ": " + ex2.Message);
			}
		}
		Persist();
	}

	public static void RestorePolicies(SystemState? state)
	{
		if (state == null)
		{
			state = _state;
		}
		foreach (RegistrySnapshot policy in state.Policies)
		{
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(policy.Path, writable: true);
				if (policy.Existed)
				{
					registryKey.SetValue(policy.Name, policy.Value, RegistryValueKind.DWord);
				}
				else
				{
					registryKey.DeleteValue(policy.Name, throwOnMissingValue: false);
				}
			}
			catch (Exception ex)
			{
				ConfigStore.Log("policy restore failed for " + policy.Name + ": " + ex.Message);
			}
		}
		_state = new SystemState();
		Persist();
	}

	public static void MonitorOff()
	{
		Native.SendMessageTimeout(Native.HWND_BROADCAST, 274, 61808, 2, 2u, 1000u, out var _);
	}

	public static bool RestoreFromJournal()
	{
		try
		{
			if (!File.Exists(ConfigStore.StatePath))
			{
				return false;
			}
			SystemState systemState = JsonSerializer.Deserialize<SystemState>(File.ReadAllText(ConfigStore.StatePath));
			if (systemState == null)
			{
				return false;
			}
			if (systemState.TaskbarHidden)
			{
				foreach (nint item in FindTaskbars())
				{
					Native.ShowWindow(item, 5);
				}
			}
			RestorePolicies(systemState);
			File.Delete(ConfigStore.StatePath);
			return true;
		}
		catch (Exception ex)
		{
			ConfigStore.Log("restore from journal failed: " + ex.Message);
			return false;
		}
	}

	public static void Persist()
	{
		try
		{
			Directory.CreateDirectory(ConfigStore.RootDir);
			File.WriteAllText(ConfigStore.StatePath, JsonSerializer.Serialize(_state, JsonOptions));
		}
		catch (Exception ex)
		{
			ConfigStore.Log("state persist failed: " + ex.Message);
		}
	}

	public static void ClearJournal()
	{
		try
		{
			if (File.Exists(ConfigStore.StatePath))
			{
				File.Delete(ConfigStore.StatePath);
			}
		}
		catch (Exception ex)
		{
			ConfigStore.Log("journal clear failed: " + ex.Message);
		}
	}

	private static IEnumerable<nint> FindTaskbars()
	{
		List<nint> found = new List<nint>();
		Native.EnumWindows(delegate(nint hwnd, nint _)
		{
			char[] array = new char[128];
			int className = Native.GetClassName(hwnd, array, array.Length);
			if (className <= 0)
			{
				return true;
			}
			string text = new string(array, 0, className);
			if (text == "Shell_TrayWnd" || text == "Shell_SecondaryTrayWnd")
			{
				found.Add(hwnd);
			}
			return true;
		}, IntPtr.Zero);
		return found;
	}

	public static bool IsAutoStartEnabled()
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: false);
		return registryKey?.GetValue("AM PC Lock") is string text && text.Length > 0;
	}

	public static void SetAutoStart(bool enabled, bool lockAtSignIn)
	{
		using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
		if (enabled)
		{
			string text = Environment.ProcessPath ?? Application.ExecutablePath;
			string text2 = (lockAtSignIn ? " /lock" : "");
			registryKey.SetValue("AM PC Lock", "\"" + text + "\"" + text2, RegistryValueKind.String);
		}
		else
		{
			registryKey.DeleteValue("AM PC Lock", throwOnMissingValue: false);
		}
	}
}
