---
title: Sample folder for Files custom Front-Matter field
author: jmalarcon
#posts: !!FilesFromFolder ./ false
#tags: !!TagsFromFolder ./ false
#categs: !!CategsFromFolder ./ false
#caching: true
#CachingTimeout: 60
arr: [cli-ref-config, cli-ref-delete, cli-ref-init]
---

# Sample folder for Files obtained from a custom Front-Matter field

Current URL relative to the root: {{url}}
Current URL relative to the root (no extension): {{urlnoext}}

The sample files have been retrieved and minimally adapted from this repo: https://github.com/NuGet/docs.microsoft.com-nuget, just for testing purposes

Check this folder file's RSS in Atom format: [![Atom Feed](media/rss.png)](./feed){target="_blank"}

This is a sample file to show all the contents of an specific folder. In this case I'm using `"./"` as the folder param for `FilesFromFolder` FM custom param. In the parent folder you should use the name of the folder. This `index.md` file won't be in the listing and no other file whose name starts with "_":

**{{ posts.size }}** published posts {% if tag %}&nbsp;with Tag '{{tag | capitalize}}'{% elseif categ %}&nbsp;with Category '{{categ | capitalize}}'{% else %}(all){% endif %}:

{%- comment -%}
IMPORTANT: Normally this kind of structure will be created in the template, directly in HTML, not in markdown. Although I've done my best to prevent this, in general, using Liquid tags for iteration or conditional intertwined with Markdown could be pretty tricky and could lead to weird results because of unexpected paragraphs generated around liquid tags, etc. It should not happen with the measures taken, but be warned.
By default, dates are formated using the .NET format, much more simple and intuitive than the Ruby one. If you want to use something from Shopify or Jekyll you can switch globally to the Ruby format for dates (see docs)
{%- endcomment -%}

{%- if posts.size == 0 -%}
Nothing to show here!
{%- endif -%}
{% for post in posts %}
- {{forloop.index}}: [{{post.Title}}]({{post.URL}}) - [{{ post.Date | date: "dddd, dd MMMM, yyyy" }}]<br>{{post.excerpt | strip_newlines | truncate: 75 }}
{% endfor %}

{%- comment -%}
Important: emojis must be preceded by a space in order to be rendered!
See: https://github.com/lunet-io/markdig/blob/master/src/Markdig.Tests/Specs/EmojiSpecs.md#emoji
{%- endcomment -%}

> :warning: **[Try the paginated page](page/1)** and examine the contents to see how it's been done

## Posts in reverse order, only the first 5 of them

In this case I've used the same `posts` parameter, but you could simply have used a Front-Matter parameter such as `revposts: !!FileFromFolder ./ true asc` and have them reversed form the source:

{%- for post in posts reversed -%}
{%- if forloop.index <= 5 -%}
- [{{ post.Title }}]({{post.URL}}) - [{{ post.Date | date: "dddd, dd MMMM, yyyy" }}]<br>{{post.Excerpt | strip_newlines | truncate: 75 }}
{%- endif -%}
{%- endfor -%}

## Tags available inside the files in this folder

There's a `tags` parameter defined in this file's Front-Matter to get all the tags defined in the files inside this folder. Here they are:

{%- for tag in tags -%}
1. [{{tag.name | capitalize}}](./?Tag={{tag.name | UrlEncode}}) ({{tag.count}})
{% endfor %}

## Categories available inside the files in this folder

There's a `categs` parameter defined in this file's Front-Matter to get all the categories defined in the files inside this folder. Here they are:

{%- for categ in categs -%}
1. [{{categ.name | capitalize}}](./?Categ={{categ.name | UrlEncode}}) ({{categ.count}})
{% endfor %}

## Filter posts by name

{% assign filtered = posts | with_name: arr %}
{% comment %} 
The previous assigment is equivalent to:

assign filtered = posts | where: "file_name_no_ext", arr

{% endcomment %}

{% assign filtered = posts | where: "tags", "Reference" %}

Check the file contents to see how it's done to return just `{{filtered.size}}` elements from hardcoded file names. In this case we use the file names to filter the collection of posts, but could have used any other property of the files or the data in them with the extra `where` filter I've added. The `with_name` filter it's just a shortcut for the whole filtering using the file name (without extension) as the property to be used for filtering.

The most common use of this filter is to return specific data from a collection of files, such as the current list of authors in blog posts and many other similar situations (check the `_data/authors/` folder, the `web.config` in this folder and the)
