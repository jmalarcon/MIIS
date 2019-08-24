---
title: Sample pagination of files in this folder
author: JM Alarcon
paginator: !!PaginatorForFolder ./
paginate: 5
#caching: false
---

# This are the files of this folder, paginated

**{{ paginator.total_files }}** files in the folder{% if tag %}, filtered by tag `{{tag}}`{%endif%}

{%- if paginator.total_files == 0 -%}
Nothing to show here!
{%- endif -%}

Page {{ paginator.page }} of {{ paginator.total_pages }} with {{ paginator.per_page }} files per page:

{%- for file in paginator.Files -%}
- {{forloop.index}}: [{{file.title}}]({{file.url}}) - [{{ file.date | date: "dddd, dd MMMM, yyyy" }}]<br>{{file.excerpt | strip_newlines | truncate: 75 }}
{%- endfor -%}

{%- if paginator.previous_page -%}
**[Previous page](./{{paginator.previous_page}}{% if tag %}?Tag={{tag}}{%endif%})** 
{%- endif -%} ////&nbsp;{%- if paginator.next_page -%}**[Next page](./{{paginator.next_page}}{% if tag %}?Tag={{tag}}{%endif%})**
{%- endif -%} 

Direct links to pages: {% for np in (1..paginator.total_pages) %} **[{{np}}](./{{np}}{% if tag %}?Tag={{tag}}{%endif%})** {% endfor%}

It works with Tags and Categories too. For example, **[filtered by a Tag](?Tag=conceptual){target="_blank"}**.

In this case I've setup a URL Rewrite rule to use friendly URLs (please, notice the URL in your browser), but the default way to do it is by using Query String parameters: `?page={%raw%}{{paginator.next_page}}{%endraw%}`
