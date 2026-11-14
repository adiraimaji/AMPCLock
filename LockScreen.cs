using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class LockScreen : Form
{
	private readonly LockController _controller;

	private readonly AppConfig _cfg;

	private readonly bool _primary;

	private readonly bool _transparent;

	private readonly PatternPad _pad = new PatternPad();

	private int _typedCount;

	private Image? _background;

	private string _statusText = "Draw a pattern, or type the letters on the dots and press Enter";

	private bool _statusWrong;

	private Rectangle _padRect;

	private Rectangle _typedRect;

	private Rectangle _forgotRect;

	private string _clockText = "";

	private Timer? _clockTimer;

	private bool _hoverForgot;

	public bool IsPrimary => _primary;

	public LockScreen(LockController controller, AppConfig cfg, Screen screen, bool primary)
	{
		_controller = controller;
		_cfg = cfg;
		_primary = primary;
		_transparent = cfg.TransparentLockBackground && primary;
		base.FormBorderStyle = FormBorderStyle.None;
		base.StartPosition = FormStartPosition.Manual;
		base.Bounds = screen.Bounds;
		base.TopMost = true;
		base.ShowInTaskbar = false;
		base.KeyPreview = true;
		if (_transparent)
		{
			BackColor = Color.Fuchsia;
			base.TransparencyKey = Color.Fuchsia;
		}
		else
		{
			BackColor = Color.FromArgb(14, 16, 24);
		}
		DoubleBuffered = true;
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		LoadBackground();
		if (primary)
		{
			_pad.GridSize = cfg.ActiveGrid;
			_pad.PatternCompleted += OnPatternCompleted;
			if (_transparent)
			{
				_pad.BackColor = Color.FromArgb(14, 16, 24);
			}
			base.Controls.Add(_pad);
		}
		UpdateClock();
		_clockTimer = new Timer
		{
			Interval = 1000
		};
		_clockTimer.Tick += delegate
		{
			UpdateClock();
			Invalidate();
		};
		_clockTimer.Start();
	}

	private void UpdateClock()
	{
		_clockText = DateTime.Now.ToString("h:mm tt  ·  dddd, MMMM d");
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		LayoutChildren();
		if (_primary)
		{
			_pad.Focus();
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		LayoutChildren();
	}

	private void LayoutChildren()
	{
		int width = base.ClientSize.Width;
		int height = base.ClientSize.Height;
		if (!_primary)
		{
			_padRect = Rectangle.Empty;
			_typedRect = Rectangle.Empty;
			_forgotRect = Rectangle.Empty;
			return;
		}
		int num = Math.Max(220, Math.Min(380, Math.Min(width, height) - 320));
		_pad.Size = new Size(num, num);
		_pad.Location = new Point((width - num) / 2, (height - num) / 2 - 20);
		_padRect = _pad.Bounds;
		int num2 = Math.Min(380, width - 80);
		_typedRect = new Rectangle((width - num2) / 2, _padRect.Bottom + 16, num2, 34);
		int num3 = Math.Min(460, width - 80);
		_forgotRect = new Rectangle((width - num3) / 2, _typedRect.Bottom + 42, num3, 22);
	}

	private void LoadBackground()
	{
		if (_transparent || string.IsNullOrWhiteSpace(_cfg.BackgroundImage) || !File.Exists(_cfg.BackgroundImage))
		{
			return;
		}
		try
		{
			using Image original = Image.FromFile(_cfg.BackgroundImage);
			_background = new Bitmap(original);
		}
		catch (Exception ex)
		{
			ConfigStore.Log("background load failed: " + ex.Message);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (_primary && _padRect.IsEmpty)
		{
			LayoutChildren();
		}
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		if (!_transparent)
		{
			using (SolidBrush brush = new SolidBrush(ParseColor(_cfg.BackgroundColor, Color.FromArgb(14, 16, 24))))
			{
				graphics.FillRectangle(brush, base.ClientRectangle);
			}
			if (_background != null)
			{
				DrawBackgroundImage(graphics, _background);
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb(90, 8, 10, 16));
				graphics.FillRectangle(brush2, base.ClientRectangle);
			}
			else
			{
				DrawGlow(graphics);
			}
		}
		if (_primary)
		{
			DrawPrimary(graphics);
		}
		else
		{
			DrawSecondary(graphics);
		}
		base.OnPaint(e);
	}

	private void DrawGlow(Graphics g)
	{
		using GraphicsPath graphicsPath = new GraphicsPath();
		float num = (float)Math.Max(base.ClientSize.Width, base.ClientSize.Height) * 0.75f;
		graphicsPath.AddEllipse((float)base.ClientSize.Width / 2f - num / 2f, (float)base.ClientSize.Height / 2f - num / 2f, num, num);
		PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
		pathGradientBrush.CenterColor = Color.FromArgb(46, 42, 66);
		pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, 20, 22, 34) };
		using PathGradientBrush brush = pathGradientBrush;
		g.FillPath(brush, graphicsPath);
		using SolidBrush brush2 = new SolidBrush(Color.FromArgb(16, 120, 150, 210));
		for (int i = 40; i < base.ClientSize.Height; i += 56)
		{
			for (int j = 40; j < base.ClientSize.Width; j += 56)
			{
				g.FillEllipse(brush2, j, i, 2.5f, 2.5f);
			}
		}
	}

	private void DrawPrimary(Graphics g)
	{
		float num = (float)base.ClientSize.Width / 2f;
		if (_transparent)
		{
			DrawCard(g, GetPrimaryCardRect());
		}
		using Font font = new Font("Segoe UI Semibold", Math.Clamp((float)base.ClientSize.Width / 64f, 18f, 34f), FontStyle.Bold);
		using SolidBrush brush = new SolidBrush(Color.FromArgb(240, 246, 255));
		SizeF sizeF = g.MeasureString("AM PC LOCK", font);
		int num2 = _padRect.Top - 126;
		g.DrawString("AM PC LOCK", font, brush, num - sizeF.Width / 2f, num2);
		using Font font2 = new Font("Segoe UI", 10.5f);
		using SolidBrush brush2 = new SolidBrush(Color.FromArgb(142, 158, 188));
		g.DrawString("This workstation is locked", font2, brush2, num - g.MeasureString("This workstation is locked", font2).Width / 2f, (float)num2 + sizeF.Height - 2f);
		DrawTypedField(g);
		using Font font3 = new Font("Segoe UI", 10.5f);
		using SolidBrush brush3 = new SolidBrush(_statusWrong ? Color.FromArgb(255, 138, 138) : Color.FromArgb(168, 186, 214));
		SizeF sizeF2 = g.MeasureString(_statusText, font3);
		int num3 = (_typedRect.IsEmpty ? (_padRect.Bottom + 20) : (_typedRect.Bottom + 12));
		g.DrawString(_statusText, font3, brush3, num - sizeF2.Width / 2f, num3);
		using Font font4 = new Font("Segoe UI", 9f, FontStyle.Underline);
		using SolidBrush brush4 = new SolidBrush(_hoverForgot ? Color.FromArgb(185, 205, 235) : Color.FromArgb(125, 145, 180));
		g.DrawString("Forgot pattern?  Use recovery code or security answer", font4, brush4, num - g.MeasureString("Forgot pattern?  Use recovery code or security answer", font4).Width / 2f, _forgotRect.Top + 3);
		using Font font5 = new Font("Segoe UI", 12f);
		using SolidBrush brush5 = new SolidBrush(Color.FromArgb(140, 158, 190));
		SizeF sizeF3 = g.MeasureString(_clockText, font5);
		float num4 = num - sizeF3.Width / 2f;
		float num5 = base.ClientSize.Height - 62;
		if (_transparent)
		{
			DrawCard(g, Rectangle.Round(new RectangleF(num4 - 16f, num5 - 6f, sizeF3.Width + 32f, sizeF3.Height + 12f)));
		}
		g.DrawString(_clockText, font5, brush5, num4, num5);
	}

	private void DrawTypedField(Graphics g)
	{
		if (_typedRect.IsEmpty)
		{
			return;
		}
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(22, 26, 38)))
		{
			g.FillRectangle(brush, _typedRect);
		}
		using (Pen pen = new Pen(Color.FromArgb(64, 84, 120)))
		{
			g.DrawRectangle(pen, _typedRect.X, _typedRect.Y, _typedRect.Width - 1, _typedRect.Height - 1);
		}
		bool flag = _typedCount > 0;
		string text = (flag ? new string('*', _typedCount) : "Typed letters appear here as *");
		using Font font = (flag ? new Font("Consolas", 15f, FontStyle.Bold) : new Font("Segoe UI", 9.5f));
		using SolidBrush brush2 = new SolidBrush(flag ? Color.FromArgb(215, 232, 255) : Color.FromArgb(110, 126, 152));
		SizeF sizeF = g.MeasureString(text, font);
		g.DrawString(text, font, brush2, (float)_typedRect.X + ((float)_typedRect.Width - sizeF.Width) / 2f, (float)_typedRect.Y + ((float)_typedRect.Height - sizeF.Height) / 2f);
	}

	private void DrawSecondary(Graphics g)
	{
		float num = (float)base.ClientSize.Width / 2f;
		if (_transparent)
		{
			int num2 = Math.Min(base.ClientSize.Width - 40, 520);
			int num3 = 220;
			DrawCard(g, new Rectangle((base.ClientSize.Width - num2) / 2, (base.ClientSize.Height - num3) / 2, num2, num3));
		}
		using Font font = new Font("Segoe UI Semibold", 34f, FontStyle.Bold);
		using SolidBrush brush = new SolidBrush(Color.FromArgb(228, 236, 250));
		g.DrawString("AM PC LOCK", font, brush, num - g.MeasureString("AM PC LOCK", font).Width / 2f, (float)base.ClientSize.Height / 2f - 96f);
		using Font font2 = new Font("Segoe UI", 12f);
		using SolidBrush brush2 = new SolidBrush(Color.FromArgb(145, 162, 192));
		g.DrawString("This workstation is locked", font2, brush2, num - g.MeasureString("This workstation is locked", font2).Width / 2f, (float)base.ClientSize.Height / 2f - 42f);
		using Font font3 = new Font("Segoe UI", 20f);
		using SolidBrush brush3 = new SolidBrush(Color.FromArgb(150, 168, 200));
		SizeF sizeF = g.MeasureString(_clockText, font3);
		g.DrawString(_clockText, font3, brush3, num - sizeF.Width / 2f, (float)base.ClientSize.Height / 2f + 10f);
	}

	private Rectangle GetPrimaryCardRect()
	{
		int num = Math.Max(0, _padRect.Left - 70);
		int num2 = Math.Max(0, _padRect.Top - 150);
		int num3 = Math.Min(base.ClientSize.Width, _padRect.Right + 70);
		int num4 = Math.Min(base.ClientSize.Height, _forgotRect.Bottom + 28);
		if (num3 <= num || num4 <= num2)
		{
			return Rectangle.Empty;
		}
		return Rectangle.FromLTRB(num, num2, num3, num4);
	}

	private static void DrawCard(Graphics g, Rectangle rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}
		using SolidBrush brush = new SolidBrush(Color.FromArgb(14, 16, 24));
		g.FillRectangle(brush, rect);
		using Pen pen = new Pen(Color.FromArgb(52, 68, 100));
		g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
	}

	private void DrawBackgroundImage(Graphics g, Image image)
	{
		Rectangle clientRectangle = base.ClientRectangle;
		switch (_cfg.BackgroundModeNormalized)
		{
		case "stretch":
			g.DrawImage(image, clientRectangle);
			break;
		case "center":
			g.DrawImage(image, new Rectangle(clientRectangle.X + (clientRectangle.Width - image.Width) / 2, clientRectangle.Y + (clientRectangle.Height - image.Height) / 2, image.Width, image.Height));
			break;
		case "fit":
		{
			float num4 = Math.Min((float)clientRectangle.Width / (float)image.Width, (float)clientRectangle.Height / (float)image.Height);
			int num5 = (int)((float)image.Width * num4);
			int num6 = (int)((float)image.Height * num4);
			g.DrawImage(image, new Rectangle(clientRectangle.X + (clientRectangle.Width - num5) / 2, clientRectangle.Y + (clientRectangle.Height - num6) / 2, num5, num6));
			break;
		}
		default:
		{
			float num = Math.Max((float)clientRectangle.Width / (float)image.Width, (float)clientRectangle.Height / (float)image.Height);
			int num2 = (int)((float)image.Width * num);
			int num3 = (int)((float)image.Height * num);
			g.DrawImage(image, new Rectangle(clientRectangle.X + (clientRectangle.Width - num2) / 2, clientRectangle.Y + (clientRectangle.Height - num3) / 2, num2, num3));
			break;
		}
		}
	}

	private static Color ParseColor(string value, Color fallback)
	{
		try
		{
			return ColorTranslator.FromHtml(value);
		}
		catch
		{
			return fallback;
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_primary)
		{
			bool flag = _forgotRect.Contains(e.Location);
			if (flag != _hoverForgot)
			{
				_hoverForgot = flag;
				Cursor = (flag ? Cursors.Hand : Cursors.Default);
				Invalidate(_forgotRect);
			}
		}
	}

	protected override void OnMouseClick(MouseEventArgs e)
	{
		base.OnMouseClick(e);
		if (_primary && _forgotRect.Contains(e.Location))
		{
			_controller.ShowRecovery(this);
		}
	}

	private void OnPatternCompleted(string pattern)
	{
		_controller.TryPattern(pattern);
	}

	public void ShowAttempt(string message, bool wrong)
	{
		if (_primary)
		{
			_statusText = message;
			_statusWrong = wrong;
			int y = (_typedRect.IsEmpty ? (_padRect.Bottom + 8) : (_typedRect.Bottom + 4));
			Invalidate(new Rectangle(0, y, base.ClientSize.Width, 120));
		}
	}

	public void SetTypedCount(int count)
	{
		if (_primary)
		{
			count = Math.Max(0, count);
			if (_typedCount != count)
			{
				_typedCount = count;
				Invalidate(Rectangle.Inflate(_typedRect, 4, 4));
			}
		}
	}

	public void SetInputEnabled(bool enabled)
	{
		if (_primary)
		{
			_pad.InputEnabled = enabled;
			_pad.Enabled = enabled;
		}
	}

	public void ClearPad()
	{
		_pad.Clear();
	}

	public void SetDotLabels(char[] labels)
	{
		if (_primary)
		{
			_pad.SetLabels(labels);
		}
	}

	public void SetStealthHidden(bool hidden)
	{
		if (hidden)
		{
			Hide();
			return;
		}
		Show();
		base.TopMost = true;
		BringToFront();
		if (_primary)
		{
			ForceFocus();
		}
		Invalidate();
	}

	public void ForceFocus()
	{
		if (base.IsHandleCreated && !base.IsDisposed)
		{
			Native.ForceForeground(base.Handle);
			if (_primary)
			{
				_pad.Focus();
			}
		}
	}

	protected override void WndProc(ref Message m)
	{
		if (_controller.IsPreview || (m.Msg != 16 && (m.Msg != 274 || ((int)m.WParam & 0xFFF0) != 61536)))
		{
			base.WndProc(ref m);
		}
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (_controller.IsPreview)
		{
			if (keyData == Keys.Escape)
			{
				_controller.Unlock();
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}
		Keys keys = keyData & Keys.KeyCode;
		if (keys >= Keys.A && keys <= Keys.Z)
		{
			_controller.HandleLetter((char)(65 + (keys - 65)));
			return true;
		}
		switch (keys)
		{
		case Keys.Back:
			_controller.HandleBackspace();
			return true;
		case Keys.Return:
			_controller.SubmitTyped();
			return true;
		default:
			return true;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_clockTimer != null)
			{
				_clockTimer.Stop();
				_clockTimer.Dispose();
				_clockTimer = null;
			}
			_background?.Dispose();
		}
		base.Dispose(disposing);
	}
}
