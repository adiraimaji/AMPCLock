using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AMPCLock;

internal static class Native
{
	public struct KBDLLHOOKSTRUCT
	{
		public uint vkCode;

		public uint scanCode;

		public uint flags;

		public uint time;

		public nint dwExtraInfo;
	}

	public struct LASTINPUTINFO
	{
		public uint cbSize;

		public uint dwTime;
	}

	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public struct POINT
	{
		public int X;

		public int Y;
	}

	public delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

	public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

	public const int WH_KEYBOARD_LL = 13;

	public const int WH_MOUSE_LL = 14;

	public const int WM_KEYDOWN = 256;

	public const int WM_SYSKEYDOWN = 260;

	public const int WM_LBUTTONDOWN = 513;

	public const int WM_RBUTTONDOWN = 516;

	public const int WM_MBUTTONDOWN = 519;

	public const int WM_XBUTTONDOWN = 523;

	public const int WM_MOUSEWHEEL = 522;

	public const int WM_MOUSEHWHEEL = 526;

	public const int WM_HOTKEY = 786;

	public const int WM_SYSCOMMAND = 274;

	public const int SC_CLOSE = 61536;

	public const int SC_MONITORPOWER = 61808;

	public const int MONITOR_OFF = 2;

	public static readonly nint HWND_BROADCAST = new IntPtr(65535);

	public static readonly nint HWND_TOPMOST = new IntPtr(-1);

	public const uint SWP_NOSIZE = 1u;

	public const uint SWP_NOMOVE = 2u;

	public const uint SWP_NOACTIVATE = 16u;

	public const uint SWP_SHOWWINDOW = 64u;

	public const int SW_HIDE = 0;

	public const int SW_SHOW = 5;

	public const uint MOD_ALT = 1u;

	public const uint MOD_CONTROL = 2u;

	public const uint MOD_SHIFT = 4u;

	public const uint MOD_WIN = 8u;

	public const uint MOD_NOREPEAT = 16384u;

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnhookWindowsHookEx(nint hhk);

	[DllImport("user32.dll")]
	public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	public static extern nint GetModuleHandle(string? lpModuleName);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ClipCursor(ref RECT lpRect);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ClipCursor(nint lpRect);

	[DllImport("user32.dll")]
	public static extern int GetSystemMetrics(int nIndex);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern nint FindWindow(string? lpClassName, string? lpWindowName);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern nint FindWindowEx(nint parent, nint childAfter, string? className, string? windowName);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ShowWindow(nint hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWindowVisible(nint hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern int GetClassName(nint hWnd, char[] lpClassName, int nMaxCount);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnregisterHotKey(nint hWnd, int id);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

	[DllImport("user32.dll")]
	public static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	public static extern nint SendMessageTimeout(nint hWnd, int msg, nint wParam, nint lParam, uint fuFlags, uint uTimeout, out nint lpdwResult);

	[DllImport("user32.dll")]
	public static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	public static extern nint GetDesktopWindow();

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

	[DllImport("user32.dll")]
	public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

	[DllImport("user32.dll")]
	public static extern nint SetFocus(nint hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool BringWindowToTop(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	public static extern nint WindowFromPoint(POINT point);

	[DllImport("user32.dll")]
	public static extern void SwitchToThisWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool fAltTab);

	public static bool ForceForeground(nint hWnd)
	{
		if (hWnd == IntPtr.Zero)
		{
			return false;
		}
		ShowWindow(hWnd, 5);
		BringWindowToTop(hWnd);
		SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 67u);
		SetForegroundWindow(hWnd);
		if (GetForegroundWindow() != hWnd)
		{
			SwitchToThisWindow(hWnd, fAltTab: true);
		}
		SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 67u);
		SetFocus(hWnd);
		return GetForegroundWindow() == hWnd;
	}
}
