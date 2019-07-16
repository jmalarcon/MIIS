---
title: Sample folder for Files custom Front-Matter field
author: JM Alarcon
posts: !!FilesFromFolder ./
---

# Sample folder for Files custom Front-Matter field

>These files have been retrieved and minimally adapted from this repo: https://github.com/NuGet/docs.microsoft.com-nuget, just for testing purposes

This is a sample file to show all the contents of an specific folder. In this case I'm using `"./"` as the folder param for `FilesFromFolder` FM custom param. In the parent folder you should use the name of the folder. This `index.md` file shouldn't be in the listing:

**{{posts.Count}}** posts:

<ul>
{% for post in posts %}
    <li>
        <a href="{{post.URL}}">{{ post.Title }} - [{{post.Date}}]</a><br>
        {{post.Excerpt}}
    </li>
{% endfor %}
</ul>