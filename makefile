ASSEMBLIES=	-r:RSS.NET.dll	\
		-r:System.Web

lb.exe: lb.cs
	mcs $(ASSEMBLIES) -g lb.cs config.cs -out:lb.exe

b: lb.exe
	mono --debug lb.exe

clean:
	rm -f *.exe

push: b
	chmod 644 archive/*/*.html
	chmod 644 *html *rss2 *php
	rsync -pr -v --rsh=ssh texts archive prettyprint.js log-style.css miguel.rss2 activity-log.php all.html primates.ximian.com:public_html
