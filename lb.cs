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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using Rss;
using Markdig;
class DayEntry : IComparable {
	public DateTime Date;
	public string Body;
	public string Caption = "";
	public string DateCaption;
	public string Category = "";
	public bool Comments = true; // DateTime.Now > new DateTime (2007, 1, 1);
	public string RenderedComment;
	public List<string> Images;
	
	Blog blog;
	public string extra = "";
	
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
	public DateTime SecondFix = new DateTime (2005, 09, 30, 0, 0, 8);
	
	DayEntry (Blog blog, string file)
	{
		this.blog = blog;
		blog_base = blog.config.BlogWebDirectory;
		ParseDate (file);

		using (FileStream i = File.OpenRead (file)){
			using (StreamReader s = new StreamReader (i, Encoding.GetEncoding (blog.config.InputEncoding))){
				if (file.EndsWith (".html"))
					Load (s, true, file);
				else if (file.EndsWith (".txt"))
					Load (s, false, file);
				else if (file.EndsWith (".md")) {
					var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
					using (TextReader tr = new StringReader (Markdown.ToHtml (s.ReadToEnd (), pipeline))) Load (tr, true, file);
#if false
					Markdown m = new Markdown (new MarkdownOptions {
						AutoHyperlink = true,
						AutoNewlines = false,
						EmptyElementSuffix = ">",
						EncodeProblemUrlCharacters = true,
						LinkEmails = true,
						StrictBoldItalic = true
					});
					using (TextReader tr = new StringReader (m.Transform (s.ReadToEnd ()))) Load (tr, true, file);
#endif
				}
					
			}
		}
	}

	public static DayEntry Load (Blog blog, string file)
	{
		DayEntry de = null;

		try {
			de = new DayEntry (blog, file);
		} catch (Exception e) {
			if (blog.Verbose)
				Console.WriteLine ("Failed to load file: {0}. Reason: {1}", file, e.Message);
		}
		return de;
	}

	void ParseDate (string file)
	{
		int month;

		int idx = file.IndexOf (blog.config.BlogDirectory);
		string entry = file;
		if (idx >= 0)
			entry = file.Substring (blog.config.BlogDirectory.Length);
		Match match = Regex.Match (entry, "^(.*/)?(20[0-9][0-9])/([a-z]+)-0*([0-9]+)(-[0-9])?");

		Category = match.Groups [1].Value;
		int year = Int32.Parse (match.Groups [2].Value);
		int day = Int32.Parse (match.Groups [4].Value);
		string month_name = match.Groups [3].Value;
		extra = match.Groups [5].Value;

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
			DateTime access_date;
			
			if (Date >= SecondFix)
				access_date = fi.LastWriteTime;
			else
				access_date = fi.LastWriteTimeUtc;

			Date = new DateTime (year, month, day, access_date.Hour, access_date.Minute, 0);
		}
					     
