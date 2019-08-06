---
# Change the default naming convention (Ruby) to force the C# naming convention instead
# In this case the property names are: PropertyOne instead of property_one
# It affects filters and Liquid syntax in general too!
Naming: CSharp
title: Sample pagination of files in this folder
author: JM Alarcon
paginator: !!PaginatorForFolder ./
caching: false
---

# This are the files of this folder, paginated

**{{ paginator.TotalFiles }}** files in the folder

{%- if paginator.TotalFiles == 0 -%}
Nothing to show here!
{%- endif -%}

Page {{ paginator.Page }} of {{ paginator.TotalPages }} with {{ paginator.PerPage }} files per page:

{%- for file in paginator.Files -%}
- {{forloop.index}}: [{{file.Title}}]({{file.URL}}) - [{{ file.Date | Date: "dddd, dd MMMM, yyyy" }}]<br>{{file.Excerpt | StripNewlines | Truncate: 75 }}
{%- endfor -%}

{%- if paginator.PreviousPage -%}
**[Previous page](./{{paginator.PreviousPage}})** 
{%- endif -%} ////&nbsp;{%- if paginator.NextPage -%}**[Next page](./{{paginator.NextPage}})**
{%- endif -%} 

Direct links to pages: {% for np in (1..paginator.TotalPages) %} **[{{np}}](./{{np}})** {% endfor%}

It works with Tags and Categories too. For example, **[page 2 filtered by Tag](?Tag=reference)**.

In this case I've setup a URL Rewrite rule to use friendly URLs (please, notice the URL in your browser), but the default way to do it is using Query String parameters, for example:

```markdown
{%- raw -%}
{%- if paginator.PreviousPage -%}
**[Previous page](?page={{paginator.PreviousPage}})** ////// 
{%- endif -%} 
{%- if paginator.NextPage -%}
**[Next page](?page={{paginator.NextPage}})**
{%- endif -%}
{%- endraw -%}
```
