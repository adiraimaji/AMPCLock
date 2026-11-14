using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
	private readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();

	public bool Register(int id, uint modifiers, uint virtualKey, Action action)
	{
		if (base.Handle == IntPtr.Zero)
		{
			CreateHandle(new CreateParams
			{
				Caption = "AMPCLock.Hotkey"
			});
		}
		if (!Native.RegisterHotKey(base.Handle, id, modifiers | 0x4000u, virtualKey))
		{
			return false;
		}
		_actions[id] = action;
		return true;
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 786 && _actions.TryGetValue(((IntPtr)m.WParam).ToInt32(), out Action value))
		{
			value();
		}
		else
		{
			base.WndProc(ref m);
		}
	}

	public void Dispose()
	{
		if (base.Handle == IntPtr.Zero)
		{
			return;
		}
		foreach (int key in _actions.Keys)
		{
			Native.UnregisterHotKey(base.Handle, key);
		}
		_actions.Clear();
		DestroyHandle();
	}
}
