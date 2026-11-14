using System;
using System.Collections.Generic;

namespace AMPCLock;

public sealed class SystemState
{
	public List<RegistrySnapshot> Policies { get; set; } = new List<RegistrySnapshot>();


	public bool TaskbarHidden { get; set; }

	public DateTime LockedAtUtc { get; set; } = DateTime.UtcNow;

}
