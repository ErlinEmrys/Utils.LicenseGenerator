using Erlin.Lib.Common;

using Newtonsoft.Json;

namespace Erlin.Utils.LicenseGenerator;

/// <summary>
/// Output writer
/// </summary>
public static class OutputWriter
{
	/// <summary>
	/// Writes output to JSON file
	/// </summary>
	public static async Task WriteOutputJson( ProgramArgs args, GeneratorResult result )
	{
		if( args.OutputJsonPath.IsEmpty() )
		{
			return;
		}

		string filePath = Path.Combine( args.SolutionPath, args.OutputJsonPath );

		Log.Inf( "Writing output JSON file {FilePath}", filePath );

		await using StreamWriter stream = new( filePath );
		await using JsonTextWriter writer = new( stream );

		writer.Formatting = Formatting.Indented;
		writer.Indentation = 1;
		writer.IndentChar = '\t';

		JsonSerializer serializer = new();
		serializer.NullValueHandling = NullValueHandling.Ignore;
		serializer.Serialize( writer, result );
	}

	/// <summary>
	/// Writes output to MD file
	/// </summary>
	public static async Task WriteOutputMD( ProgramArgs args, GeneratorResult result )
	{
		if( args.OutputMDPath.IsEmpty() )
		{
			return;
		}

		string filePath = Path.Combine( args.SolutionPath, args.OutputMDPath );

		Log.Inf( "Writing output MD file {FilePath}", filePath );

		await using StreamWriter stream = new( filePath );

		await stream.WriteHeader( "Third party licenses", '=' );
		await stream.WriteParagraph( "*This software stands on the shoulders of the following giants:*" );
		await stream.WriteLineAsync();

		foreach( PackageInfo fPackage in result.Packages )
		{
			await stream.WriteHeader( $"{fPackage.Name} [{fPackage.Version}]", '-', 1 );

			if( fPackage.Homepage.IsNotEmpty() )
			{
				await stream.WriteParagraph( $"Homepage: <{fPackage.Homepage}>", 1 );
			}

			if( fPackage.Authors.IsNotEmpty() )
			{
				await stream.WriteParagraph( $"Authors: {fPackage.Authors}", 1 );
			}

			if( fPackage.Copyright.IsNotEmpty() )
			{
				await stream.WriteParagraph( $"Copyright: {fPackage.Copyright}", 1 );
			}

			if( fPackage.License.IsNotEmpty() )
			{
				await stream.WriteLineAsync( "License:", 1 );

				switch( fPackage.LicenseDataType )
				{
					case LicenseDataType.Text:
						await stream.WriteComplex( fPackage.License, 2 );
						break;

					case LicenseDataType.Url:
						await stream.WriteLineAsync( $"<{fPackage.License}>", 2 );
						break;

					case LicenseDataType.Expression:
						string url = ExpressionLicenseResolver.Resolve( fPackage.License );
						await stream.WriteLineAsync( $"[{fPackage.License}]({url})", 2 );

						break;

					default:
						await stream.WriteLineAsync( $"{fPackage.LicenseDataType}: {fPackage.License}", 2 );
						break;
				}
			}

			if( fPackage.Notice.IsNotEmpty() )
			{
				await stream.WriteLineAsync( null, 1 );

				await stream.WriteLineAsync( "Notice:", 1 );

				await stream.WriteComplex( fPackage.Notice, 2 );
			}

			await stream.WriteLineAsync( null, 1 );

			await stream.WriteLineAsync();
		}
	}

	/// <summary>
	/// Writes header to MD
	/// </summary>
	private static async Task WriteHeader(
		this TextWriter stream, string text, char headerSeparator,
		int indentation = 0 )
	{
		await stream.WriteLineAsync( text, indentation );
		await stream.WriteLineAsync( new string( headerSeparator, text.Length ), indentation );
		await stream.WriteLineAsync( null, indentation );
	}

	/// <summary>
	/// Writes multiline text to MD
	/// </summary>
	private static async Task WriteComplex( this TextWriter stream, string text, int indentation = 0 )
	{
		string[] lines = text.Split( EnvHelper.NewLineBreakers, StringSplitOptions.None );

		foreach( string fLine in lines )
		{
			await stream.WriteLineAsync( fLine, indentation );
		}
	}

	/// <summary>
	/// Writes paragraph to MD
	/// </summary>
	private static async Task WriteParagraph( this TextWriter stream, string? text, int indentation = 0 )
	{
		await stream.WriteLineAsync( text, indentation );
		await stream.WriteLineAsync( null, indentation );
	}

	/// <summary>
	/// Writes text line to MD
	/// </summary>
	private static async Task WriteLineAsync( this TextWriter stream, string? text, int indentation )
	{
		await stream.WriteIndentation( indentation );
		await stream.WriteLineAsync( text );
	}

	/// <summary>
	/// Writes indentation characters to MD
	/// </summary>
	private static async Task WriteIndentation( this TextWriter stream, int indentation )
	{
		if( indentation > 0 )
		{
			await stream.WriteAsync( new string( '>', indentation ) );
			await stream.WriteAsync( " " );
		}
	}
}
