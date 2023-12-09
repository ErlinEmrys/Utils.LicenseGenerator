namespace Erlin.Utils.LicenseGenerator;

/// <summary>
///    Result of the license generator
/// </summary>
public class GeneratorResult
{
	/// <summary>
	///    Path to packages cache
	/// </summary>
	required public string PackagesPath { get; set; }

	/// <summary>
	///    List of packages that project depends upon
	/// </summary>
	public List<PackageInfo> Packages { get; } = [];

	/// <summary>
	///    Adds packages to this result object
	/// </summary>
	public void AddPackages( List<PackageInfo> list )
	{
		foreach( PackageInfo fPackage in list )
		{
			fPackage.Parent = this;
			Packages.Add( fPackage );
		}
	}
}
