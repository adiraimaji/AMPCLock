using System.Drawing;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class RecoveryForm : Form
{
	private readonly TextBox _code = new TextBox();

	private readonly TextBox _answer = new TextBox();

	private readonly Label _error = new Label();

	public bool CodeRejected { get; private set; }

	public RecoveryForm(LockController controller)
	{
		LockController controller2 = controller;
		Text = "AM PC Lock — unlock with recovery";
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.ClientSize = new Size(470, 330);
		BackColor = Color.FromArgb(24, 27, 38);
		ForeColor = Color.Gainsboro;
		base.Controls.Add(new Label
		{
			Text = "Enter your recovery code, or answer your security question.",
			Bounds = new Rectangle(20, 14, 430, 22)
		});
		base.Controls.Add(new Label
		{
			Text = "Recovery code:",
			Bounds = new Rectangle(20, 44, 430, 20)
		});
		_code.SetBounds(20, 66, 430, 28);
		_code.Font = new Font("Consolas", 12f);
		_code.CharacterCasing = CharacterCasing.Upper;
		_code.Enabled = controller2.HasRecoveryCode;
		if (!controller2.HasRecoveryCode)
		{
			_code.Text = "(no recovery code set)";
		}
		base.Controls.Add(_code);
		string securityQuestion = controller2.SecurityQuestion;
		base.Controls.Add(new Label
		{
			Text = (string.IsNullOrWhiteSpace(securityQuestion) ? "Security question: (not set)" : ("Security question: " + securityQuestion)),
			Bounds = new Rectangle(20, 104, 430, 40)
		});
		_answer.SetBounds(20, 146, 430, 28);
		_answer.Font = new Font("Segoe UI", 11f);
		_answer.Enabled = controller2.HasSecurityAnswer;
		base.Controls.Add(_answer);
		_error.SetBounds(20, 182, 430, 20);
		_error.ForeColor = Color.FromArgb(255, 138, 138);
		base.Controls.Add(_error);
		Button button = new Button
		{
			Text = "Unlock",
			Bounds = new Rectangle(250, 212, 90, 30)
		};
		Button button2 = new Button
		{
			Text = "Cancel",
			DialogResult = DialogResult.Cancel,
			Bounds = new Rectangle(350, 212, 90, 30)
		};
		base.Controls.Add(button);
		base.Controls.Add(button2);
		base.Controls.Add(new Label
		{
			Text = "Either method unlocks the PC. Both can be changed in Settings.",
			ForeColor = Color.FromArgb(150, 165, 195),
			Bounds = new Rectangle(20, 256, 430, 34)
		});
		base.AcceptButton = button;
		base.CancelButton = button2;
		button.Click += delegate
		{
			if (controller2.VerifyRecoveryOrAnswer(_code.Text, _answer.Text))
			{
				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				CodeRejected = true;
				_error.Text = "Not accepted — check the code or answer and try again.";
			}
		};
	}
}
