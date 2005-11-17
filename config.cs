using System.Xml.Serialization;

[XmlRoot("config")]
public class Config {
	[XmlAttribute] public string Author;
	[XmlAttribute] public string BlogDirectory;
	[XmlAttribute] public string BlogFileName;
	[XmlAttribute] public string BlogWebDirectory;
	[XmlAttribute] public string BlogImageBasedir;
	[XmlAttribute] public string Copyright;
	[XmlAttribute] public string Description;
	[XmlAttribute] public string ManagingEditor;
	[XmlAttribute] public string Link;
	[XmlAttribute] public string Title;
	[XmlAttribute] public string RSSFileName;
	[XmlAttribute] public string InputEncoding;
	[XmlAttribute] public string OutputEncoding;
	[XmlAttribute] public string AnalyticsStub;
}
