namespace Erlin.Utils.LicenseGenerator;

/// <summary>
/// Status of resolving of URL to license
/// </summary>
public enum UrlResolveStatus
{
	/// <summary>
	/// Error happened
	/// </summary>
	Error = 0,
	/// <summary>
	/// URL is being resolved
	/// </summary>
	Resolving = 1,
	/// <summary>
	/// URL has been successfully resolved
	/// </summary>
	Resolved = 2,
	/// <summary>
	/// HTTP returned invalid status code
	/// </summary>
	HttpStatusCode = 3,
	/// <summary>
	/// URL replacement leads to loop
	/// </summary>
	ReplacementLoop = 4,
}
