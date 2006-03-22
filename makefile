ASSEMBLIES=	-r:RSS.NET.dll	\
		-r:System.Web.dll

lb.exe: lb.cs config.cs
	mcs $(ASSEMBLIES) -debug -out:$@ $^

clean:
	rm -f *.exe *.mdb

