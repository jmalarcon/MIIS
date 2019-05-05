---
title: Test for external files
data: a,b,c,d,e
extFile: ~/toc.md
extFile2: raw.mdh
---
# This is a file that includes an external file (File Processing Fields)

This content is taken from the `toc.md` file:

{{extFile}}

For security reasons, won't process further fields inside the external file!!:

{{extFile2}}

This content is taken from a external file with teh MIIS-Liquid's special tag `insertfile` and includes **field processing** in the same contest as the parent file:

```
{% insertfile raw.mdh %}
```