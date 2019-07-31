---
title: Test for external files
data: a,b,c,d,e
extFile: ~/toc.md
extFile2: ../raw.mdh
extFilePath: ../raw
---
# This is a file that includes an external file (File Processing Fields)

This content is taken from the `toc.md` file using a file with an `.md` or `.mdh` extension in a Front-Matter field. It's shown as a code block but could have been rendered as HTML in the final page by removing the code fence:

```
{{extFile}}
```

For security reasons, this kind of external files won't process further fields inside their contents!!:

```
{{extFile2}}
```

This content is taken from a external file with the MIIS-Liquid's special tag `insertfile` and includes **field processing** in the same context as the parent file:

```
{% insertfile ../raw.mdh %}
```

You can even use Front-Matter fields as the origin for the file to load:

```
{% insertfile {{extFilePath}}.mdh %}
```

>IMPORTANT: Please, notice that you couldn't have used `extFile` or `extFile2` as parameters in the previous tag. The reason is that, since parameters are rendered before reading the file, the will be rendered into the file content itself, so not a valid filename and therefore an invalid file type exception will be shown. Try one of them to check exactly what I'm talking about.