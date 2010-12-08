#REMOTE_DIRECTORY=miguel@www.tirania.org:tirania/blog
REMOTE_DIRECTORY=u59770950@s338502803.onlinehome.us:web/tirania.org/blog

ASSEMBLIES=	-r:RSS.NET.dll	\
		-r:System.Web	

lb.exe: lb.cs config.cs
	gmcs $(ASSEMBLIES) -debug lb.cs config.cs -out:lb.exe 

b: lb.exe
	mono --debug lb.exe -c config.xml -p output

c: lb.exe
	-mkdir new; cp twentyten/* new
	mono --debug lb.exe -c config.xml -p new -b twentyten/blog-template -e twentyten/entry-template

clean:
	rm -fr *.exe output

push:
	echo select pushb or pushc

pushb: b
	make do-push DIR=output

pushc: c
	make do-push DIR=new

r:
	rsync twentyten/* $(REMOTE_DIRECTORY)

do-push:
	chmod 644 $(DIR)/archive/*/*.html
	chmod 644 $(DIR)/*html output/*rss2 
	rsync -zu --checksum -pr -v --rsh=ssh texts prettyprint.js log-style.css 		\
	$(DIR)/archive $(DIR)/*.rss2 $(DIR)/index.html $(DIR)/page*.html $(DIR)/all.html	\
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

