using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class InputGuard : IDisposable
{
	private Native.LowLevelKeyboardProc? _proc;

	private Native.LowLevelKeyboardProc? _mouseProc;

	private nint _hook = IntPtr.Zero;

	private nint _mouseHook = IntPtr.Zero;

	private bool _cursorClipped;

	private readonly bool _blockKeyboard;

	private SynchronizationContext? _sync;

	private bool _stealth;

	private int _focusPending;

	public event Action? RevealRequested;

	public event Action? FocusRequested;

	public event Action<uint>? KeyPressed;

	public InputGuard(bool blockKeyboard)
	{
		_blockKeyboard = blockKeyboard;
	}

	public void Start()
	{
		_sync = SynchronizationContext.Current;
		if (_hook == IntPtr.Zero)
		{
			_proc = HookCallback;
			_hook = Native.SetWindowsHookEx(13, _proc, Native.GetModuleHandle(null), 0u);
			if (_hook == IntPtr.Zero)
			{
				ConfigStore.Log($"keyboard hook failed (win32 {Marshal.GetLastWin32Error()})");
			}
		}
		if (_mouseHook == IntPtr.Zero)
		{
			_mouseProc = MouseHookCallback;
			_mouseHook = Native.SetWindowsHookEx(14, _mouseProc, Native.GetModuleHandle(null), 0u);
			if (_mouseHook == IntPtr.Zero)
			{
				ConfigStore.Log($"mouse hook failed (win32 {Marshal.GetLastWin32Error()})");
			}
		}
		ClipToPrimary();
	}

	public void SetStealth(bool hidden)
	{
		_stealth = hidden;
		if (hidden)
		{
			if (_cursorClipped)
			{
				Native.ClipCursor(IntPtr.Zero);
				_cursorClipped = false;
			}
		}
		else
		{
			ClipToPrimary();
		}
	}

	private void ClipToPrimary()
	{
		Rectangle rectangle = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
		Native.RECT rECT = default(Native.RECT);
		rECT.Left = rectangle.Left;
		rECT.Top = rectangle.Top;
		rECT.Right = rectangle.Right;
		rECT.Bottom = rectangle.Bottom;
		Native.RECT lpRect = rECT;
		_cursorClipped = Native.ClipCursor(ref lpRect);
	}

	private nint HookCallback(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0 && (wParam == 256 || wParam == 260))
		{
			uint vkCode = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam).vkCode;
			if (_stealth)
			{
				RequestReveal();
				return 1;
			}
			if (!IsForegroundOurs())
			{
				RequestFocus();
				if (IsPatternKey(vkCode))
				{
					RequestKey(vkCode);
				}
				return 1;
			}
			if (_blockKeyboard && !IsTextEntryKey(vkCode))
			{
				return 1;
			}
		}
		return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
	}

	private nint MouseHookCallback(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0)
		{
			bool flag = wParam == 513 || wParam == 516 || wParam == 519 || wParam == 523;
			bool flag2 = wParam == 522 || wParam == 526;
			if (_stealth && flag)
			{
				RequestReveal();
				return 1;
			}
			if ((flag || flag2) && !IsPointOverOurWindow())
			{
				RequestFocus();
				return 1;
			}
		}
		return Native.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
	}

	private static bool IsForegroundOurs()
	{
		nint foregroundWindow = Native.GetForegroundWindow();
		if (foregroundWindow == IntPtr.Zero)
		{
			return true;
		}
		Native.GetWindowThreadProcessId(foregroundWindow, out var lpdwProcessId);
		return lpdwProcessId == (uint)Environment.ProcessId;
	}

	private static bool IsPointOverOurWindow()
	{
		if (!Native.GetCursorPos(out var lpPoint))
		{
			return true;
		}
		nint num = Native.WindowFromPoint(lpPoint);
		if (num == IntPtr.Zero)
		{
			return false;
		}
		Native.GetWindowThreadProcessId(num, out var lpdwProcessId);
		return lpdwProcessId == (uint)Environment.ProcessId;
	}

	private static bool IsPatternKey(uint vk)
	{
		if (vk != 8 && vk != 13)
		{
			if (vk >= 65)
			{
				return vk <= 90;
			}
			return false;
		}
		return true;
	}

	private void RequestReveal()
	{
		Action handler = this.RevealRequested;
		if (handler != null)
		{
			Post(delegate
			{
				handler();
			});
		}
	}

	private void RequestFocus()
	{
		Action handler = this.FocusRequested;
		if (handler != null && Interlocked.Exchange(ref _focusPending, 1) != 1)
		{
			Post(delegate
			{
				Interlocked.Exchange(ref _focusPending, 0);
				handler();
			});
		}
	}

	private void RequestKey(uint vk)
	{
		Action<uint> handler = this.KeyPressed;
		if (handler != null)
		{
			Post(delegate
			{
				handler(vk);
			});
		}
	}

	private void Post(Action action)
	{
		Action action2 = action;
		if (_sync != null)
		{
			_sync.Post(delegate
			{
				action2();
			}, null);
		}
		else
		{
			action2();
		}
	}

	private static bool IsTextEntryKey(uint vk)
	{
		switch (vk)
		{
		case 8u:
		case 13u:
		case 16u:
		case 32u:
		case 48u:
		case 49u:
		case 50u:
		case 51u:
		case 52u:
		case 53u:
		case 54u:
		case 55u:
		case 56u:
		case 57u:
		case 65u:
		case 66u:
		case 67u:
		case 68u:
		case 69u:
		case 70u:
		case 71u:
		case 72u:
		case 73u:
		case 74u:
		case 75u:
		case 76u:
		case 77u:
		case 78u:
		case 79u:
		case 80u:
		case 81u:
		case 82u:
		case 83u:
		case 84u:
		case 85u:
		case 86u:
		case 87u:
		case 88u:
		case 89u:
		case 90u:
		case 160u:
		case 161u:
		case 186u:
		case 187u:
		case 188u:
		case 189u:
		case 190u:
		case 191u:
		case 192u:
		case 219u:
		case 220u:
		case 221u:
		case 222u:
			return true;
		default:
			return false;
		}
	}

	public void Stop()
	{
		if (_hook != IntPtr.Zero)
		{
			Native.UnhookWindowsHookEx(_hook);
			_hook = IntPtr.Zero;
			_proc = null;
		}
		if (_mouseHook != IntPtr.Zero)
		{
			Native.UnhookWindowsHookEx(_mouseHook);
			_mouseHook = IntPtr.Zero;
			_mouseProc = null;
		}
		if (_cursorClipped)
		{
			Native.ClipCursor(IntPtr.Zero);
			_cursorClipped = false;
		}
		_stealth = false;
	}

	public void Dispose()
	{
		Stop();
	}
}
