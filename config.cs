using System;
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
	[XmlAttribute] public string Prefix;
	[XmlAttribute] public string BlogTemplate;
	[XmlAttribute] public string EntryTemplate;
	[XmlAttribute] public string CommentsStub;
	[XmlAttribute] public string EntrySpecific;
	[XmlAttribute] public string ImageDirectory;
	[XmlAttribute] public string ThumbnailCommand;
	public string ConfigFile;
	public bool Verbose;
	
	static public int EntriesPerPage = 10;

	internal string ThumbnailCommandFileName;
	internal string ThumbnailCommandArguments;

	public bool Parse (string[] args)
	{
		for (int i = 0; i < args.Length; ++i) {
			string arg = args [i];
			switch (arg) {
			case "-h": case "--help":
				PrintHelp ();
				return false;
			case "-p": case "--prefix":
				if (NextArgument (args, ref i, ref Prefix))
					break;
				return false;
			case "-d": case "--blog-directory":
				if (NextArgument (args, ref i, ref BlogDirectory))
					break;
				return false;
			case "-b": case "--blog-template":
				if (NextArgument (args, ref i, ref BlogTemplate))
					break;
				return false;
			case "-e": case "--entry-template":
				if (NextArgument (args, ref i, ref EntryTemplate))
					break;
				return false;
			case "-x": case "--rss-filename":
				if (NextArgument (args, ref i, ref RSSFileName))
					break;
				return false;
			case "-t": case "--thumbnail-command":
				if (NextArgument (args, ref i, ref ThumbnailCommand))
					break;
				return false;
			case "-c": case "--config":
				if (NextArgument (args, ref i, ref ConfigFile))
					break;
				return false;
			case "-v": case "--verbose":
				Verbose = true;
				break;
			default:
				if (ExtractArgument ("-p", arg, ref Prefix))
					break;
				if (ExtractArgument ("--prefix", arg, ref Prefix))
					break;
				if (ExtractArgument ("-b", arg, ref BlogTemplate))
					break;
				if (ExtractArgument ("--blog-template", arg, ref BlogTemplate))
					break;
				if (ExtractArgument ("-e", arg, ref EntryTemplate))
					break;
				if (ExtractArgument ("--entry-template", arg, ref EntryTemplate))
					break;
				if (ExtractArgument ("-d", arg, ref BlogDirectory))
					break;
				if (ExtractArgument ("--blog-directory", arg, ref BlogDirectory))
					break;
				if (ExtractArgument ("-x", arg, ref RSSFileName))
					break;
				if (ExtractArgument ("--rss-filename", arg, ref RSSFileName))
					break;
				if (ExtractArgument ("-t", arg, ref ThumbnailCommand))
					break;
				if (ExtractArgument ("--thumbnail-command", arg, ref ThumbnailCommand))
					break;
				Error ("unrecognized option `{0}'", arg);
				return false;
			}
		}
		if (ThumbnailCommand != null) {
			ThumbnailCommandFileName  = ThumbnailCommand.Split (' ')[0];
			ThumbnailCommandArguments = ThumbnailCommand.Substring (ThumbnailCommandFileName.Length+1);
		}
		return true;
	}

	private static void PrintHelp ()
	{
		Console.WriteLine ("Usage: lb [OPTION]*");
		Console.WriteLine ("lb (Lame Blog) is a blog engine.");
		Console.WriteLine (@"
Options:
  -p, --prefix=DIR            Root directory for generated files.
  -d, --blog-directory=DIR    Where to find blog entry files (*.txt, *.html).
  -b, --blog-template=FILE    Blog template file .
  -e, --entry-template=FILE   Entry template file.
  -x, --rss-filename=FILE     Basename for RSS filename.
  -t, --thumbnail-command=CMD Command to use to generate thumbnails.
                                {0} is the input file.
                                {1} is the input file.
  -h, --help                  Display this message and exit.
");
	}

	private void Error (string format, params object[] args)
	{
		Console.Write ("lb: ");
		Console.WriteLine (format, args);
		Console.WriteLine ("Try `lb --help' for more information.");
	}

	private bool ExtractArgument (string prefix, string argument, ref string value)
	{
		if (argument.Length - 1 <= prefix.Length)
			return false;
		if (!argument.StartsWith (prefix))
			return false;

		char delim = argument [prefix.Length];
		if (delim != '=' && delim != ':')
			return false;

		value = argument.Substring (prefix.Length+1);
		return true;
	}

	private bool NextArgument (string[] args, ref int i, ref string value)
	{
		if ((i+1) >= args.Length) {
			Error ("missing argument for `{0}'", args [i]);
			return false;
		}
		value = args [++i];
		return true;
	}
}
