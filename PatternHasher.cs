using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AMPCLock;

public static class PatternHasher
{
	public const int MinDots = 2;

	public static string Serialize(IEnumerable<int> path)
	{
		return string.Join('-', path);
	}

	public static (string Hash, string Salt) Create(string pattern, int iterations)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(16);
		return (Hash: Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pattern), bytes, iterations, HashAlgorithmName.SHA256, 32)), Salt: Convert.ToBase64String(bytes));
	}

	public static bool Verify(string pattern, string? hash, string? salt, int iterations)
	{
		if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
		{
			return false;
		}
		try
		{
			byte[] array = Convert.FromBase64String(hash);
			byte[] array2 = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pattern), Convert.FromBase64String(salt), iterations, HashAlgorithmName.SHA256, array.Length);
			return CryptographicOperations.FixedTimeEquals(array, array2);
		}
		catch
		{
			return false;
		}
	}

	public static bool VerifyPattern(this AppConfig cfg, string pattern)
	{
		return Verify(pattern, cfg.PatternHash, cfg.PatternSalt, cfg.PatternIterations);
	}

	public static bool VerifyRecovery(this AppConfig cfg, string code)
	{
		return Verify(NormalizeRecovery(code), cfg.RecoveryHash, cfg.RecoverySalt, cfg.PatternIterations);
	}

	public static string NormalizeRecovery(string code)
	{
		return new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
	}

	public static string NewRecoveryCode()
	{
		char[] array = new char[12];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[RandomNumberGenerator.GetInt32("ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Length)];
		}
		return $"{new string(array, 0, 4)}-{new string(array, 4, 4)}-{new string(array, 8, 4)}";
	}

	public static void SetPattern(this AppConfig cfg, string pattern)
	{
		(string Hash, string Salt) tuple = Create(pattern, cfg.PatternIterations);
		string item = tuple.Hash;
		string item2 = tuple.Salt;
		cfg.PatternHash = item;
		cfg.PatternSalt = item2;
	}

	public static void SetRecovery(this AppConfig cfg, string code)
	{
		(string Hash, string Salt) tuple = Create(NormalizeRecovery(code), cfg.PatternIterations);
		string item = tuple.Hash;
		string item2 = tuple.Salt;
		cfg.RecoveryHash = item;
		cfg.RecoverySalt = item2;
	}

	public static string NormalizeAnswer(string answer)
	{
		return string.Join(' ', answer.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
	}

	public static void SetSecurityAnswer(this AppConfig cfg, string answer)
	{
		(string Hash, string Salt) tuple = Create(NormalizeAnswer(answer), cfg.PatternIterations);
		string item = tuple.Hash;
		string item2 = tuple.Salt;
		cfg.SecurityAnswerHash = item;
		cfg.SecurityAnswerSalt = item2;
	}

	public static bool VerifySecurityAnswer(this AppConfig cfg, string answer)
	{
		if (string.IsNullOrWhiteSpace(answer))
		{
			return false;
		}
		return Verify(NormalizeAnswer(answer), cfg.SecurityAnswerHash, cfg.SecurityAnswerSalt, cfg.PatternIterations);
	}

	public static void ResetCredentials(this AppConfig cfg)
	{
		cfg.PatternHash = null;
		cfg.PatternSalt = null;
		cfg.PatternGridSize = 0;
		cfg.RecoveryHash = null;
		cfg.RecoverySalt = null;
		cfg.SecurityQuestion = null;
		cfg.SecurityAnswerHash = null;
		cfg.SecurityAnswerSalt = null;
	}
}