		DateCaption = String.Format ("{0:dd} {0:MMM} {0:yyyy}", Date);
	}
	
	void Load (TextReader i, bool is_html, string file)
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

			// '#date' followed by the output of:
			//	LC_TIME=C date -u +"%a, %d %b %Y %T GMT"
			// will set the publication date.
			// Example:
			// #date Thu, 09 Feb 2006 18:42:56 GMT
			if (s.StartsWith ("#date ")) {
				try {
					Date = DateTime.ParseExact (s.Substring (6), "r", null);
				} catch (Exception e) {
					Console.WriteLine ("Error parsing: '{0}'\n{1} on {2}", s.Substring (5), e, file);
					Environment.Exit (1);
				}
				continue;
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
						Console.WriteLine ("Wrong #pic command on {0}", file);
						continue;
					}
					
					string filename = s.Substring (5, idx-5);
					string caption = s.Substring (idx + 1);
					sb.Append (String.Format ("<p><center><a href=\"{0}pic.php?name={1}&caption={2}\"><img border=0 src=\"{3}/pictures/small-{1}\"></a><p>{2}</center></p>", blog_base, filename, caption, blog.config.BlogImageBasedir));
					continue;
					
				} else if (s.StartsWith ("#comment")){
					Comments = true;
					continue;
				} else if (s.StartsWith ("#nocomment")){
					Comments = false;
					continue;
				} else if (s.StartsWith ("#thumbnail")) {
					Match m = Regex.Match (s, @"^#thumbnail\s+(?<filename>[^\s]+)\s+(?<desc>.*)$");
					if (m.Groups.Count > 0) {
						string filename = Path.GetFileName (m.Groups ["filename"].Value);
						string thumbnail = LB.GetThumbnailName (filename);
						sb.AppendFormat ("<blockquote><p><a href=\"@ENTRY_PATH@{0}\"><img " +
								"src=\"@ENTRY_PATH@{1}\" title=\"{2}\" " + 
								"alt=\"{2}\" /></a></p></blockquote>\n",
								filename, thumbnail, 
								m.Groups.Count == 1 ? "" : m.Groups ["desc"].Value);
						Images = Images ?? new List<string> ();
						Images.Add (m.Groups ["filename"].Value);
						continue;
					}
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
			StreamReader s = new StreamReader (i, Encoding.GetEncoding (blog.config.InputEncoding));
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
			return String.Format ("archive{2}{0:yyyy}/{0:MMM}-{0:dd}{1}.html", Date, extra, Category);
		}
	}

	public string Id {
		get {
			return string.Format ("entry{0}{1}{2}",
				Category.Replace ('/', '-'), Date.ToString ("yyyy-MM-ddThh:mm:sstt"), extra);
		}
	}
}

class Blog {
	public Config config;
	public bool Verbose;
	string template;
	public DateTime pubDate = new DateTime (1, 1, 1);
	string entry_template;
	string analytics = "";
	string comments = "";
	string archive_navigator;
	Hashtable category_entries = new Hashtable ();

	List<DayEntry> entries = new List<DayEntry> ();

	public int Entries {
		get {
			return entries.Count;
		}
	}
	
	public Blog (Config config, string template)
	{
		this.template = template;
		this.config = config;
		this.entry_template = File.OpenText (config.EntryTemplate).ReadToEnd ();
		Verbose = config.Verbose;

		LoadDirectory (new DirectoryInfo (config.BlogDirectory));

		Console.WriteLine ("Loaded: {0} days", entries.Count);

		entries.Sort ();
		foreach (DayEntry de in entries)
			AddCategory (category_entries, de);

		if (config.AnalyticsStub != null && config.AnalyticsStub.Length > 0)
			analytics = File.OpenText (config.AnalyticsStub).ReadToEnd ();
		if (config.CommentsStub != null && config.CommentsStub.Length > 0){
			comments = File.OpenText (config.CommentsStub).ReadToEnd ();
		}

		GenerateArchiveNavigation ();
	}

	void GenerateArchiveNavigation ()
	{
		StringBuilder ab = new StringBuilder ();

		ab.Append ("<div class=\"archive\">");
		
		var grouping = from be in entries
			group be by be.Date.Year into years
			orderby years.Key descending
			select new {
				Year = years.Key,
				Months = from ybe in years
					group ybe by ybe.Date.Month into monthly
					orderby monthly.Key
					select new { Month = monthly.Key, Entries = monthly } };

		string prefix = "<div class=\"col-lg-6\">\n<ul class=\"list-unstyled\">";
		string suffix = "</ul></div>";
		var first_column = new StringBuilder (prefix);
		var second_column = new StringBuilder (prefix);
		bool first = true;
		
		foreach (var year_group in grouping){
			var target = first ? first_column : second_column;
			first = !first;
			
			target.Append (String.Format ("\n<li><b>{0}</b><br/>\n", year_group.Year));
			int count = 0;
			foreach (var month in year_group.Months){
				DateTime year_month = new DateTime (year_group.Year, month.Month, 1);
				string month_archive_path = String.Format ("archive/{0:yyyy}/{0:MMM}.html", year_month);
				
				target.Append (String.Format ("<a href=\"{1}/{2}\">{0:MMM}</a> ", year_month, config.BlogWebDirectory, month_archive_path));
			}
			target.Append ("</li>");
		}
		first_column.Append (suffix);
		second_column.Append (suffix);
		ab.Append (first_column.ToString ());
		ab.Append (second_column.ToString ());
		ab.Append ("</div>");
		archive_navigator = ab.ToString ();
		foreach (var year_group in grouping){
			foreach (var month in year_group.Months){
				DateTime year_month = new DateTime (year_group.Year, month.Month, 1);
				string month_archive_path = String.Format ("archive/{0:yyyy}/{0:MMM}.html", year_month);
			
				IList month_entries = month.Entries.ToList<DayEntry> ();
				RenderHtml (Path.Combine (LB.config.Prefix, month_archive_path), "../../", month_entries, 0, month_entries.Count, false);
			}
		}
	}
	
