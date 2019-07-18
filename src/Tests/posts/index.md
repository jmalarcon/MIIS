---
title: Sample folder for Files custom Front-Matter field
author: JM Alarcon
posts: !!FilesFromFolder ./
---

# Sample folder for Files custom Front-Matter field

>These files have been retrieved and minimally adapted from this repo: https://github.com/NuGet/docs.microsoft.com-nuget, just for testing purposes

This is a sample file to show all the contents of an specific folder. In this case I'm using `"./"` as the folder param for `FilesFromFolder` FM custom param. In the parent folder you should use the name of the folder. This `index.md` file shouldn't be in the listing:

**{{ posts.size }}** posts:

{% for post in posts %}
- {{forloop.index}}: [{{post.Title}}]({{post.URL}}) - [{{ post.Date | Date: "dddd, dd MMMM, yyyy" }}]<br>{{post.Excerpt | StripNewlines | Truncate: 75 }}
{% endfor %}

## Posts in reverse order, only the first 5 of them

In this case I've used the same `posts` parameter, but you could simply have used a Front-Matter parameter such as `revposts: !!FileFromFolder ./ true asc` and heve them reversed form the source:

{%- for post in posts reversed -%}
{%- if forloop.index <= 5 -%}
- [{{ post.Title }}]({{post.URL}}) - [{{ post.Date | Date: "dddd, dd MMMM, yyyy" }}]<br>{{post.Excerpt | StripNewlines | Truncate: 75 }}
{%- endif -%}
{%- endfor -%}