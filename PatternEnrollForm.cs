using System.Drawing;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class PatternEnrollForm : Form
{
	private readonly PatternPad _pad = new PatternPad();

	private readonly Label _instruction = new Label();

	private readonly int _grid;

	private string? _first;

	public string? Pattern { get; private set; }

	public PatternEnrollForm(int grid)
	{
		_grid = grid;
		Text = "AM PC Lock — set unlock pattern";
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.ClientSize = new Size(440, 540);
		BackColor = Color.FromArgb(24, 27, 38);
		ForeColor = Color.Gainsboro;
		_instruction.Text = $"Draw a pattern connecting at least {2} dots";
		_instruction.TextAlign = ContentAlignment.MiddleCenter;
		_instruction.SetBounds(0, 16, base.ClientSize.Width, 24);
		base.Controls.Add(_instruction);
		_pad.GridSize = _grid;
		_pad.SetBounds(30, 56, 380, 380);
		_pad.PatternCompleted += OnPattern;
		base.Controls.Add(_pad);
		Label value = new Label
		{
			Text = $"Grid {_grid}×{_grid}",
			ForeColor = Color.FromArgb(140, 155, 185),
			TextAlign = ContentAlignment.MiddleCenter,
			Bounds = new Rectangle(0, 440, base.ClientSize.Width, 20)
		};
		base.Controls.Add(value);
		Button button = new Button
		{
			Text = "Cancel",
			DialogResult = DialogResult.Cancel,
			Bounds = new Rectangle(330, 480, 80, 30)
		};
		base.Controls.Add(button);
		base.CancelButton = button;
	}

	private void OnPattern(string pattern)
	{
		if (_first == null)
		{
			_first = pattern;
			_instruction.Text = "Draw the same pattern again to confirm";
			_pad.Clear();
		}
		else if (_first == pattern)
		{
			Pattern = pattern;
			base.DialogResult = DialogResult.OK;
			Close();
		}
		else
		{
			_first = null;
			_instruction.Text = "Patterns did not match — start again";
			_pad.Clear();
		}
	}
}
