﻿using System.Diagnostics;
using System.Globalization;

using CommandLine;

using Erlin.Lib.Common;

using Serilog;

using Log = Erlin.Lib.Common.Log;

namespace Erlin.Utils.LicenseGenerator;

/// <summary>
/// Main program
/// </summary>
public static class Program
{
	public const int PRG_EXIT_OK = 0;
	public const int PRG_EXIT_LOG_INIT = 1;
	public const int PRG_EXIT_CONSOLE_ERROR = 2;
	public const int PRG_EXIT_LOG_FATAL = 3;
	public const int PRG_EXIT_ARGUMENTS_ERROR = 4;
	public const int PRG_EXIT_PACKAGES_ERROR = 5;

	/// <summary>
	/// Entry point
	/// </summary>
	/// <param name="args">Command line arguments</param>
	public static async Task<int> Main( string[] args )
	{
		try
		{
			return await Program.Run( args );
		}
		catch( Exception e )
		{
			try
			{
				await Console.Error.WriteLineAsync( $"Critical unhandled exception {e}" );

				if( Debugger.IsAttached )
				{
					Debugger.Break();
				}

				return PRG_EXIT_LOG_INIT;
			}
			catch
			{
				return PRG_EXIT_CONSOLE_ERROR;
			}
		}
	}

	/// <summary>
	/// Logging and error handling
	/// </summary>
	private static async Task<int> Run( IEnumerable<string> args )
	{
		LoggerConfiguration logConfig = new();
#if DEBUG
		logConfig.MinimumLevel.Debug();
#endif
		logConfig.WriteTo.Console(
						theme: Log.DefaultConsoleColorTheme, outputTemplate: Log.DefaultOutputTemplate,
						formatProvider: CultureInfo.InvariantCulture )
				.Enrich.With<ExceptionLogEnricher>();

		Log.Initialize( logConfig.CreateLogger() );

		try
		{
			ParserResult<ProgramArgs>? parsedArgs = Parser.Default.ParseArguments<ProgramArgs>( args );
			return await parsedArgs.MapResult(
				Program.RunApp, errors =>
				{
					foreach( Error fArgError in errors )
					{
						switch( fArgError )
						{
							case TokenError tokenError:
								Log.Err(
									"Command line argument error: {Token} {Tag}", tokenError.Token, fArgError.Tag );

								break;

							case NamedError namedError:
								Log.Err(
									"Command line argument error: {Name} {Tag}", namedError.NameInfo.NameText,
									fArgError.Tag );

								break;

							default:
								Log.Err( "Command line argument error: {Tag}", fArgError.Tag );
								break;
						}
					}

					return Task.FromResult( PRG_EXIT_ARGUMENTS_ERROR );
				} );
		}
		catch( Exception e )
		{
			Log.Fatal( e );
			return PRG_EXIT_LOG_FATAL;
		}
		finally
		{
			await Log.DisposeAsync();
		}
	}

	/// <summary>
	/// Application
	/// </summary>
	private static async Task<int> RunApp( ProgramArgs args )
	{
		if( args.SolutionPath.IsEmpty() )
		{
			args.SolutionPath = Path.GetFullPath( Path.Combine( Directory.GetCurrentDirectory(), @"..\..\..\" ) );
		}

		Directory.SetCurrentDirectory( args.SolutionPath );

		GeneratorResult result = await PackageResolver.ResolvePackages();

		await OutputWriter.WriteOutputMD( args, result );

		await OutputWriter.WriteOutputJson( args, result );

		return result.Packages.Any(
			p => p.LicenseDataType is LicenseDataType.EnumEmptyError or LicenseDataType.Error )
			? PRG_EXIT_PACKAGES_ERROR : PRG_EXIT_OK;
	}
}
