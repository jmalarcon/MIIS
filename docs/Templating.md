---
title: How to create your own templates and fields
prefield: {{
postfield: }}
---
# Custom templates and fields

>Please, invest some time looking the out-of-the-box templates included with MIIS to see how the following fields are used.

Creating your own templates is really straightforward. You simply need to create one HTML file inside a sub-folder of the "Templates" folder, and include some placeholders for parameters. The only mandatory field that you must include in every template is the **`content`** field (more on this later).

MIS offers **two types of parameters and fields** that you can use anywhere in your site: **standard** and **custom**.

These parameters can be used **inside the templates** you create and **inside the content** of your Markdown or HTML (`.mdh`) files. You just need to **wrap their names with double curly brackets**, and you're all set. For example:

```
{{prefield}}title{{postfield}}
```
will be substituted for the title of your document before rendering.

There can be spaces between the curly brackets and the name of the field. The name of the field is case-insensitive. Therefore all the following expression are exactly the same parameter:

```

{{prefield}}title{{postfield}}
{{prefield}}Title{{postfield}}
{{prefield}}TITLE{{postfield}}
{{prefield}} title {{postfield}}
{{prefield}}Title {{postfield}}
```

Field names can only consist of letters from A to Z, numbers, hyphens and underscores.

## Standard fields
There are some MIIS-defined parameters and fields that already exist out-of-the-box, and that you can use anywhere in your site.

- **`Content`**: This is the only field that is **mandatory**. This placeholder is substituted at render time with the HTML contents obtained from the Markdown or .mdh file. Every template must have this once (and only once), or an error will be raised.
- **`TemplateBaseFolder`**: This one is very important since it will always point, from the root of the site, to the folder where your template is located. This means that you can very easily get relative paths to all the supporting files your template needs, such as CSS, JavaScript or Image files. This way, if your template is located under `/templates/MyGreatTemplate` this will the value that appears instead of the field's placeholder after rendering a file. You must then add the rest of the path to point to your supporting resources. It's only calculated once per site too, so it's very fast to use. Take into account that it doesn't include the trailing slash.
- **`{{tilde}}/`**: Any path that starts with this expression will be automatically substituted by base folder of the current web app. This will translate at runtime in the path, relative to the root of the domain, where the base folder of the site is hosted. If your site is hosted at the root of the domain (ie.: `http://www.mydomain.com`), it will be simply `/`. However if your site is hosted at a virtual folder or a virtual app (ie.: `http://www.mydomain.com/Docs/`) then this parameter will be translated into that path (`/Docs/` in the example). This is something a good designed template should take into account since you don't know if it will be always hosted at the root folder or not. This placeholder **can be used anywhere in your contents**. This is useful for using relative to the root links in your tables of contents or any other link.
- **`Title`**: The title for the page. You can set it in every page Front Matter (see later) or it'll be automatically guessed from the Markdown file. In ths case it'll be extracted from the first title expressed with a `#` symbol in the Markdown file (`# Title`). If no title is found then the name of the file will be used. You should normally set the title in the Front Matter of each content file.
- **`Filename`**: The name of the markdown file from which the final HTML is extracted. It includes the file extension.
- **`DateCreated`**: The creation date and time for the content file. It's shown in the default format for the current user language.
- **`DateModified`**: The date and time when the content file was last modified. It's shown in the default format for the current user language.
- **`IsAuthenticated`**: Boolean that shows if the current user is authenticated against the server or not. Useful to be included in scripts.
- **`AuthType`**: The type of authentication used (Forms, Windows, Google, Facebook...).
- **`UserName`**: The name of the current authenticated user.
- **`Domain`**: The current domain name or IP used to access the site. If the current port used to access the site is not a default one (80 for HTTP, 443 for HTTPS) it includes that port. For example, if you're accessing the site from `https://www.example.com/` it will return `www.example.com`, but if you're accesing it from `http://localhost:8080` then the result is `localhost:8080`.
- **`BaseUrl`**: The current base URL that is used to access the site. Useful for creating absolute paths to your contents (for example in canonical headers or Open Graph meta tags). It includes the protocol, domain and port if its not the default one. For example: `https://www.example.com`. It doesn't include the slash bar at the end.

## Custom Fields
The previous placeholders are managed and set by default by MIIS. However, **you can create as many custom fields as you want** in your templates or even directly in your content, and define their values globally or for the contents in a sub-folder using  the `web.config` file, or specifically for any content page using it's Front Matter.

