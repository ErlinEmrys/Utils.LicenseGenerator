using System.Globalization;
using System.Xml.Serialization;

using Erlin.Lib.Common;
using Erlin.Lib.Common.Exceptions;
using Erlin.Lib.Common.Threading;
using Erlin.Lib.Common.Xml;

using Newtonsoft.Json.Linq;

using SimpleExec;

namespace Erlin.Utils.LicenseGenerator;

/// <summary>
///    Class for resolving Nuget packages
/// </summary>
public static class PackageResolver
{
	private const string CMD = "dotnet";
	private const string CMD_ARGS_PATH = "nuget locals -l global-packages --force-english-output";
	private const string CMD_ARGS_LIST = "list package --include-transitive --format json";

	/// <summary>
	///    *.nuspec file serializer
	/// </summary>
	public static XmlSerializer NuspecSerializer { get; } = new( typeof( NuspecPackage ) );

	/// <summary>
	///    Resolves all packages of the project
	/// </summary>
	public static async Task<GeneratorResult> ResolvePackages()
	{
		// Retrieve physical path to packages
		string packagesPath = await PackageResolver.ResolvePackagesPath();
		GeneratorResult result = new()
		{
			PackagesPath = packagesPath
		};

		// Retrieve and parse all packages for a project
		string packagesListText = await PackageResolver.ExecuteCommand( CMD, CMD_ARGS_LIST );
		JObject json = JObject.Parse( packagesListText );
		PackageResolver.ConvertPackagesJson( result, json );

		// Reads all *.nuspec files for each found package
		ParallelHelper.ForEach( result.Packages, PackageResolver.ReadNuspecFile );

		return result;
	}

	/// <summary>
	///    Utility for resolving physical path to local packages cache
	/// </summary>
	private static async Task<string> ResolvePackagesPath()
	{
		string packagesPath = await PackageResolver.ExecuteCommand( CMD, CMD_ARGS_PATH );

		const string PATH_PREFIX = "global-packages:";
		if( packagesPath.IsEmpty()
			|| !packagesPath.StartsWith( PATH_PREFIX, true, CultureInfo.InvariantCulture ) )
		{
			throw new UnexpectedResultException(
				$"Unexpected output: {packagesPath} for command: {CMD} {CMD_ARGS_PATH}" );
		}

		packagesPath = packagesPath[ PATH_PREFIX.Length.. ].Trim();
		if( !Directory.Exists( packagesPath ) )
		{
			throw new UnexpectedResultException( $"Packages directory {packagesPath} not exist" );
		}

		Log.Inf( "Packages path resolved: {Path}", packagesPath );

		return packagesPath;
	}

	/// <summary>
	///    Deserialize *.nuspec file for selected package
	/// </summary>
	private static void ReadNuspecFile( PackageInfo package )
	{
		Log.Inf( "Reading package {PackageID}", package.Id );
		package.NuspecDirPath = Path.Combine(
			package.Parent.PackagesPath, Utils.ToLower( package.Name ),
			Utils.ToLower( package.Version ) );

		string nuspecFilePath = Path.Combine( package.NuspecDirPath, Utils.ToLower( package.Name ) + ".nuspec" );

		if( File.Exists( nuspecFilePath ) )
		{
			using StreamReader reader = new( nuspecFilePath );
			if( PackageResolver.NuspecSerializer.Deserialize( new IgnoreNamespaceXmlReader( reader ) ) is
				NuspecPackage
				nuspec )
			{
				PackageResolver.FillInfo( nuspec, package );
			}
		}
	}

	/// <summary>
	///    Reads info from *.nuspec package
	/// </summary>
	private static void FillInfo( NuspecPackage nuget, PackageInfo info )
	{
		if( nuget.Metadata != null )
		{
			PackageResolver.FillInfo( nuget.Metadata, info );
		}
	}

	/// <summary>
	///    Reads info from *.nuspec package metadata
	/// </summary>
	public static void FillInfo( NuspecMetadata nuget, PackageInfo info )
	{
		info.Authors = nuget.Authors;
		info.Copyright = nuget.Copyright;
		info.Homepage = nuget.ProjectUrl;
		if( info.Homepage.IsEmpty() )
		{
			info.Homepage = nuget.Repository?.Url;
		}

		info.Notice = FileResolver.GetNoticeFile( info.NuspecDirPath );
		PackageResolver.FillLicense( nuget, info );
	}

