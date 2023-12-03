using System.Net;

namespace Erlin.Utils.LicenseGenerator;

public class UrlToLicense
{
	private HashSet<string> Probed { get; } = new();

	public string UrlOriginal { get; }

	public UrlResolveStatus Status { get; private set; }

	public string Url { get; private set; }

	public string? Text { get; private set; }

	public UrlToLicense( string url )
	{
		Status = UrlResolveStatus.Resolving;
		UrlOriginal = Url = url;
		Probed.Add( Url );
	}

	public void ReplaceUrl( string url )
	{
		if( Probed.Contains( url ) )
		{
			Status = UrlResolveStatus.ReplacementLoop;
		}
		else
		{
			Url = url;
			Probed.Add( Url );
		}
	}

	public void SetLicense( string text )
	{
		Text = text;
		Status = UrlResolveStatus.Resolved;
	}

	public void SetHttpStatus( HttpStatusCode httpStatus )
	{
		Status = UrlResolveStatus.HttpStatusCode;
	}
}