	void LoadDirectory (DirectoryInfo dir)
	{
		if (dir.Name.EndsWith ("drafts"))
			return;
		if (Verbose)
			Console.WriteLine ("dir:" + dir);
		foreach (DirectoryInfo subdir in dir.GetDirectories ()) {
			LoadDirectory (subdir);
		}

		foreach (FileInfo file in dir.GetFiles ()) {
			if (!(file.Name.EndsWith (".html") || file.Name.EndsWith (".txt") || file.Name.EndsWith (".md")))
				continue;
			DayEntry de = DayEntry.Load (this, file.FullName);
			
			if (de != null) {
				entries.Add (de);
				if (de.Date > pubDate)
					pubDate = de.Date;
			}
		}
	}

	// A category is always of the form "/((.+)/)*", e.g. /, /foo/, /foo/bar/
	// Add the DayEntry to its category and all parent categories
	void AddCategory (Hashtable hash, DayEntry day)
	{
		string category = day.Category;
		do {
			IList entries = (IList) hash [category];
			if (entries == null) {
				entries = new ArrayList ();
				hash [category] = entries;
			}
			entries.Add (day);
			int n = category.Length > 2 
				? category.LastIndexOf ('/', category.Length-2) 
				: -1;
			category = (n == -1) ? null : category.Substring (0, n+1);
		} while (category != null && category.Length > 0);
	}
	
	static DateTime LastDate = new DateTime (2004, 5, 19, 0, 0, 0);
	
	void Render (TextWriter o, IList entries, int idx, string blog_base, bool include_daily_anchor, bool single_entry)
	{
		DayEntry d = (DayEntry) entries [idx];

		string anchor = HttpUtility.UrlEncode (d.Date.ToString ()).Replace ('%','-').Replace ('+', '-');
		string entry_anchor = "";
		if (include_daily_anchor || d.Date < LastDate)
			entry_anchor = String.Format ("<a name=\"{0}\"></a>", anchor);
		
		string entry_specific = "";
		string navigation = "";

		if (single_entry){
			navigation = GetEntryNavigation (entries, idx, blog_base);
			if (config.EntrySpecific != null && config.EntrySpecific != String.Empty)
			    entry_specific = File.OpenText (config.EntrySpecific).ReadToEnd ();
		}

		Hashtable substitutions = new Hashtable ();
		substitutions.Add ("@ENTRY_ANCHOR@", entry_anchor);
		substitutions.Add ("@ENTRY_NAVIGATION@", navigation);
		substitutions.Add ("@ENTRY_SPECIFIC@", entry_specific);

		FillEntrySubstitutions (substitutions, d, blog_base, single_entry);

		StringWriter body = new StringWriter (new StringBuilder (d.Body.Length));
		Translate (d.Body, body, substitutions);

		substitutions.Add ("@ENTRY_BODY@", body.ToString ());
		Translate (entry_template, o, substitutions);
	}

