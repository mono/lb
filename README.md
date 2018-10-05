# This is Lame Blog.

A small script that I wrote to maintain my blog on the web.  I
did not use another blog system, because I had different
requirements than other people have.

**I wanted:**

* To support off-line editing.  Since for long periods of time I might not have an internet connection.

* To be able to edit my blog entries with my choice editor, not using some integrated tool.

* To keep my old `.txt` file format for my entries.

* To do some minimal processing on the input.


## Configuration
To configure LameBlog, you must create a file `config.xml`, which looks like this:

		<?xml version="1.0"?>
		<config
		    Author="Miguel de Icaza (miguel@gnome.org)"
		    BlogDirectory="/home/miguel/activity"
		    BlogFileName="index.html"
		    BlogWebDirectory="http://tirania.org/blog/"
		    BlogImageBasedir="http://primates.ximian.com/~miguel"
		    Copyright="Miguel de Icaza"
		    Description="Miguel de Icaza's web log"
		    ManagingEditor="miguel@gnome.org"
		    Title="Miguel de Icaza"
		    RSSFileName="miguel"
		    InputEncoding="utf-8"
		    OutputEncoding="utf-8"
		    AnalyticsStub="analytics"
		    CommentsStub="comments"
		    ImageDirectory="/path/to/images"
		    ThumbnailCommand="shell-command-for-thumbnail source={0} thumbnail={1}"
		/>

The various parameters should be self explanatory.
		
It looks for blog entries in:

`~/activity/YYYY/mmm-dd{-N}.{html|txt}`

For example:

`~/activity/2003/oct-01.html`

For files with the HTML ending, it assumes that the title for the blog entry will be the first line (which should be tagged
with `<h1>title</h1>`).   

This means:

`<h1>This is my Title</h1>`

For text files, the first line should be formatted like this:

`@This is my Title`

To add **multiple entries per day**, append a -N to the entry:

`~/activity/2003/oct-01-1.html`
`~/activity/2003/oct-01-2.html`

For historic reasons, it generates an `all.html` (compatibility with old permalinks) and also now it generates per-day
permalink entries in the archive/ directory.

**To run**, you must type:

`make`

**To upload**, edit the settings on the Makefile, and type:

`make push`

The result is then pushed to a server with rsync.

The AnalyticsStub is the file provided to you by Google as the stub that you must insert.

Lame Blog also now supports Markdown-formatted files as blog entries. They take the `.md` extension, and are regular Markdown
format. You can use the `h1` Markdown tag (`#`) to denote the title on the first line as well, because Markdown files are first processed by MarkdownSharp into HTML, and then loaded as .html
files would be. MarkdownSharp is courtesy of Stack Overflow, and can be found at http://code.google.com/p/markdownsharp/.

## Commands

The following commands can be used in your individual blog entries:

Command | Action
--------|--------
`#include file` | Will include the contents of "file" into the blog entry
`#pic filename,caption` | Will generate the code to center the image stored in filename with the given caption.This emits an HTML anchor to `pic.php` providing filename and caption as GET parameters.
`#comment, #comments` | Will enable the entry to have comments.   To activate comments, you need to get a comment hosting provider,and then paste the stub into the file `comments', or the file referenced in the `config.xml` CommentsStub  field
`#thumbnail filename caption` | A static alternative to #pic.  This looks for `filename` underneath `ImageDirectory`, copies `filename` into the archive directory, creates a thumbnail for `filename` by invoking ThumbnailCommand, and generates HTML that is an `<a><img/></a>` to the thumbnail which targets the full-size image.<br><br> Caption is used as the alt and title text of the `<img/>`. <br><br>A possible ThumbnailCommand value is:<br>`convert -size 400x300 {0} -resize 400x300 +profile "*" {1}` <br> which generates a 400x300 pixel thumbnail of the input file.

## Templates

The main blog template is the file `template`, this contains the general layout of the file and will do substitutious on a number of fields:

**Main template:** `template` file

`@TITLE@` : The title of the blog, this becomes the `<title>`
	
`@DESCRIPTION@` 
			
`@ANALYTICS@` : Substituted with the contents specified in the config file for "Analytics"
	
`@BASEDIR@`
	
`@BLOGWEBDIR@`
	
`@RSSFILENAME@`
	
`@EDITOR@` : Replaced with the string in `ManagingEditor` from the config file
	
`@BLOG_ARTICLES@` : Lists the articles that you want to link, these are not the blog entries, but thinks that you want on your sidebar.
	
`@BLOG_ENTRIES@` : The blog entries rendered.

**Per entry template:** `entry` file.
`@ENTRY_ID@`

`@ENTRY_ANCHOR@` : Anchor used to target an entry.

`@ENTRY_PATH@`

`@ENTRY_NAVIGATION@`: Produces HTML to move to the previous/main/next blog entries

`@ENTRY_PERMALINK@`

`@ENTRY_CAPTION@`

`@ENTRY_CAPTION_ENC@`

`@BASEDIR@`

`@BASEIMAGES@`

`@COPYRIGHT@`

`@ENTRY_CATEGORY@`

`@ENTRY_DATECAPTION@`

`@ENTRY_CATEGORY_PATHS@`

`@BLOGWEBDIR@`

`@ENTRY_URL_PERMALINK@`

`@ENTRY_SPECIFIC@`

Enjoy,<br>
Miguel de Icaza (miguel@gnu.org)
