//
// Lame Blog 1.0
//
// Features:
//    Per-day entries
//    HTML and .txt files supported (pulls header from the file).
//    Include text support
//
//
// Template macros:
//
//	@BLOG_ENTRIES@
//	    The blob entries rendered
//
// TODO:
//   Add images, so I can do:
//   @image file
//   @caption Caption
//

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Globalization;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Rss;

class DayEntry : IComparable {
	public DateTime Date;
	public string Body;
	public string Caption = "";
	public string DateCaption;
	
	Blog blog;
	string extra = "";
	
	public string blog_base;

	//
	// Text to inline CSS for the blog for classes code, code-csharp and shell
	//
	const string code_style = "class=\"code\" style=\"border-style: solid; background: #ddddff; border-width: 1px; padding: 2pt;\"";
	const string code_csharp_style = "class=\"code-csharp\" style=\"border-style: solid; background: #ddddff; border-width: 1px; padding: 2pt;\"";
	const string shell_style = "style=\"border-style: solid; background: #000000; color: #bbbbbb; border-width: 1px; padding: 2pt;\"";

	//
	// The date when we started using filestamps instead of the hardcoded 4pm
	//
	public DateTime SwitchDate = new DateTime (2005, 05, 25, 0, 0, 8);
	
	DayEntry (Blog blog, string file)
	{
		this.blog = blog;
		blog_base = blog.config.BlogWebDirectory;
		ParseDate (file);

		using (FileStream i = File.OpenRead (file)){
			using (StreamReader s = new StreamReader (i, Encoding.GetEncoding (28591))){
				if (file.EndsWith (".html"))
					Load (s, true);
				else if (file.EndsWith (".txt"))
					Load (s, false);
			}
		}
	}

	public static DayEntry Load (Blog blog, string file)
	{
		DayEntry de = null;
		
		try {
			de = new DayEntry (blog, file);
		} catch {
			Console.WriteLine ("Failed to load file: {0}", file);
		}
		return de;
	}
	
	void ParseDate (string file)
	{
		int month;

		Match match = Regex.Match (file, "(200[0-9])/([a-z]+)-0*([0-9]+)(-[0-9])?");

		int year = Int32.Parse (file.Substring (match.Groups [1].Index, match.Groups [1].Length));
		int day = Int32.Parse (file.Substring (match.Groups [3].Index, match.Groups [3].Length));
		string month_name = file.Substring (match.Groups [2].Index, match.Groups [2].Length);
		extra = file.Substring (match.Groups [4].Index, match.Groups [4].Length);

		switch (month_name){
		case "jan":
			month = 1; break;
		case "feb":
			month = 2; break;
		case "mar":
			month = 3; break;
		case "apr":
			month = 4; break;
		case "may":
			month = 5; break;
		case "jun":
			month = 6; break;
		case "jul":
			month = 7; break;
		case "aug":
			month = 8; break;
		case "sep":
			month = 9; break;
		case "oct":
			month = 10; break;
		case "nov":
			month = 11; break;
		case "dec":
			month = 12; break;
		default:
			throw new Exception ("Unknown month: " + month_name + " from: " + file);
		}

		Date = new DateTime (year, month, day, 13, 55, 0);

		//
		// Start using the file time stamp as the publishing date, this is 
		// better than hardcoding a value.  To avoid people sending us long
		// rants, only do this after today
		//
		if (Date > SwitchDate){
			FileInfo fi = new FileInfo (file);
			DateTime access_date = fi.LastWriteTimeUtc;

			Console.WriteLine ("Hour: {0}", access_date.Hour);
			Date = new DateTime (year, month, day, access_date.Hour, access_date.Minute, 0);
		}
					     
		DateCaption = String.Format ("{0:dd} {0:MMM} {0:yyyy}", Date);
	}
	
	void Load (StreamReader i, bool is_html)
	{
		bool caption_found = false;
		bool in_pre = false;
		StringBuilder sb = new StringBuilder ();
		string s;
		
		while ((s = i.ReadLine ()) != null){
			if (!caption_found){
				if (is_html){
					if (s.StartsWith ("<h1>")){
						Caption = s.Replace ("<h1>", "").Replace ("</h1>", "");
						caption_found = true;
						continue;
					} else if (s.StartsWith ("#include")){
						sb.Append (Include (s.Substring (9), out Caption));
						caption_found = true;
						continue;
					}
				} else {
					if (s.StartsWith ("@") && !caption_found){
						Caption = s.Substring (1);
						caption_found = true;
						continue;
					}
				}
			}
			if (!is_html){
				if (s == "" && !in_pre)
					sb.Append ("<p>");
				else if (s.StartsWith ("@"))
					sb.Append (String.Format ("<h1>{0}</h1>", s.Substring (1)));
				else if (s.StartsWith ("#pre"))
					in_pre = true;
				else if (s.StartsWith ("#endpre"))
					in_pre = false;
				else
					sb.Append (s);
			} else {
				if (s.StartsWith ("#include")){
					string c;
					sb.Append (Include (s.Substring (9), out c));
					continue;
				} else if (s.StartsWith ("#pic")){
					int idx = s.IndexOf (",");
					if (idx == -1){
						Console.WriteLine ("Wrong #pic command");
						continue;
					}
					
					string filename = s.Substring (5, idx-5);
					string caption = s.Substring (idx + 1);
					sb.Append (String.Format ("<p><center><a href=\"{0}pic.php?name={1}&caption={2}\"><img border=0 src=\"{3}/pictures/small-{1}\"></a><p>{2}</center></p>", blog_base, filename, caption, blog.config.BlogImageBasedir));
					continue;
					
				}
				sb.Append (s);
			}
			sb.Append ("\n");
		}
		Body = sb.ToString ();
	}