	void FillEntrySubstitutions (Hashtable substitutions, DayEntry d, string blog_base, bool single_entry)
	{
		string category_paths = GetCategoryPaths (d, blog_base);
		string entry_path = LB.GetEntryPath (blog_base, d);

		substitutions.Add ("@ENTRY_ID@", d.Id);
		substitutions.Add ("@ENTRY_PATH@", entry_path);
		substitutions.Add ("@ENTRY_PERMALINK@", d.PermaLink);
		substitutions.Add ("@ENTRY_CAPTION@", d.Caption);
		substitutions.Add ("@ENTRY_CAPTION_ENC@", HttpUtility.UrlEncode (d.Caption));
		substitutions.Add ("@BASEDIR@", config.BlogWebDirectory);
		substitutions.Add ("@BASEIMAGES@", config.BlogImageBasedir);
		substitutions.Add ("@COPYRIGHT@", config.Copyright);
		substitutions.Add ("@ENTRY_CATEGORY@", d.Category);
		substitutions.Add ("@ENTRY_DATECAPTION@", d.DateCaption);
		substitutions.Add ("@ENTRY_CATEGORY_PATHS@", category_paths);
		substitutions.Add ("@BLOGWEBDIR@", config.BlogWebDirectory);
		substitutions.Add ("@ENTRY_URL_PERMALINK@", Path.Combine (config.BlogWebDirectory, d.PermaLink));

		if (d.Comments && single_entry){
			StringWriter rendered_comment = new StringWriter (new StringBuilder (comments.Length));
			Translate (comments, rendered_comment, substitutions);
			substitutions.Add ("@COMMENTS@", rendered_comment.ToString ());
			d.RenderedComment = rendered_comment.ToString ();
		} else 
			substitutions.Add ("@COMMENTS@", "");
	}

	string GetEntryNavigation (IList entries, int idx, string blog_base)
	{
		DayEntry prev = (DayEntry) (idx > 0 ? entries [idx-1] : null);
		DayEntry next = (DayEntry) (idx+1 < entries.Count ? entries [idx+1] : null);

		StringBuilder nav = new StringBuilder ();

		if (prev != null)
			nav.Append (string.Format ("<a href=\"{0}{1}\">&laquo; {2}</a> | \n", 
						blog_base, prev.PermaLink, prev.Caption));
		nav.Append (string.Format ("<a href=\"{0}{1}\">Main</a>\n", 
					blog_base, config.BlogFileName));
		if (next != null)
			nav.Append (string.Format (" | <a href=\"{0}{1}\">{2} &raquo;</a> \n", 
						blog_base, next.PermaLink, next.Caption));
#if false	
		nav.Append (string.Format ("<h3><a href=\"{2}{0}\" class=\"entryTitle\">{3}</a></h3>",
			     d.PermaLink, d.DateCaption, blog_base, d.Caption));
		nav.Append ("<div class='blogentry'>" + d.Body + "</div>");
		nav.Append (string.Format ("<div class='footer'>Posted by {2} on <a href=\"{0}{1}\">{3}</a></div><p>",
			     blog_base, d.PermaLink, config.Copyright, d.DateCaption));
#endif
		return nav.ToString ();
	}

	string GetCategoryPaths (DayEntry d, string blog_base)
	{
		string[] paths = d.Category.Split ('/');
		// It'll be more common for no category to be used -- the "/" category --
		// which requires 24 characters.  Optimize for the common case.
		StringBuilder cat_paths = new StringBuilder (32);

		// Skip the last paths entry, as it's the "" string
		for (int i = 0; i < paths.Length-1; ++i) {
			string parent = string.Join ("/", paths, 0, i+1) + "/";
			cat_paths.AppendFormat ("<a href=\"{0}archive{1}\">{2}/</a>", 
					blog_base, parent, paths [i]);
		}

		return cat_paths.ToString ();
	}

	// Single-pass s/@...@/.../ replacement engine.
	// `substitutions' contains search text (including @) and replacement text.
	void Translate (string input, TextWriter o, Hashtable substitutions)
	{
		int token = -1;
		bool escape = false;
		for (int i = 0; i < input.Length; ++i) {
			char c = input [i];
			string subst = null;
			if (token >= 0)
				subst = input.Substring (token, i-token+1);
			switch (c) {
			case '\\':
				escape = true;
				break;
			case '@':
				if (escape) {
					escape = false;
					// Only write a new @ if not inside @...@.
					if (token == -1) {
						escape = false;
						o.Write ('@');
					}
					break;
				}
				if (token == -1) {
					token = i;
				}
				else {
					string rep = (string) substitutions [subst];
					if (rep == null) {
						// No match; look for a new match from this point
						o.Write (subst.Substring (0, subst.Length-1));
						token = i;
					}
					else {
						o.Write (rep);
						token = -1;
					}
				}
				break;
			case '\r': case '\n':
				if (token != -1) {
					o.Write (subst);
					token = -1;
					escape = false;
					break;
				}
				goto default;
			default:
				if (escape && token == -1) {
					o.Write ('\\');
				}
				escape = false;
				if (token == -1)
					o.Write (c); 
				break;
			}
		}
	}

