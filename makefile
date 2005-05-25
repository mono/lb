ASSEMBLIES=	-r:RSS.NET.dll	\
		-r:System.Web	
		#-r:Atom.NET.dll

lb.exe: lb.cs
	mcs $(ASSEMBLIES) -g lb.cs config.cs -out:lb.exe 

b: lb.exe
	mono --debug lb.exe

clean:
	rm -f *.exe

push: b
	chmod 644 archive/*/*.html
	chmod 644 *html *rss2 *php 
	rsync -pr -v -v --rsh=ssh texts archive prettyprint.js	\
	log-style.css *.rss2 *.php index.html all.html		\
	miguel@www.tirania.org:tirania/blog

pp: b
	chmod 644 archive/*/*.html
	chmod 644 *html *rss2 *php *atom
	rsync -pr -v --rsh=ssh texts archive prettyprint.js	\
	log-style.css *.rss2 *.atom *.php all.html		\
	primates.ximian.com:public_html
