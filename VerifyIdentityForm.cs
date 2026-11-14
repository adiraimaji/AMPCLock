using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AMPCLock;

internal sealed class VerifyIdentityForm : Form
{
	private readonly AppConfig _cfg;

	private readonly PatternPad _pad = new PatternPad();

	private readonly Label _instruction = new Label();

	private readonly Label _status = new Label();

	private readonly Panel _altPanel = new Panel();

	private readonly Button _cancel = new Button();

	private readonly Button _verifyAlt = new Button();

	private readonly TextBox _code = new TextBox();

	private readonly TextBox _answer = new TextBox();

	private readonly List<int> _typed = new List<int>();

	private readonly char[] _labels;

	public VerifyIdentityForm(AppConfig cfg)
	{
		_cfg = cfg;
		Text = "AM PC Lock — confirm your identity";
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.KeyPreview = true;
		base.ClientSize = new Size(460, 550);
		BackColor = Color.FromArgb(24, 27, 38);
		ForeColor = Color.Gainsboro;
		_instruction.Text = "Draw your pattern, or type the letters on the dots and press Enter";
		_instruction.TextAlign = ContentAlignment.MiddleCenter;
		_instruction.SetBounds(0, 12, base.ClientSize.Width, 24);
		base.Controls.Add(_instruction);
		_pad.GridSize = cfg.ActiveGrid;
		_labels = CreateLabels(cfg.ActiveGrid);
		_pad.SetLabels(_labels);
		_pad.SetBounds(40, 44, 380, 380);
		_pad.PatternCompleted += OnPattern;
		base.Controls.Add(_pad);
		_status.SetBounds(0, 430, base.ClientSize.Width, 22);
		_status.TextAlign = ContentAlignment.MiddleCenter;
		_status.ForeColor = Color.FromArgb(160, 180, 210);
		base.Controls.Add(_status);
		Button button = new Button
		{
			Text = "Use recovery code / security answer",
			Bounds = new Rectangle(110, 458, 240, 30),
			Enabled = (!string.IsNullOrEmpty(cfg.RecoveryHash) || !string.IsNullOrEmpty(cfg.SecurityAnswerHash))
		};
		button.Click += delegate
		{
			ToggleAlternative();
		};
		base.Controls.Add(button);
		_altPanel.SetBounds(10, 496, 440, 150);
		_altPanel.Visible = false;
		base.Controls.Add(_altPanel);
		_altPanel.Controls.Add(new Label
		{
			Text = "Recovery code:",
			Bounds = new Rectangle(10, 6, 140, 20)
		});
		_code.SetBounds(150, 2, 280, 26);
		_code.Font = new Font("Consolas", 11f);
		_code.CharacterCasing = CharacterCasing.Upper;
		_code.Enabled = !string.IsNullOrEmpty(cfg.RecoveryHash);
		_altPanel.Controls.Add(_code);
		_altPanel.Controls.Add(new Label
		{
			Text = (string.IsNullOrWhiteSpace(cfg.SecurityQuestion) ? "Security question: (not set)" : ("Security question: " + cfg.SecurityQuestion)),
			Bounds = new Rectangle(10, 34, 420, 34)
		});
		_altPanel.Controls.Add(new Label
		{
			Text = "Answer:",
			Bounds = new Rectangle(10, 72, 140, 20)
		});
		_answer.SetBounds(150, 68, 280, 26);
		_answer.Font = new Font("Segoe UI", 10f);
		_answer.UseSystemPasswordChar = true;
		_answer.Enabled = !string.IsNullOrEmpty(cfg.SecurityAnswerHash);
		_altPanel.Controls.Add(_answer);
		_verifyAlt.Text = "Verify";
		_verifyAlt.SetBounds(150, 104, 100, 30);
		_verifyAlt.Click += delegate
		{
			VerifyAlternative();
		};
		_altPanel.Controls.Add(_verifyAlt);
		_cancel.Text = "Cancel";
		_cancel.DialogResult = DialogResult.Cancel;
		_cancel.SetBounds(350, 500, 90, 30);
		base.Controls.Add(_cancel);
		base.CancelButton = _cancel;
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		_pad.Focus();
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (_altPanel.Visible || (keyData & (Keys.Control | Keys.Alt)) != 0)
		{
			return base.ProcessCmdKey(ref msg, keyData);
		}
		Keys key = keyData & Keys.KeyCode;
		if (key >= Keys.A && key <= Keys.Z)
		{
			HandleLetter((char)(int)key);
			return true;
		}
		switch (key)
		{
		case Keys.Back:
			HandleBackspace();
			return true;
		case Keys.Delete:
			_typed.Clear();
			ShowTypedProgress();
			return true;
		case Keys.Enter:
			SubmitTyped();
			return true;
		default:
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}

	private void HandleLetter(char letter)
	{
		int num = Array.IndexOf(_labels, char.ToUpperInvariant(letter));
		if (num < 0 || _typed.Count >= _labels.Length)
		{
			return;
		}
		if (_typed.Count == 0)
		{
			_pad.Clear();
		}
		_typed.Add(num);
		ShowTypedProgress();
	}

	private void HandleBackspace()
	{
		if (_typed.Count != 0)
		{
			_typed.RemoveAt(_typed.Count - 1);
			ShowTypedProgress();
		}
	}

	private void SubmitTyped()
	{
		if (_typed.Count < PatternHasher.MinDots)
		{
			_status.ForeColor = Color.FromArgb(160, 180, 210);
			_status.Text = "Type at least " + PatternHasher.MinDots + " letters, then press Enter";
			return;
		}
		string pattern = PatternHasher.Serialize(_typed);
		_typed.Clear();
		_pad.Clear();
		OnPattern(pattern);
	}

	private void ShowTypedProgress()
	{
		if (_typed.Count == 0)
		{
			_status.Text = "";
			return;
		}
		_status.ForeColor = Color.FromArgb(160, 180, 210);
		_status.Text = new string('*', _typed.Count) + "  ·  Enter to confirm, Backspace to undo";
	}

	private static char[] CreateLabels(int grid)
	{
		char[] array = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
		for (int num = array.Length - 1; num > 0; num--)
		{
			int num2 = Random.Shared.Next(num + 1);
			char c = array[num];
			array[num] = array[num2];
			array[num2] = c;
		}
		char[] array2 = new char[grid * grid];
		Array.Copy(array, array2, array2.Length);
		return array2;
	}

	private void ToggleAlternative()
	{
		_altPanel.Visible = !_altPanel.Visible;
		_typed.Clear();
		_instruction.Text = (_altPanel.Visible ? "Enter your recovery code or security answer" : "Draw your pattern, or type the letters on the dots and press Enter");
		_status.Text = "";
		_status.ForeColor = Color.FromArgb(160, 180, 210);
		base.ClientSize = new Size(460, _altPanel.Visible ? 710 : 550);
		_cancel.Top = base.ClientSize.Height - 50;
		base.AcceptButton = (_altPanel.Visible ? _verifyAlt : null);
		if (_altPanel.Visible)
		{
			if (_code.Enabled)
			{
				_code.Focus();
			}
			else
			{
				_answer.Focus();
			}
		}
		else
		{
			_pad.Focus();
		}
	}

	private void OnPattern(string pattern)
	{
		_typed.Clear();
		if (_cfg.VerifyPattern(pattern))
		{
			Succeed();
			return;
		}
		_status.Text = "Wrong pattern — try again";
		_status.ForeColor = Color.FromArgb(255, 138, 138);
		_pad.Clear();
	}

	private void VerifyAlternative()
	{
		bool num = _cfg.VerifyRecovery(_code.Text);
		bool flag = _cfg.VerifySecurityAnswer(_answer.Text);
		if (num || flag)
		{
			Succeed();
			return;
		}
		_status.Text = "Not accepted — check the code or answer";
		_status.ForeColor = Color.FromArgb(255, 138, 138);
	}

	private void Succeed()
	{
		base.DialogResult = DialogResult.OK;
		Close();
	}
}
