
lb.exe: lb.cs
	mcs -g lb.cs -out:lb.exe -r:RSS.NET.dll -r:System.Web

b: lb.exe
	mono --debug lb.exe

clean:
	rm -f *.exe

push: b
	chmod 644 archive/*/*.html
	chmod 644 *html *rss2 *php
	rsync -pr -v --rsh=ssh texts archive log-style.css miguel.rss2 activity-log.php all.html primates.ximian.com:public_html
