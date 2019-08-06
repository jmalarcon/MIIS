---
# Change the default naming convention (Ruby) to force the C# naming convention instead
# In this case the property names are: PropertyOne instead of property_one
# It affects filters and Liquid syntax in general too!
Naming: CSharp
title: Sample folder for Files custom Front-Matter field
author: JM Alarcon
posts: !!FilesFromFolder ./
tags: !!TagsFromFolder ./
categs: !!CategsFromFolder ./
caching: false
---

# Sample folder for Files obtained from a custom Front-Matter field

>**IMPORTANT**: this sample file uses the C# naming convention to render Liquid tags and to access element properties.
>Check [this other variant](_index-ruby.md) to see the same file with the most common Ruby convention.

The sample files have been retrieved and minimally adapted from this repo: https://github.com/NuGet/docs.microsoft.com-nuget, just for testing purposes

This is a sample file to show all the contents of an specific folder. In this case I'm using `"./"` as the folder param for `FilesFromFolder` FM custom param. In the parent folder you should use the name of the folder. This `index.md` file shouldn't be in the listing:

**{{ posts.size }}** posts {% if tag != "" %}&nbsp;with Tag '{{tag}}'{% elseif categ != "" %}&nbsp;with Category '{{categ}}'{% else %}(all){% endif %}:

{%- if posts.size == 0 -%}
Nothing to show here!
{%- endif -%}

{%- for post in posts -%}
- {{forloop.index}}: [{{post.Title}}]({{post.URL}}) - [{{ post.Date | Date: "dddd, dd MMMM, yyyy" }}]<br>{{post.Excerpt | StripNewlines | Truncate: 75 }}
{%- endfor -%}

## Posts in reverse order, only the first 5 of them

In this case I've used the same `posts` parameter, but you could simply have used a Front-Matter parameter such as `revposts: !!FileFromFolder ./ true asc` and have them reversed form the source:

{%- for post in posts reversed -%}
{%- if forloop.index <= 5 -%}
- [{{ post.Title }}]({{post.URL}}) - [{{ post.Date | Date: "dddd, dd MMMM, yyyy" }}]<br>{{post.Excerpt | StripNewlines | Truncate: 75 }}
{%- endif -%}
{%- endfor -%}

## Tags available inside the files in this folder

There's a `tags` parameter defined in this file's Front-Matter to get all the tags defined in the files inside this folder. Here they are:

{%- for tag in tags -%}
1. [{{tag}}](./?Tag={{tag | url_encode}})
{% endfor %}

## Categories available inside the files in this folder

There's a `categs` parameter defined in this file's Front-Matter to get all the categories defined in the files inside this folder. Here they are:

{%- for categ in categs -%}
1. [{{categ}}](./?Categ={{categ | url_encode}})
{% endfor %}