	void Render (TextWriter o, IList entries, int start, int end, string blog_base, bool include_daily_anchor)
	{
		bool navigation = start + 1 == end;
		
		for (int i = start; i < end; i++){
			int idx = entries.Count - i - 1;
			if (idx < 0)
				return;
			
			Render (o, entries, idx, blog_base, include_daily_anchor, navigation);
		}
	}

	void RenderArticleList (TextWriter o)
	{
		foreach (Article a in articles){
			o.WriteLine ("<a href=\"{0}\">{1}</a><br>", a.url, a.caption);
		}
	}

	string GetPageNavigation (int start, int end)
	{
		StringBuilder nav = new StringBuilder ();
		
		if (start != 0)
			nav.Append (string.Format ("<a href=\"{0}\">&laquo; Newer entries</a>", LB.GetOutputFileAtOffset (start)));
		
		if (end + Config.EntriesPerPage < entries.Count){
			if (nav.Length > 0)
				nav.Append (" | ");
			nav.Append (string.Format ("<a href=\"{0}\">Older entries &raquo;</a>", LB.GetOutputFileAtOffset (end+1)));
		}

		return nav.ToString ();
	}
	
	void RenderHtml (string output, string blog_base, IList entries, int start, int end, bool include_page_navigation)
	{
		using (FileStream o = CreateFile (output)){
			StreamWriter w = new StreamWriter (o, GetOutputEncoding ());

			StringWriter blog_entries = new StringWriter ();
			Render (blog_entries, entries, start, end, blog_base, Path.GetFileName (output) == "all.html");

			StringWriter blog_articles = new StringWriter ();
			RenderArticleList (blog_articles);

			string page_navigation = "";
			
			string title;
			if (Math.Abs (start - end) == 1){
				DayEntry d = (DayEntry) entries [entries.Count - start - 1];
				title = String.Format ("{0} - {1}", d.Caption, config.Title);
			} else {
				title = config.Title;

				if (include_page_navigation)
					page_navigation = GetPageNavigation (start, end);
			}

			Hashtable substitutions = new Hashtable ();
			substitutions.Add ("@BLOG_ENTRIES@", blog_entries.ToString ());
			substitutions.Add ("@ANALYTICS@", analytics);
			substitutions.Add ("@BLOG_ENTRY_INDEX@", CreateEntryIndex (entries, start, end));
			substitutions.Add ("@BLOG_ARTICLES@", blog_articles.ToString ());
			substitutions.Add ("@BASEDIR@", blog_base);
			substitutions.Add ("@TITLE@", title);
			substitutions.Add ("@DESCRIPTION@", config.Description);
			substitutions.Add ("@RSSFILENAME@", config.RSSFileName);
			substitutions.Add ("@EDITOR@", config.ManagingEditor);
			substitutions.Add ("@BLOGWEBDIR@", config.BlogWebDirectory);
			substitutions.Add ("@ARCHIVE_NAVIGATOR@", archive_navigator);
			substitutions.Add ("@PAGE_NAVIGATION@", page_navigation);

			var processed_widgets = new StringWriter ();
			Translate (config.Widgets, processed_widgets, substitutions);
			substitutions.Add ("@WIDGETS@", processed_widgets.ToString ());
				
			Translate (template, w, substitutions);

			w.Flush ();
		}
	}

	// The default Encoding.GetEncoding ("utf-8") includes the BOM, 
	// which we don't want
	Encoding GetOutputEncoding ()
	{
		string encoding = config.OutputEncoding;
		Encoding e;
		if (encoding != null && encoding.ToLower().Replace ("-", "_").Equals ("utf_8"))
			e = new UTF8Encoding (false);
		else
			e = Encoding.GetEncoding (encoding);
		return e;
	}

