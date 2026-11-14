using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AMPCLock;

internal static class MediaGuard
{
	[ComImport]
	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	private class MMDeviceEnumeratorComObject
	{
	}

	[ComImport]
	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDeviceEnumerator
	{
		[PreserveSig]
		int EnumAudioEndpoints(int dataFlow, int stateMask, out nint devices);

		[PreserveSig]
		int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? endpoint);

		[PreserveSig]
		int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice? device);

		[PreserveSig]
		int RegisterEndpointNotificationCallback(nint client);

		[PreserveSig]
		int UnregisterEndpointNotificationCallback(nint client);
	}

	[ComImport]
	[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDevice
	{
		[PreserveSig]
		int Activate(ref Guid iid, uint clsCtx, nint activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object? instance);

		[PreserveSig]
		int OpenPropertyStore(uint stgmAccess, out nint properties);

		[PreserveSig]
		int GetId([MarshalAs(UnmanagedType.LPWStr)] out string? id);

		[PreserveSig]
		int GetState(out uint state);
	}

	[ComImport]
	[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioSessionManager2
	{
		[PreserveSig]
		int GetAudioSessionControl(ref Guid audioSessionGuid, uint streamFlags, out nint sessionControl);

		[PreserveSig]
		int GetSimpleAudioVolume(ref Guid audioSessionGuid, uint streamFlags, out nint audioVolume);

		[PreserveSig]
		int GetSessionEnumerator(out IAudioSessionEnumerator? sessionEnum);

		[PreserveSig]
		int RegisterSessionNotification(nint sessionNotification);

		[PreserveSig]
		int UnregisterSessionNotification(nint sessionNotification);

		[PreserveSig]
		int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionId, nint duckNotification);

		[PreserveSig]
		int UnregisterDuckNotification(nint duckNotification);
	}

	[ComImport]
	[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioSessionEnumerator
	{
		[PreserveSig]
		int GetCount(out int sessionCount);

		[PreserveSig]
		int GetSession(int sessionIndex, out IAudioSessionControl? session);
	}

	[ComImport]
	[Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioSessionControl
	{
		[PreserveSig]
		int GetState(out int state);

		[PreserveSig]
		int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string? displayName);

		[PreserveSig]
		int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string displayName, ref Guid eventContext);

		[PreserveSig]
		int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string? iconPath);

		[PreserveSig]
		int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string iconPath, ref Guid eventContext);

		[PreserveSig]
		int GetGroupingParam(out Guid groupingParam);

		[PreserveSig]
		int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);

		[PreserveSig]
		int RegisterAudioSessionNotification(nint newNotifications);

		[PreserveSig]
		int UnregisterAudioSessionNotification(nint newNotifications);
	}

	[ComImport]
	[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioSessionControl2
	{
		[PreserveSig]
		int GetState(out int state);

		[PreserveSig]
		int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string? displayName);

		[PreserveSig]
		int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string displayName, ref Guid eventContext);

		[PreserveSig]
		int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string? iconPath);

		[PreserveSig]
		int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string iconPath, ref Guid eventContext);

		[PreserveSig]
		int GetGroupingParam(out Guid groupingParam);

		[PreserveSig]
		int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);

		[PreserveSig]
		int RegisterAudioSessionNotification(nint newNotifications);

		[PreserveSig]
		int UnregisterAudioSessionNotification(nint newNotifications);

		[PreserveSig]
		int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string? sessionIdentifier);

		[PreserveSig]
		int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string? sessionInstanceIdentifier);

		[PreserveSig]
		int GetProcessId(out uint processId);

		[PreserveSig]
		int IsSystemSoundsSession();

		[PreserveSig]
		int SetDuckingPreference(bool optOut);
	}

	private static readonly HashSet<string> KnownVideoPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"vlc", "mpv", "mpc-hc", "mpc-hc64", "mpc-be", "mpc-be64", "potplayer", "potplayermini", "potplayermini64", "wmplayer",
		"video.ui", "movies", "mediaplayer"
	};

	public static bool IsForegroundMediaPlaying()
	{
		try
		{
			nint foregroundWindow = Native.GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return false;
			}
			Native.GetWindowThreadProcessId(foregroundWindow, out var lpdwProcessId);
			if (lpdwProcessId == 0)
			{
				return false;
			}
			string processName = GetProcessName(lpdwProcessId);
			if (processName != null && KnownVideoPlayers.Contains(processName))
			{
				return true;
			}
			return HasActiveAudioSession(lpdwProcessId, processName);
		}
		catch (Exception ex)
		{
			ConfigStore.Log("media detection failed: " + ex.Message);
			return false;
		}
	}

	private static string? GetProcessName(uint pid)
	{
		try
		{
			using Process process = Process.GetProcessById((int)pid);
			return process.ProcessName;
		}
		catch
		{
			return null;
		}
	}

	private static bool HasActiveAudioSession(uint foregroundPid, string? foregroundName)
	{
		IMMDeviceEnumerator iMMDeviceEnumerator = null;
		IMMDevice endpoint = null;
		IAudioSessionManager2 audioSessionManager = null;
		IAudioSessionEnumerator sessionEnum = null;
		try
		{
			iMMDeviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
			if (iMMDeviceEnumerator.GetDefaultAudioEndpoint(0, 0, out endpoint) != 0 || endpoint == null)
			{
				return false;
			}
			Guid iid = typeof(IAudioSessionManager2).GUID;
			if (endpoint.Activate(ref iid, 23u, IntPtr.Zero, out object instance) != 0 || !(instance is IAudioSessionManager2 audioSessionManager2))
			{
				return false;
			}
			audioSessionManager = audioSessionManager2;
			if (audioSessionManager.GetSessionEnumerator(out sessionEnum) != 0 || sessionEnum == null)
			{
				return false;
			}
			if (sessionEnum.GetCount(out var sessionCount) != 0)
			{
				return false;
			}
			for (int i = 0; i < sessionCount; i++)
			{
				if (sessionEnum.GetSession(i, out IAudioSessionControl session) != 0 || session == null)
				{
					continue;
				}
				try
				{
					if (session.GetState(out var state) == 0 && state == 1 && session is IAudioSessionControl2 audioSessionControl && audioSessionControl.GetProcessId(out var processId) == 0 && processId != 0)
					{
						if (processId == foregroundPid)
						{
							return true;
						}
						if (!string.IsNullOrEmpty(foregroundName) && string.Equals(GetProcessName(processId), foregroundName, StringComparison.OrdinalIgnoreCase))
						{
							return true;
						}
					}
				}
				finally
				{
					Marshal.ReleaseComObject(session);
				}
			}
		}
		finally
		{
			if (sessionEnum != null)
			{
				Marshal.ReleaseComObject(sessionEnum);
			}
			if (audioSessionManager != null)
			{
				Marshal.ReleaseComObject(audioSessionManager);
			}
			if (endpoint != null)
			{
				Marshal.ReleaseComObject(endpoint);
			}
			if (iMMDeviceEnumerator != null)
			{
				Marshal.ReleaseComObject(iMMDeviceEnumerator);
			}
		}
		return false;
	}
}
