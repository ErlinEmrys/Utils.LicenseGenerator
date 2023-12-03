namespace Erlin.Utils.LicenseGenerator;

/// <summary>
/// Resolver for license expressions
/// </summary>
public static class ExpressionLicenseResolver
{
	/// <summary>
	/// Resolve license by expression
	/// </summary>
	public static string Resolve( string expression )
	{
		return $"https://spdx.org/licenses/{expression}.html";
	}
}
