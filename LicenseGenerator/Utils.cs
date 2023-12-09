using System.Globalization;

namespace Erlin.Utils.LicenseGenerator;

/// <summary>
///    Simple utilities
/// </summary>
public static class Utils
{
	/// <summary>
	///    Makes text lowercase
	/// </summary>
	public static string ToLower( string text )
	{
		return text.ToLower( CultureInfo.InvariantCulture );
	}

	/// <summary>
	///    Check if entered text is valid URL
	/// </summary>
	public static bool CheckUrl( string text )
	{
		return Uri.TryCreate( text, UriKind.Absolute, out Uri? uriResult )
			&& ( ( uriResult.Scheme == Uri.UriSchemeHttp ) || ( uriResult.Scheme == Uri.UriSchemeHttps ) );
	}
}
