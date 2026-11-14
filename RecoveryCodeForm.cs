using System.Drawing;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class RecoveryCodeForm : Form
{
	public RecoveryCodeForm(string code)
	{
		string code2 = code;
		Text = "AM PC Lock — recovery code";
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.ClientSize = new Size(440, 210);
		BackColor = Color.FromArgb(24, 27, 38);
		ForeColor = Color.Gainsboro;
		base.Controls.Add(new Label
		{
			Text = "Write this code down and keep it somewhere safe.\r\nIt is the only way back in if you forget your pattern.",
			Bounds = new Rectangle(20, 16, 400, 44)
		});
		TextBox value = new TextBox
		{
			Text = code2,
			ReadOnly = true,
			Font = new Font("Consolas", 16f, FontStyle.Bold),
			TextAlign = HorizontalAlignment.Center,
			Bounds = new Rectangle(20, 74, 400, 38)
		};
		base.Controls.Add(value);
		Button button = new Button
		{
			Text = "Copy",
			Bounds = new Rectangle(250, 132, 80, 30)
		};
		button.Click += delegate
		{
			Clipboard.SetText(code2);
		};
		base.Controls.Add(button);
		Button button2 = new Button
		{
			Text = "I saved it",
			DialogResult = DialogResult.OK,
			Bounds = new Rectangle(340, 132, 80, 30)
		};
		base.Controls.Add(button2);
		base.AcceptButton = button2;
	}
}
