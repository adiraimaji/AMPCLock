using System.Drawing;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class PreviewForm : Form
{
	public PreviewForm(AppConfig cfg)
	{
		Text = "AM PC Lock — preview (Esc closes, nothing is blocked)";
		base.ClientSize = new Size(980, 640);
		base.StartPosition = FormStartPosition.CenterScreen;
		BackColor = Color.FromArgb(14, 16, 24);
		PatternPad patternPad = new PatternPad
		{
			GridSize = cfg.ActiveGrid,
			Size = new Size(340, 340)
		};
		patternPad.Location = new Point((base.ClientSize.Width - patternPad.Width) / 2, 120);
		base.Controls.Add(patternPad);
		Label value = new Label
		{
			Text = "AM PC LOCK",
			ForeColor = Color.FromArgb(238, 245, 255),
			Font = new Font("Segoe UI Semibold", 26f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleCenter,
			Bounds = new Rectangle(0, 30, base.ClientSize.Width, 50)
		};
		base.Controls.Add(value);
		Label value2 = new Label
		{
			Text = "Preview only — the real lock screen blocks input and covers every monitor",
			ForeColor = Color.FromArgb(150, 165, 195),
			Font = new Font("Segoe UI", 9.5f),
			TextAlign = ContentAlignment.MiddleCenter,
			Bounds = new Rectangle(0, patternPad.Bottom + 20, base.ClientSize.Width, 30)
		};
		base.Controls.Add(value2);
		base.KeyPreview = true;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Escape)
		{
			Close();
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}
}
