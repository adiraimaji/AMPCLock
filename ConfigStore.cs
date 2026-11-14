using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMPCLock;

public static class ConfigStore
{
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static string RootDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AM PC Lock");


	public static string ConfigPath => Path.Combine(RootDir, "config.json");

	public static string StatePath => Path.Combine(RootDir, "lockstate.json");

	public static string LogPath => Path.Combine(RootDir, "app.log");

	public static AppConfig Load()
	{
		try
		{
			if (File.Exists(ConfigPath))
			{
				AppConfig appConfig = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), Options);
				if (appConfig != null)
				{
					return appConfig;
				}
			}
		}
		catch (Exception ex)
		{
			Log("config load failed: " + ex.Message);
		}
		return new AppConfig();
	}

	public static void Save(AppConfig cfg)
	{
		Directory.CreateDirectory(RootDir);
		string text = ConfigPath + ".tmp";
		File.WriteAllText(text, JsonSerializer.Serialize(cfg, Options));
		File.Move(text, ConfigPath, overwrite: true);
	}

	public static void Log(string message)
	{
		try
		{
			Directory.CreateDirectory(RootDir);
			File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
		}
		catch
		{
		}
	}
}
