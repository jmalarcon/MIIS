---
title: Test for external files
data: a,b,c,d,e
extFile: ~/toc.md
extFile2: ../raw.mdh
#Can't inject this file (no extension). Is used to demonstrate processing fields in injectfile
extFilePath: ../raw
---
# This is a file that includes an external file (File Processing Fields)

Current URL relative to the root: {{url}}
Current URL relative to the root (no extension): {{urlnoext}}

This content is taken from the `toc.md` file using a file with an `.md` or `.mdh` extension in a Front-Matter field:

----
{{extFile |}}

---

This kind of external files will process further fields inside their contents. But you must be careful because you could potentially create circular references!! (although it's difficult):

```
{{extFile2}}
```

This content is taken from a external file with the MIIS-Liquid's special tag `insertfile` and includes **field processing in the same context as the parent file**:

```
{% insertfile ../raw.mdh %}
```

You can even use Front-Matter fields as the origin for the file to load:

```
{% insertfile {{extFilePath}}.mdh %}
```

>IMPORTANT: Please, notice that you couldn't have used `extFile` or `extFile2` as parameters in the previous tag. The reason is that, since parameters are rendered **before** reading the file, they will be rendered as the file content itself (.md and .mdh values in parameters are processed this way), so not a valid filename and therefore an invalid file type exception will be shown. Try one of them to check exactly what I'm talking about.

inserfile custom tags can detect and inform about custom references