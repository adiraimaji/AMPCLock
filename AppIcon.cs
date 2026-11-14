using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AMPCLock;

internal static class AppIcon
{
	public static Icon Create()
	{
		using Bitmap bitmap = new Bitmap(32, 32);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.Clear(Color.Transparent);
			using SolidBrush brush = new SolidBrush(Color.FromArgb(77, 163, 255));
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(16, 20, 30));
			using Pen pen = new Pen(Color.FromArgb(77, 163, 255), 3f);
			graphics.DrawArc(pen, 8, 4, 16, 16, 180, 180);
			graphics.FillRectangle(brush, 5, 13, 22, 16);
			graphics.FillEllipse(brush2, 14, 18, 4, 4);
			graphics.FillRectangle(brush2, 15, 21, 2, 4);
		}
		nint hicon = bitmap.GetHicon();
		try
		{
			using Icon icon = Icon.FromHandle(hicon);
			return (Icon)icon.Clone();
		}
		finally
		{
			DestroyIcon(hicon);
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool DestroyIcon(nint hIcon);
}
