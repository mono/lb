#REMOTE_DIRECTORY=miguel@www.tirania.org:tirania/blog
REMOTE_DIRECTORY=u40995234@s174196906.onlinehome.us:web/tirania.org/blog

ASSEMBLIES=	-r:RSS.NET.dll	\
		-r:System.Web	

lb.exe: lb.cs config.cs
	gmcs $(ASSEMBLIES) -debug lb.cs config.cs -out:lb.exe 

b: lb.exe
	mono --debug lb.exe

clean:
	rm -f *.exe

push: b
	chmod 644 archive/*/*.html
	chmod 644 *html *rss2 *php 
	rsync -zu --checksum -pr -v --rsh=ssh texts archive prettyprint.js		\
	log-style.css *.rss2 *.php index.html page*.html all.html	\
	$(REMOTE_DIRECTORY)

check-update:
	-mv config.xml ._config.xml
	cp config.xml.check config.xml
	mono --debug lb.exe --blog-directory=`pwd`/test/in --prefix=test/out \
		--blog-template=template.test --entry-template=entry.test \
		--rss-filename=test
	-rm config.xml
	-mv ._config.xml config.xml

check:
	-rm -Rf test/tmp
	-mv config.xml ._config.xml
	cp config.xml.check config.xml
	mono --debug lb.exe --blog-directory=`pwd`/test/in --prefix=test/tmp \
		--blog-template=template.test --entry-template=entry.test \
		--rss-filename=test
	diff -ru --exclude=\.svn test/out test/tmp 
	-rm config.xml
	-mv ._config.xml config.xml

