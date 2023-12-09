using System.Xml.Serialization;

namespace Erlin.Utils.LicenseGenerator;

/// <summary>
///    XML *.nuspec package representation
/// </summary>
[XmlRoot( ElementName = "package" )]
public class NuspecPackage
{
	[XmlElement( ElementName = "metadata" )]
	public NuspecMetadata? Metadata { get; set; }
}

/// <summary>
///    XML *.nuspec package metadata representation
/// </summary>
[XmlRoot( ElementName = "metadata" )]
public class NuspecMetadata
{
	[XmlElement( ElementName = "id" )]
	public string? Id { get; set; }

	[XmlElement( ElementName = "version" )]
	public string? Version { get; set; }

	[XmlElement( ElementName = "authors" )]
	public string? Authors { get; set; }

	[XmlElement( ElementName = "copyright" )]
	public string? Copyright { get; set; }

	[XmlElement( ElementName = "projectUrl" )]
	public string? ProjectUrl { get; set; }

	[XmlElement( ElementName = "license" )]
	public NuspecLicense? License { get; set; }

	[XmlElement( ElementName = "licenseUrl" )]
	public string? LicenseUrl { get; set; }

	[XmlElement( ElementName = "repository" )]
	public NuspecRepository? Repository { get; set; }
}

/// <summary>
///    XML *.nuspec package license data representation
/// </summary>
[XmlRoot( ElementName = "license" )]
public class NuspecLicense
{
	[XmlAttribute( AttributeName = "type" )]
	public string? Type { get; set; }

	[XmlText]
	public string? Text { get; set; }
}

/// <summary>
///    XML *.nuspec package repository data representation
/// </summary>
[XmlRoot( ElementName = "repository" )]
public class NuspecRepository
{
	[XmlAttribute( AttributeName = "url" )]
	public string? Url { get; set; }
}