	string CreateEntryIndex (IList entries, int start, int end)
	{
		StringBuilder sb = new StringBuilder ();
		sb.Append ("<ul class=\"blog-index\">\n");
		for (int i = start; i < end; ++i) {
			int idx = entries.Count - i - 1;
			if (idx < 0)
				break;
			DayEntry d = (DayEntry) entries [idx];
			sb.AppendFormat ("  <li class=\"blog-index-item\"><a href=\"#{0}\">{1}</a></li>\n",
					d.Id, d.Caption);
		}
		sb.Append ("</ul>\n");
		return sb.ToString ();
	}

	public void RenderHtml (string output, int start, int end, string blog_base, bool include_page_navigation)
	{
		RenderHtml (output, blog_base, entries, start, end, include_page_navigation);
	}

	public void RenderArchive ()
	{
		for (int i = 0; i < Entries; i++){
			DayEntry d = (DayEntry) entries [i];

			string parent_dir = "../..";
			if (d.Category.Length > 0)
				parent_dir += Regex.Replace (d.Category, "[^/]+", "..");
			RenderHtml (Path.Combine (config.Prefix, d.PermaLink), 
				    entries.Count - i - 1, entries.Count - i, parent_dir, false);
			if (d.Images == null)
				continue;
			foreach (string filename in d.Images) {
				string file = Path.GetFileName (filename);
				string thumbnail = Path.Combine (LB.GetEntryPath (config.Prefix + "/", d), LB.GetThumbnailName (file));
				string thumbnail_target = Path.Combine (
						LB.GetEntryPath (config.Prefix + "/", d), file);

				file = Path.Combine (config.ImageDirectory, filename);
				if (!File.Exists (file)) {
					Console.Error.WriteLine ("lb: Missing file for #thumbnail {0}, ({1}).", 
							filename, file);
					continue;
				}
				if (!File.Exists (thumbnail_target))
					File.Copy (file, thumbnail_target);
				if (!File.Exists (thumbnail)) {
					ProcessStartInfo psi = new ProcessStartInfo (config.ThumbnailCommandFileName);
					psi.Arguments = string.Format (config.ThumbnailCommandArguments, file, thumbnail);
					Process p = Process.Start (psi);
					p.WaitForExit ();
					if (p.ExitCode != 0)
						Console.Error.WriteLine ("lb: error running command: {0} {1}", 
								psi.FileName, psi.Arguments);
				}
			}
		}

		foreach (DictionaryEntry de in category_entries) {
			string category = de.Key.ToString ();
			IList entries = (IList) de.Value;
			string parent_dir = ".." + Regex.Replace (category, "[^/]+", "..");
			RenderHtml (Path.Combine (config.Prefix, "archive" + category + config.BlogFileName),
				    parent_dir, entries, 0, entries.Count, false);
		}
	}
	
	RssChannel MakeChannel ()
	{
		RssChannel c = new RssChannel (config.Title, config.Description, new Uri (config.BlogWebDirectory + "/" + config.BlogFileName));

		c.Copyright = config.Copyright;
		c.Generator = "lb#";
		c.ManagingEditor = config.ManagingEditor;
		// c.PubDate = System.DateTime.Now;
		c.PubDate = pubDate;
		
		return c;
	}

	public void RenderArchiveRss (RssVersion version, string output, int end)
	{
		foreach (DictionaryEntry de in category_entries) {
			string category = de.Key.ToString ();
			IList entries = (IList) de.Value;
			RenderRSS (Path.Combine (config.Prefix, "archive" + category + output),
				   entries, 0, Math.Min (end, entries.Count));
		}
	}

