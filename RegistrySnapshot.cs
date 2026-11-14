namespace AMPCLock;

public sealed class RegistrySnapshot
{
	public string Path { get; set; } = "";


	public string Name { get; set; } = "";


	public bool Existed { get; set; }

	public int Value { get; set; }
}