For example, imagine you want to create a copyright notice that will appear at the bottom of your template. Just include a `copyright` placeholder (between curly brackets) wherever you want in your template HTML, and then set a value for it in the `web.config` file:

```
<add key="MIIS:copyright" value="© campusMVP.es 2017"/>
```

Now, every time this field's placeholder appears in your template (they can be used more than once) it's value will be replaced with the value in the configuration.

>**Note**: Remember: ASP.NET has a hierarchical configuration system. So you can add a different `web.config` for every sub-folder and have all the parameters customized differently per sub-folder.

You can set the value for any field or parameter directly in your content file using **Front Matter**. This is a special section at the very beginning of your file that allows to define pairs of fields and values. The format used is called [YAML](http://yaml.org){target="_blank"} and is very well-known among developers and site editors.

Front Matter is really easy and straight-forward to define. It's a section at the beginning of your file between three hyphens with pairs of field names plus values separated by a colon. For example:

```

---
# This is a comment
title: This is the title of this page
layout: leftToC.html
author: Jose M Alarcon
---
```

In this case the page defines a title to be used, a specific layout for the template (overwriting the standard parameter value in `web.config` just for this page), and creates a new field named `author` that can be used anywhere in the page (or in the current template).

>**Note**: Although YAML allows to define complex blocks of data, I've implemented a simple parser that only accepts values in a single line, which are the only ones valid in MIIS (at least at the moment). The names of the fields are case-insensitive, and any extra spaces are removed. Although the Front Matter specification allows only exactly three hyphens to be used as delimiters, I decided to make it more forgiving, and you can use any number (but more than 3) to define your Front Matter block. Front Matter **needs to be the first content of your file**. If located at any other point of the file, it will be considered normal content.

Those **custom parameters are really powerful** and allow you to fine-tune your templates and content in an easy way. Use them wisely.

## Includes in templates
When creating a template you could need **more than one single layout** for your contents. For example, your site could require pages with a menu and some other elements on one side, while other pages would need that the contents take up all the available width, and so on. In those cases all the layouts would probably share a lot of common HTML.

To avoid repeating the same HTML in every layout and make the template more maintainable MIIS offers **include placeholders**. These are a special type of placeholder that allows to include the content of other files in the template's folder in your current template.

Include placeholders are denoted by a `$` symbol followed by the relative path to the file you want to include. For example, this is the code for a layout that uses includes:

```

{{prefield}} $includes/beginning.html {{postfield}}
        <div id="sidebar">
        </div>
        <div id="main">
            {{prefield}}content{{postfield}}
        </div>    
{{prefield}} $includes/end.html {{postfield}}
```
In this case the common HTML for all layouts is taken from the files `beginning.html` and `end.html` located at the `includes` sub-folder.

>**Note**: the `$` symbol must be attached to the included file path, without spaces in it. As with any other field placeholder, spaces after and before the the curly braces are optional.

You can use **include placeholders in the include files** too, effectively reusing HTML at multiple levels. The only limitation is that you can't create circular references. MIIS will detect circular references and throw an error in that case.

Include placeholders is a powerful feature thar allows for easy reusing of the same HTML in all the layouts of a template.

## Fragments: Pages made up of several parts
Another powerful feature to be used in templates are **Fragments**. Fragments allow you to define parts (or fragments) of your page that you want to render independently.

The most common way to go in a site created with MIIS is to define a template with a `content` placeholder in it where the transformed HTML is going to be placed. However, a lot of times would be really useful being able to render different parts of the content at different locations in your layout, instead of all the content in the same place. This is when Fragments enter the scene to save the day.

A fragment is a field placeholder that starts with an asterisk attached to a suffix for the current file name, for example: `*-header` or `*_sidebar`, etc...

What this does is that, at the time of rendering the final contents for a file your users are requesting, MIIS will search for files that are named just like your main file, but with the suffix after the `*` in their name. It will try first the `.md` extension and the `.mdh` extension if the first it's not found. In the case that no appropriate file has been found, it will simply ignore the placeholder.

This is a very powerful feature. For example, imagine a layout for an e-commerce site that is designed to show product information. The product's layout consists of three main content areas: the header (with general information about the product and maybe an image), the main content area (with the detailed info for the product) and a sidebar content (with customers testimonials for this specific product). In this case you can use three content placeholders located at different parts of the HTML structure for the product layout in your template:

- **`content`**: that marks the place where the main content will be injected.
- **`*-header`**: marking the place to inject the header contents.
- **`*-testimonials`**: where the testimonials are going to be injected.

Now, in your site, when you define the contents for a product page (let's say `product-01.md`) you can add two more files with the same name and the indicated suffixes, for example: `product-01-header.mdh` and `product-01-testimonials.md`. The content of these two files will be processed and injected in the corresponding locations in the layout, creating a full page from three different (but related) contents.

Notice that the fragments can be `.md` or `.mdh` files, using Markdown or plain HTML as needed, and you can use one or another indistinctly. MIIS will find the correct one and use it. In case of conflict (two fragments with the same name but different file extension) the files with `.md` extension will take precedence over the files with `.mdh` extension.

If you need that, for example, one product page doesn't have a testimonials part, just don't create that fragment file. The placeholder will be ignored and your testimonials fragment will be empty in the layout.

>**Note**: Although no separator is needed between the `*` and the suffix, is advised to use a hyphen or an underscore (as shown in the previous example) to make it easier locating the fragments in disk when you're editing the files in your system.

It's important to note that **all those fragments will take part in invalidating the cache** for the current content file. This means that, as is expected, the cache for the file is invalidated and the file is rendered again from disk as soon as you change any fragment.

## File Fields: fields pointing to content files
If the value assigned to a field ends with `.md` it will be considered **a pointer to a Markdown file**. Then, that markdown file contents will be read and transformed into plain HTML (without using a template), before replacing the placeholder.

If the extension for the field value is `.mdh`, the content will be used as-is. 

This means that **you can insert content from files in your template or even in your content (sub-content)**. 

For example, as seen in the provided templates, you can define a field for your table of contents or any other secondary content with just a placeholder. 

>Take into account that you can use the `~` symbol to point to your root folder in the path indicated as a value for this kind of fields. In that way you can always be sure that the file you're pointing to is correctly defined even if you're inside any sub-folder or even if the site is hosted under a virtual folder. 

For a working example of these kind of fields take a look at any of the provided templates that include at least a table of contents.

For example, the Material template defines a parameter called `toc` that you can point to a Markdown (or `.mdh`) file that wll be used as a Table of Contents for your documentation site. The normal thing to do is to define the value for this paramenter in your root folder's `web.config` file (as done in the sample included in the release), for example:

```
<add key="MIIS:toc" value="{{tilde}}/toc.md"/>
```

but you can also define this field especially for one of your files that needs a different ToC for any reason using its Front Matter:

```

---
toc: myCustomToC.md
---
```

and this setting will take precedence over the global setting in the `web.config` file.

These File Fields feature is very powerful too and allows to define common reusable contents to be used in your whole site or in parts of it.

## Processing order
It's important to notice the order in which each part that makes up a final page is processed, which is:

1. **Template layout**
1. **Includes** in the layout if any
1. **Template specific fields** (`templateBaseFolder`)
1. **File contents** (Markdown processing or HTML reading)
1. **Fragments** if any
1. **Standard parameters**
1. **Custom Fields** and parameters

Take this into account when defining your layouts, fields and contents.

## Common Template JavaScript Helpers
Located in the root folder of the `Templates` folder delivered with MIIS, there are **two helper files** used by some of the included templates. You can take advantage of them for your current templates too.

They are:

### common.js
This file automatically does some interesting things to your doc:

- Makes all external links to open in a new tab
- Highlight the current selected link in the ToC. It automatically applies the `.current-doc` class to it. You must use this class in your template to highlight the current selected document in the Toc.
- Makes the first list items with sub-list collapsible
- Set the links and text in the previous and next links. For this to work the next button must have an `id='next-button'` and the previous button an `id='prev-button'`. If you want the link/button to have the next/prev elements titles assigned too, then it must have an element with `class="title"`.

>This file depends on jQuery, so you must include this library if your template doesn't already include it.

### common.css
This files does some UI tweaks in your template:

- Adds a small icon to indicate that a link is pointing to an external URL. It works in the table of contents (`.miis-toc` class) and in the main contents (`.miss-content` class).
- It automatically numbers the entries in the table of contents, with 1.-, 1.1.-, 2.- and so on. If you don't want this to happen just redefine the last four CSS selectors with counters in the CSS of your own template.

>It uses the font called "font-awesome", located in the '_common_fonts' sub-folder. If you use that font in your template you don't need to include it.