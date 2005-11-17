REMOTE_DIRECTORY=miguel@www.tirania.org:tirania/blog

ASSEMBLIES=	-r:RSS.NET.dll	\
		-r:System.Web	

lb.exe: lb.cs
	mcs $(ASSEMBLIES) -debug lb.cs config.cs -out:lb.exe 

b: lb.exe
	mono --debug lb.exe

clean:
	rm -f *.exe

push: b
	chmod 644 archive/*/*.html
	chmod 644 *html *rss2 *php 
	rsync -pr -v --rsh=ssh texts archive prettyprint.js	\
	log-style.css *.rss2 *.php index.html all.html		\
	$(REMOTE_DIRECTORY)

