using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class PatternPad : Control
{
	private readonly List<int> _path = new List<int>();

	private bool _dragging;

	private PointF _cursor;

	private int _grid = 3;

	private char[] _labels = Array.Empty<char>();

	public bool InputEnabled { get; set; } = true;


	public int GridSize
	{
		get
		{
			return _grid;
		}
		set
		{
			_grid = Math.Clamp(value, 3, 5);
			Invalidate();
		}
	}

	private float DotRadius => Math.Max(12f, (float)Math.Min(base.Width, base.Height) * 0.045f);

	public event Action<string>? PatternCompleted;

	public void SetLabels(char[] labels)
	{
		_labels = labels;
		Invalidate();
	}

	public PatternPad()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		DoubleBuffered = true;
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
	}

	public void Clear()
	{
		_path.Clear();
		_dragging = false;
		Invalidate();
	}

	private PointF Dot(int index)
	{
		int num = Math.Min(base.Width - 40, base.Height - 40);
		PointF pointF = new PointF((float)(base.Width - num) / 2f, (float)(base.Height - num) / 2f);
		float num2 = (float)num / (float)(_grid - 1);
		int num3 = index / _grid;
		int num4 = index % _grid;
		return new PointF(pointF.X + (float)num4 * num2, pointF.Y + (float)num3 * num2);
	}

	private int HitTest(PointF p)
	{
		float num = DotRadius * 1.9f;
		for (int i = 0; i < _grid * _grid; i++)
		{
			PointF pointF = Dot(i);
			float num2 = pointF.X - p.X;
			float num3 = pointF.Y - p.Y;
			if (num2 * num2 + num3 * num3 <= num * num)
			{
				return i;
			}
		}
		return -1;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (InputEnabled && e.Button == MouseButtons.Left)
		{
			int num = HitTest(e.Location);
			if (num >= 0)
			{
				_path.Clear();
				_path.Add(num);
				_dragging = true;
				_cursor = e.Location;
				Invalidate();
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (InputEnabled && _dragging)
		{
			_cursor = e.Location;
			int num = HitTest(e.Location);
			if (num >= 0 && !_path.Contains(num))
			{
				_path.Add(num);
			}
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (_dragging)
		{
			_dragging = false;
			if (_path.Count >= 2)
			{
				string obj = PatternHasher.Serialize(_path);
				_path.Clear();
				Invalidate();
				this.PatternCompleted?.Invoke(obj);
			}
			else
			{
				_path.Clear();
				Invalidate();
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		float dotRadius = DotRadius;
		float width = Math.Max(3f, dotRadius * 0.75f);
		if (_path.Count > 0)
		{
			using Pen pen = new Pen(Color.FromArgb(210, 77, 163, 255), width)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round,
				LineJoin = LineJoin.Round
			};
			List<PointF> list = _path.Select(Dot).ToList();
			if (_dragging)
			{
				list.Add(_cursor);
			}
			if (list.Count > 1)
			{
				graphics.DrawLines(pen, list.ToArray());
			}
		}
		for (int i = 0; i < _grid * _grid; i++)
		{
			PointF pointF = Dot(i);
			bool flag = _path.Contains(i);
			using SolidBrush brush = new SolidBrush(flag ? Color.FromArgb(235, 77, 163, 255) : Color.FromArgb(235, 30, 35, 52));
			using Pen pen2 = new Pen(flag ? Color.FromArgb(255, 150, 205, 255) : Color.FromArgb(210, 74, 86, 122), flag ? 2.4f : 2f);
			float num = (flag ? (dotRadius * 1.5f) : dotRadius);
			graphics.FillEllipse(brush, pointF.X - num, pointF.Y - num, num * 2f, num * 2f);
			graphics.DrawEllipse(pen2, pointF.X - num, pointF.Y - num, num * 2f, num * 2f);
			if (_labels.Length == _grid * _grid)
			{
				using Font font = new Font("Segoe UI Semibold", Math.Max(9f, dotRadius * 1.4f), FontStyle.Bold, GraphicsUnit.Pixel);
				using StringFormat format = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};
				using SolidBrush brush2 = new SolidBrush(flag ? Color.White : Color.FromArgb(226, 236, 252));
				graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
				graphics.DrawString(_labels[i].ToString(), font, brush2, new RectangleF(pointF.X - num, pointF.Y - num, num * 2f, num * 2f), format);
			}
			else if (flag)
			{
				using SolidBrush brush3 = new SolidBrush(Color.White);
				float num2 = num * 0.35f;
				graphics.FillEllipse(brush3, pointF.X - num2, pointF.Y - num2, num2 * 2f, num2 * 2f);
			}
		}
	}
}
