
lb.exe: lb.cs
	mcs -g lb.cs -out:lb.exe -r:RSS.NET -r:System.Web

b: lb.exe
	mono --debug lb.exe

push: b
	chmod 644 archive/*/*.html
	rsync -pr -v --rsh=ssh texts archive log-style.css miguel.rss2 activity-log.php all.html primates.ximian.com:public_html