	/// <summary>
	///    Reads license data from *.nuspec package metadata
	/// </summary>
	private static void FillLicense( NuspecMetadata nuget, PackageInfo info )
	{
		if( nuget.License != null )
		{
			string? filePath = null;
			if( nuget.License.Type == "file" )
			{
				filePath = nuget.License.Text;
			}

			// 1. LICENSE file
			info.License = FileResolver.GetLicenseFile( info.NuspecDirPath, filePath );

			if( info.License.IsNotEmpty() )
			{
				info.LicenseDataType = LicenseDataType.Text;
			}
			else if( info.License.IsEmpty() && filePath.IsNotEmpty() )
			{
				info.LicenseDataType = LicenseDataType.Error;
				info.License =
					$"License specified as packaged file: {filePath}, but no such file found in package!";
			}
			else if( nuget.License.Type == "expression" )
			{
				// 2. Expression
				info.LicenseDataType = LicenseDataType.Expression;
				info.License = nuget.License.Text;
				if( info.License.IsEmpty() )
				{
					info.LicenseDataType = LicenseDataType.Error;
					info.License =
						"License specified as expression, but no expression provided by the package!";
				}
			}
		}

		if( ( info.LicenseDataType == LicenseDataType.EnumNullError ) && nuget.LicenseUrl.IsNotEmpty() )
		{
			// 3. URL
			info.LicenseDataType = LicenseDataType.Url;
			info.License = nuget.LicenseUrl;

			if( info.License.IsEmpty() )
			{
				info.LicenseDataType = LicenseDataType.Error;
				info.License =
					"License specified as URL, but no URL provided by the package!";
			}
			else if( !Utils.CheckUrl( info.License ) )
			{
				info.LicenseDataType = LicenseDataType.Error;
				info.License = "License specified as URL, but the URL is not valid: " + info.License;
			}
		}

		if( info.LicenseDataType == LicenseDataType.EnumNullError )
		{
			info.LicenseDataType = LicenseDataType.Error;
			info.License =
				$"Package does not contain a supported license: {nuget.License?.Type}/{nuget.License?.Text}";
		}
	}

	/// <summary>
	///    Reads basic packages info from JSON list
	/// </summary>
	private static void ConvertPackagesJson( GeneratorResult result, JObject json )
	{
		if( json[ "projects" ] is not JArray projects )
		{
			throw new UnexpectedResultException( "Packages JSON: Missing 'projects' array" );
		}

		Dictionary<string, PackageInfo> tempDic = new();
		foreach( JToken fProject in projects )
		{
			if( fProject[ "frameworks" ] is not JArray frameworks )
			{
				throw new UnexpectedResultException( "Packages JSON: Missing 'frameworks' array" );
			}

			foreach( JToken fFramework in frameworks )
			{
				JArray? topLevelPackages = fFramework.SelectToken( "topLevelPackages" ) as JArray;
				JArray? transitivePackages = fFramework.SelectToken( "transitivePackages" ) as JArray;

				PackageResolver.PackageArrToInfo( result, tempDic, topLevelPackages );
				PackageResolver.PackageArrToInfo( result, tempDic, transitivePackages );
			}
		}

		List<PackageInfo> list = tempDic.Values.ToList();
		list.Sort(
			( l, r ) =>
			{
				int comparison = string.Compare( l.Name, r.Name, StringComparison.OrdinalIgnoreCase );
				if( comparison == 0 )
				{
					comparison = string.Compare( l.Version, r.Version, StringComparison.OrdinalIgnoreCase );
				}

				return comparison;
			} );

		result.AddPackages( list );
	}

	/// <summary>
	///    Reads basic packages info from JSON list
	/// </summary>
	private static void PackageArrToInfo(
		GeneratorResult genResult, Dictionary<string, PackageInfo> result, JArray? array )
	{
		if( array != null )
		{
			foreach( JToken fPackage in array )
			{
				string? id = fPackage[ "id" ]?.Value<string>();
				string? version = fPackage[ "resolvedVersion" ]?.Value<string>();

				if( id.IsEmpty() || version.IsEmpty() )
				{
					throw new UnexpectedResultException(
						$"Packages JSON: Package missing 'id' or 'version': {fPackage}" );
				}

				PackageInfo info = new()
				{
					Name = id,
					Version = version,
					Parent = genResult
				};

				result.TryAdd( info.Id, info );
			}
		}
	}

	/// <summary>
	///    Executes shell command
	/// </summary>
	/// <param name="cmd">Command to execute</param>
	/// <param name="cmdArgs">Command arguments</param>
	/// <returns>Command result</returns>
	private static async Task<string> ExecuteCommand( string cmd, string cmdArgs )
	{
		int cmdErrorCode = 0;
		( string cmdOutput, string cmdError ) = await Command.ReadAsync(
			cmd, cmdArgs,
			handleExitCode: code =>
			{
				cmdErrorCode = code;
				return true;
			} );

		if( cmdErrorCode != 0 )
		{
			throw new UnexpectedResultException(
				$"CMD: {cmd} {cmdArgs}{Environment.NewLine}"
				+ $"ERROR: {cmdError}{Environment.NewLine}"
				+ $"OUTPUT: {cmdOutput}" );
		}

		return cmdOutput;
	}
}