	public int CompareTo (object o)
	{
		return Date.CompareTo (((DayEntry) o).Date);
	}

	string Include (string file, out string caption)
	{
		if (file.StartsWith ("~/")){
			file = Environment.GetEnvironmentVariable ("HOME") + "/" + file.Substring (2);
		}

		string article_file = "./texts/" + Path.GetFileName (file);
		File.Copy (file, article_file, true);
		article_file = article_file.Substring (1);

		//
		// Remove header stuff, and include inline, stick a copy 
		//
		StringBuilder r = new StringBuilder ();

		caption = "";

		Console.WriteLine ("Reading: " + file);
		using (FileStream i = File.OpenRead (file)){
			StreamReader s = new StreamReader (i, Encoding.GetEncoding (28591));
			string line;
			bool output = false;
			
			while ((line = s.ReadLine ()) != null){
				Match m = Regex.Match (line, "<title>(.*)</title>");
				if (m.Groups.Count > 1){
					caption = line.Substring (m.Groups [1].Index, m.Groups [1].Length);
					blog.AddArticle (blog_base + article_file, caption);
					continue;
				}
				if (!output){
					if (line == "<!--start-->"){
						output = true;
						r.Append (String.Format ("<h3>{0}: (<a href=\"{2}{1}\">Article Permalink</a>)</h3>", caption, article_file, blog_base));
					}
					continue;
				}
				line = Regex.Replace (line, "class=\"code\"", code_style);
				line = Regex.Replace (line, "class=\"code-csharp\"", code_csharp_style);
				line = Regex.Replace (line, "class=\"shell\"", shell_style);
				r.Append (line);
				r.Append ("\n");
			}
		}
		return r.ToString ();
	}

	public string PermaLink {
		get {
			return String.Format ("archive/{0:yyyy}/{0:MMM}-{0:dd}{1}.html", Date, extra);
		}
	}
}

class Blog {
	public Config config;
	ArrayList entries = new ArrayList ();

	public int Entries {
		get {
			return entries.Count;
		}
	}
	
	public Blog (Config config)
	{
		this.config = config;
		string [] years = Directory.GetDirectories (config.BlogDirectory);

		foreach (string year in years){
			string [] days = Directory.GetFiles (year);

			foreach (string file in days){
				if (!(file.EndsWith (".html") || file.EndsWith (".txt")))
					continue;

				DayEntry de = DayEntry.Load (this, file);
				if (de != null)
					entries.Add (de);
			}
		}

		Console.WriteLine ("Loaded: {0} days", entries.Count);

		entries.Sort ();
	}

	static DateTime LastDate = new DateTime (2004, 5, 19, 0, 0, 0);
	
	void Render (StreamWriter o, int idx, string blog_base, bool include_daily_anchor, bool include_navigation)
	{
		DayEntry d = (DayEntry) entries [idx];

		string anchor = HttpUtility.UrlEncode (d.Date.ToString ()).Replace ('%','-').Replace ('+', '-');
		if (include_daily_anchor || d.Date < LastDate)
			o.WriteLine (String.Format ("<a name=\"{0}\"></a>", anchor));
		
		if (include_navigation){
			DayEntry prev = (DayEntry) (idx > 0 ? entries [idx-1] : null);
			DayEntry next = (DayEntry) (idx+1 < Entries ? entries [idx+1] : null);

			o.WriteLine ("<p>");
			if (prev != null)
				o.WriteLine ("<a href=\"{0}{1}\">« {2}</a> | ", blog_base, prev.PermaLink, prev.Caption);
			o.WriteLine ("<a href=\"{0}{1}\">Main</a>", blog_base, config.BlogFileName);
			if (next != null)
				o.WriteLine ("| <a href=\"{0}{1}\">{2} »</a> ", blog_base, next.PermaLink, next.Caption);
			
			o.WriteLine ("<p>");
		}
		
		o.WriteLine ("<h3><a href=\"{2}/{0}\" class=\"entryTitle\">{3}</a></h3>",
			     d.PermaLink, d.DateCaption, blog_base, d.Caption);
		o.WriteLine ("<div class='blogentry'>" + d.Body + "</div>");
		o.WriteLine ("<div class='footer'>Posted by {2} on <a href=\"{0}{1}\">{3}</a></div><p>",
			     blog_base, d.PermaLink, config.Copyright, d.DateCaption);

	}
		     