	public void RenderRSS (string output, IList entries, int start, int end)
	{
		RssChannel channel = MakeChannel ();

		for (int i = start; i < end; i++){
			int idx = entries.Count - i - 1;
			if (idx < 0)
				continue;
			
			DayEntry d = (DayEntry) entries [idx];

			Hashtable substitutions = new Hashtable ();
			FillEntrySubstitutions (substitutions, d, config.BlogWebDirectory, false);
			StringWriter description = new StringWriter (new StringBuilder (d.Body.Length));
			Translate (d.Body, description, substitutions);

			StringWriter sw = new StringWriter (new StringBuilder (d.Body.Length));
			Render (sw, entries, idx, "", false, false);
			RssItem item = new RssItem ();
			item.Author = config.Author;
			item.Description = description.ToString ();
			item.Guid = new RssGuid ();
			item.Guid.Name = config.BlogWebDirectory + d.PermaLink;
			item.Link = new Uri (item.Guid.Name);
			item.Guid.PermaLink = DBBool.True;

			item.PubDate = d.Date.ToUniversalTime ();
			if (d.Caption == ""){
				Console.WriteLine ("No caption for: " + d.DateCaption);
				d.Caption = d.DateCaption;
			}
			item.Title = d.Caption;
			
			channel.Items.Add (item);
		}

		FileStream o = CreateFile (output);
		XmlTextWriter xtw = new XmlTextWriter (o, new UTF8Encoding (false));
		Rss20Writer w = new Rss20Writer (xtw);

		w.Write (channel);
		w.Close ();
	}

	public void RenderRSS (string output, int start, int end)
	{
		RenderRSS (output + ".rss2", entries, start, end);
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
	

	FileStream CreateFile (string file)
	{
		FileInfo info = new FileInfo (file);
		if (!info.Directory.Exists)
			info.Directory.Create ();
		try {
			return File.Create (file);
		} catch (Exception e){
			Console.WriteLine (e);
			return null;
		}
	}
}

class LB {
	public static Config config;
	
	static public string GetOutputFileAtOffset (int offset)
	{
		return offset == 0 ? config.BlogFileName : String.Format ("page{0}.html", offset / Config.EntriesPerPage);
	}

	static void Main (string[] args)
	{
		var basicConfig = new Config ();
		if (!basicConfig.Parse (args))
			return;
		
		config = (Config) new XmlSerializer (typeof (Config)).Deserialize (new XmlTextReader (basicConfig.ConfigFile));

		if (args.Length > 1)
			config.Prefix = args [1];
		
		if (config.BlogImageBasedir == null || config.BlogImageBasedir == "")
			config.BlogImageBasedir = config.BlogWebDirectory;
		if (config.Prefix == null || config.Prefix == "")
			config.Prefix = Environment.CurrentDirectory;
		if (config.BlogTemplate == null || config.BlogTemplate == "")
			config.BlogTemplate = "template";
		if (config.EntryTemplate == null || config.EntryTemplate == "")
			config.EntryTemplate = "entry";

		if (!config.Parse (args))
			return;

		string template = File.OpenText (config.BlogTemplate).ReadToEnd ();

		Blog b = new Blog (config, template);

		//
		// Renders the main page (index.html) and the various pageNN.html
		//
		for (int start = 0; start < b.Entries; start += Config.EntriesPerPage){
			string output = GetOutputFileAtOffset (start);

			b.RenderHtml (Path.Combine (config.Prefix, output), start, start + Config.EntriesPerPage, "", true);
		}

		//
		// Renders the year/month dinguses
		//
		
		//
		// Legacy render: all.html
		//
		b.RenderHtml (Path.Combine (config.Prefix, "all.html"), 0, b.Entries, "", false);

		//
		// Renders each individual blog entry into the archive
		//
		b.RenderArchive ();

		//
		// The RSS feed
		//
		b.RenderRSS (Path.Combine (config.Prefix, config.RSSFileName), 0, 30);
		b.RenderArchiveRss (RssVersion.RSS20, config.RSSFileName + ".rss2", 30);

		if (File.Exists ("log-style.css")) {
			File.Copy ("log-style.css", "texts/log-style.css", true);
			File.Copy ("log-style.css", Path.Combine (config.Prefix, "log-style.css"), true);
		}
	}

	public static string GetThumbnailName (string filename)
	{
		return Regex.Replace (filename, @"\.(.*?)$", @"-web.$1");
	}

	public static string GetEntryPath (string blog_base, DayEntry d)
	{
		return string.Format ("{0}archive{1}{2:yyyy}/", blog_base, 
				d.Category, d.Date);
	}
}
