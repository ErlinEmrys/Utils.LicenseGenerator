using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Mime;
using System.Text.RegularExpressions;

using Erlin.Lib.Common;
using Erlin.Lib.Common.Threading;

namespace Erlin.Utils.LicenseGenerator;

public static partial class UrlResolver
{
	private static ConcurrentDictionary<string, UrlToLicense> UrlLicenses { get; } = new();

	private static HttpClient UrlClient { get; } = new(
		new UrlHttpRetryHandler(
			new HttpClientHandler
			{
				AllowAutoRedirect = true,
				MaxAutomaticRedirections = 5,
				ServerCertificateCustomValidationCallback =
					( _, _, _, _ ) => true,
			} ) )
	{
		Timeout = TimeSpan.FromSeconds( 5 ),
	};

	private static TaskWorker<PackageInfo> UrlWorker { get; } =
		new( "UrlLicenseRetriever", RetrieveUrlLicense, 1 );

	public static async Task AssignUrlLicences( IEnumerable<PackageInfo> packages )
	{
		foreach( PackageInfo fPackage in packages )
		{
			if( fPackage.LicenseDataType == LicenseDataType.Url )
			{
				UrlWorker.Enqueue( fPackage );
			}
		}

		await UrlWorker.WaitToFinish();

		ParallelHelper.ForEach(
			packages, fPackage =>
			{
				if( fPackage.LicenseDataType == LicenseDataType.Url )
				{
					fPackage.License = UrlLicenses[ fPackage.License! ].Text;
				}
			} );
	}

	private static async Task RetrieveUrlLicense( PackageInfo package, CancellationToken cancelToken )
	{
		UrlToLicense urlToLicense = new( package.License! );
		if( UrlLicenses.TryAdd( urlToLicense.UrlOriginal, urlToLicense ) )
		{
			while( urlToLicense.Status == UrlResolveStatus.Resolving )
			{
				await UrlResolver.ResolveUrl( urlToLicense, cancelToken );
			}
		}
	}

	private static async Task ResolveUrl( UrlToLicense urlToLicense, CancellationToken cancelToken )
	{
		Log.Inf( "Retrieving license: {Url}", urlToLicense.Url );

		using HttpRequestMessage request = new( HttpMethod.Get, urlToLicense.Url );
		using HttpResponseMessage response = await UrlResolver.UrlClient.SendAsync( request, cancelToken );
		if( !response.IsSuccessStatusCode )
		{
			urlToLicense.SetHttpStatus( response.StatusCode );
			Log.Wrn( "{RequestUri} failed due to {StatusCode}!", request.RequestUri, response.StatusCode );
			return;
		}

		if( response.Content.Headers.ContentType?.MediaType != MediaTypeNames.Text.Plain )
		{
			string replacedUrl = UrlResolver.GithubReplacement()
													.Replace(
															urlToLicense.Url, m =>
															{
																if( m.Success )
																{
																	string path = m.Groups[ 5 ].Value;
																	path = path.Replace(
																		"/blob/", "/", true,
																		CultureInfo.InvariantCulture );

																	return $"https://raw.githubusercontent.com{path}";
																}

																return string.Empty;
															} );

			urlToLicense.ReplaceUrl( replacedUrl );
			if( urlToLicense.Status != UrlResolveStatus.ReplacementLoop )
			{
				return;
			}
		}

		using MemoryStream fileStream = new();

		await response.Content.CopyToAsync( fileStream, cancelToken );
		fileStream.Seek( 0, SeekOrigin.Begin );

		using StreamReader reader = new( fileStream );

		string text = await reader.ReadToEndAsync( cancelToken );
		urlToLicense.SetLicense( text );
	}

	[GeneratedRegex(
		@"(http(s)?://)?(www.)?(github\.com)([a-zA-Z0-9/\.-_~].*)?", RegexOptions.IgnoreCase,
		"en-GB" )]
	private static partial Regex GithubReplacement();
}