	void Render (StreamWriter o, int start, int end, string blog_base, bool include_daily_anchor)
	{
		bool navigation = start + 1 == end;
		
		for (int i = start; i < end; i++){
			int idx = entries.Count - i - 1;
			if (idx < 0)
				return;
			
			Render (o, idx, blog_base, include_daily_anchor, navigation);
		}
	}

	void RenderArticleList (StreamWriter o)
	{
		foreach (Article a in articles){
			o.WriteLine ("<a href=\"{0}\">{1}</a><br>", a.url, a.caption);
		}
	}
	
	public void RenderHtml (string template, string output, int start, int end, string blog_base)
	{
		using (FileStream i = File.OpenRead (template), o = File.Create (output)){
			StreamReader s = new StreamReader (i, Encoding.GetEncoding (28591));
			StreamWriter w = new StreamWriter (o, Encoding.GetEncoding (28591));
			string line;

			while ((line = s.ReadLine ()) != null){
				switch (line){
				case "@BLOG_ENTRIES@":
					Render (w, start, end, blog_base, output == "all.html");
					break;
				case "@BLOG_ARTICLES@":
					RenderArticleList (w);
					break;

				default:
					line = line.Replace ("@BASEDIR@", blog_base);
					line = line.Replace ("@TITLE@", config.Title);
					line = line.Replace ("@DESCRIPTION@", config.Description);
					line = line.Replace ("@RSSFILENAME@", config.RSSFileName);
					line = line.Replace ("@EDITOR@", config.ManagingEditor);
					w.WriteLine (line);
					break;
				}

			}
			w.Flush ();
		}
	}

	public void RenderArchive (string template)
	{
		for (int i = 0; i < Entries; i++){
			DayEntry d = (DayEntry) entries [i];

			RenderHtml (template, d.PermaLink, Entries - i - 1, Entries - i, "../../");
		}
	}
	
	RssChannel MakeChannel ()
	{
		RssChannel c = new RssChannel ();

		c.Title = config.Title;
		c.Link = new Uri (config.BlogWebDirectory + "/" + config.BlogFileName);
		c.Description = config.Description;
		c.Copyright = config.Copyright;
		c.Generator = "lb#";
		c.ManagingEditor = config.ManagingEditor;
		c.PubDate = System.DateTime.Now;
		
		return c;
	}

	public void RenderRSS (RssVersion version, string output, int start, int end)
	{
		RssChannel channel = MakeChannel ();

		for (int i = start; i < end; i++){
			int idx = entries.Count - i - 1;
			if (idx < 0)
				continue;
			
			DayEntry d = (DayEntry) entries [idx];

			RssItem item = new RssItem ();
			item.Author = config.Author;
			item.Description = d.Body;
			item.Guid = new RssGuid ();
			item.Guid.Name = config.BlogWebDirectory + d.PermaLink;
			item.Link = new Uri (item.Guid.Name);
			item.Guid.PermaLink = DBBool.True;
			item.PubDate = d.Date;
			if (d.Caption == ""){
				Console.WriteLine ("No caption for: " + d.DateCaption);
				d.Caption = d.DateCaption;
			}
			item.Title = d.Caption;
			
			channel.Items.Add (item);
		}

		FileStream o = File.Create (output);
		RssWriter w = new RssWriter (o, new UTF8Encoding (false));

		w.Version = version;

		w.Write (channel);
		w.Close ();
	}

	public void RenderRSS (string output, int start, int end)
	{
		RenderRSS (RssVersion.RSS20, output + ".rss2", start, end);
	}

	public class Article {
		public string url, caption;

		public Article (string u, string c)
		{
			url = u;
			caption = c;
		}
	}
	
	ArrayList articles = new ArrayList ();

	public void AddArticle (string url, string caption)
	{
		articles.Add (new Article (url, caption));
	}
	

}

class LB {

	static void Main ()
	{
		Config config = (Config) 
			new XmlSerializer (typeof (Config)).Deserialize (new XmlTextReader ("config.xml"));
		if (config.BlogImageBasedir == null || config.BlogImageBasedir == "")
			config.BlogImageBasedir = config.BlogWebDirectory;
		
		Blog b = new Blog (config);

		b.RenderHtml ("template", config.BlogFileName, 0, 30, "");
		b.RenderHtml ("template", "all.html", 0, b.Entries, "");
		b.RenderArchive ("template");
		
		b.RenderRSS (config.RSSFileName, 0, 30);
		File.Copy ("log-style.css", "texts/log-style.css", true);
	}
}
