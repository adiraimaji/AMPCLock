using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace AMPCLock;

internal static class SelfTest
{
	public static int Run(AppConfig cfg)
	{
		bool ok = true;
		List<string> notes = new List<string>();
		var (text, salt) = PatternHasher.Create("0-1-2-5-8", 1000);
		Check("pattern verifies", PatternHasher.Verify("0-1-2-5-8", text, salt, 1000));
		Check("wrong pattern rejected", !PatternHasher.Verify("0-1-2-5-7", text, salt, 1000));
		Check("salt changes hash", PatternHasher.Create("0-1-2-5-8", 1000).Hash != text);
		AppConfig appConfig = new AppConfig
		{
			PatternIterations = 1000,
			GridSize = 4
		};
		appConfig.SetPattern("0-1-2-5-8");
		appConfig.SetRecovery("ABCD-EFGH-JKLM");
		Check("config pattern verify", appConfig.VerifyPattern("0-1-2-5-8"));
		Check("config pattern reject", !appConfig.VerifyPattern("1-2-3-4"));
		Check("recovery ignores separators/case", appConfig.VerifyRecovery("abcd efgh jklm"));
		Check("recovery reject altered", !appConfig.VerifyRecovery("ABCD-EFGH-JKLN"));
		appConfig.SetSecurityAnswer("Blue Dog");
		Check("security answer normalized", appConfig.VerifySecurityAnswer("  blue   dog "));
		Check("security answer reject", !appConfig.VerifySecurityAnswer("blue cat"));
		AppConfig appConfig2 = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(appConfig));
		Check("config round-trip", appConfig2.VerifyPattern("0-1-2-5-8") && appConfig2.GridSize == 4 && appConfig2.VerifySecurityAnswer("blue dog"));
		AppConfig resetConfig = new AppConfig
		{
			PatternIterations = 1000,
			PatternGridSize = 4,
			SecurityQuestion = "Test question"
		};
		resetConfig.SetPattern("0-1-2-5-8");
		resetConfig.SetRecovery("ABCD-EFGH-JKLM");
		resetConfig.SetSecurityAnswer("Blue Dog");
		resetConfig.ResetCredentials();
		Check("reset clears stored credentials", string.IsNullOrEmpty(resetConfig.PatternHash) && string.IsNullOrEmpty(resetConfig.PatternSalt) && string.IsNullOrEmpty(resetConfig.RecoveryHash) && string.IsNullOrEmpty(resetConfig.RecoverySalt) && string.IsNullOrEmpty(resetConfig.SecurityAnswerHash) && string.IsNullOrEmpty(resetConfig.SecurityAnswerSalt) && string.IsNullOrEmpty(resetConfig.SecurityQuestion));
		Check("reset returns to first-run state", resetConfig.ActiveGrid == 3 && !resetConfig.VerifyPattern("0-1-2-5-8") && !resetConfig.VerifyRecovery("ABCD-EFGH-JKLM") && !resetConfig.VerifySecurityAnswer("Blue Dog"));
		SystemState systemState = JsonSerializer.Deserialize<SystemState>(JsonSerializer.Serialize(new SystemState
		{
			TaskbarHidden = true,
			Policies = 
			{
				new RegistrySnapshot
				{
					Path = "Software\\Test",
					Name = "DisableTaskmgr",
					Existed = true,
					Value = 0
				},
				new RegistrySnapshot
				{
					Path = "Software\\Test",
					Name = "NoClose",
					Existed = false
				}
			}
		}));
		Check("journal round-trip", systemState.Policies.Count == 2 && systemState.TaskbarHidden);
		Check("config object intact", cfg.PatternIterations > 0);
		try
		{
			PatternPad pad = new PatternPad
			{
				GridSize = 3,
				Size = new Size(400, 400)
			};
			string captured = null;
			pad.PatternCompleted += delegate(string p)
			{
				captured = p;
			};
			BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
			Mouse("OnMouseDown", 20, 20);
			Mouse("OnMouseMove", 200, 20);
			Mouse("OnMouseMove", 380, 20);
			Mouse("OnMouseMove", 380, 200);
			Mouse("OnMouseMove", 380, 380);
			Mouse("OnMouseUp", 380, 380);
			Check("pad drag produces path 0-1-2-5-8", captured == "0-1-2-5-8");
			captured = null;
			Mouse("OnMouseDown", 20, 20);
			Mouse("OnMouseMove", 200, 20);
			Mouse("OnMouseUp", 200, 20);
			Check("pad accepts two-dot path", captured == "0-1");
			captured = null;
			Mouse("OnMouseDown", 20, 20);
			Mouse("OnMouseUp", 20, 20);
			Check("pad ignores single dot", captured == null);
			captured = null;
			Mouse("OnMouseDown", 5, 5);
			Mouse("OnMouseUp", 5, 5);
			Check("pad ignores off-dot start", captured == null);
			pad.Dispose();
			void Mouse(string method, int x, int y)
			{
				typeof(PatternPad).GetMethod(method, flags)?.Invoke(pad, new object[1]
				{
					new MouseEventArgs(MouseButtons.Left, 1, x, y, 0)
				});
			}
		}
		catch (Exception ex)
		{
			Check("pad interaction (exception: " + ex.Message + ")", condition: false);
		}
		string text2 = string.Join(Environment.NewLine, notes);
		ConfigStore.Log("selftest " + (ok ? "PASS" : "FAIL") + Environment.NewLine + text2);
		return (!ok) ? 1 : 0;
		void Check(string name, bool condition)
		{
			notes.Add((condition ? "PASS" : "FAIL") + " " + name);
			ok &= condition;
		}
	}
}
