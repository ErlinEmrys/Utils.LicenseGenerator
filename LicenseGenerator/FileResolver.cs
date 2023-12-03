namespace Erlin.Utils.LicenseGenerator;

/// <summary>
/// Resolver for packaged files
/// </summary>
public static class FileResolver
{
	/// <summary>
	/// All common license file names
	/// </summary>
	private static string[] LicenseFileNames { get; } =
	{
		"LICENSE", "LICENCE",
	};

	/// <summary>
	/// All common notice file names
	/// </summary>
	private static string[] NoticeFileNames { get; } =
	{
		"NOTICE",
	};

	/// <summary>
	/// All commonly used file extensions
	/// </summary>
	private static string[] FileExtensions { get; } =
	{
		string.Empty, ".md", ".txt",
	};

	/// <summary>
	/// Attempt to retrieve content of LICENSE file
	/// </summary>
	public static string? GetLicenseFile( string? nugetPath, string? licenseFilePath )
	{
		return GetFile( nugetPath, licenseFilePath, FileResolver.LicenseFileNames, FileResolver.FileExtensions );
	}

	/// <summary>
	/// Attempt to retrieve content of NOTICE file
	/// </summary>
	public static string? GetNoticeFile( string? nugetPath )
	{
		return GetFile( nugetPath, null, FileResolver.NoticeFileNames, FileResolver.FileExtensions );
	}

	/// <summary>
	/// Attempt to retrieve content of a file
	/// </summary>
	private static string? GetFile(
		string? nugetPath, string? filePath, IEnumerable<string> fileNames, IEnumerable<string> fileExtensions )
	{
		ArgumentException.ThrowIfNullOrEmpty( nugetPath );

		if( filePath.IsNotEmpty() )
		{
			filePath = Path.Combine( nugetPath, filePath );
			if( File.Exists( filePath ) )
			{
				return File.ReadAllText( filePath );
			}
		}

		foreach( string fFileName in fileNames )
		{
			foreach( string fFileExt in fileExtensions )
			{
				filePath = Path.Combine( nugetPath, fFileName + fFileExt );
				if( File.Exists( filePath ) )
				{
					return File.ReadAllText( filePath );
				}
			}
		}

		return null;
	}
}
