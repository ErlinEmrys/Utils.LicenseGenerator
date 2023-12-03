using Newtonsoft.Json;

namespace Erlin.Utils.LicenseGenerator;

/// <summary>
/// Information about package
/// </summary>
public class PackageInfo
{
	/// <summary>
	/// Package ID
	/// </summary>
	[JsonIgnore]
	public string Id
	{
		get { return $"{Name}@{Version}"; }
	}

	/// <summary>
	/// Name of the package
	/// </summary>
	required public string Name { get; set; }

	/// <summary>
	/// Version of the package
	/// </summary>
	required public string Version { get; set; }

	/// <summary>
	/// Authors of the package
	/// </summary>
	public string? Authors { get; set; }

	/// <summary>
	/// Copyright notice of the package
	/// </summary>
	public string? Copyright { get; set; }

	/// <summary>
	/// Package project homepage url
	/// </summary>
	public string? Homepage { get; set; }

	/// <summary>
	/// Data type of license
	/// </summary>
	public LicenseDataType LicenseDataType { get; set; }

	/// <summary>
	/// License data
	/// </summary>
	public string? License { get; set; }

	/// <summary>
	/// License notice
	/// </summary>
	public string? Notice { get; set; }

	/// <summary>
	/// Directory containing *.nuspec file for this package
	/// </summary>
	[JsonIgnore]
	public string? NuspecDirPath { get; set; }

	/// <summary>
	/// Parent result object
	/// </summary>
	[JsonIgnore]
	required public GeneratorResult Parent { get; set; }
}
