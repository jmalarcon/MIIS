---
title: Out-of-the-box templates
---
# List of templates available out of the box

> Remember: you can [serve plain, styled HTML](Plain-HTML) just by not specifying any Layout parameter.

> Otherwise noted, the following templates offer a single layout called `main.html`.

## :page_facing_up: Barebones
A simple template that uses the same `cssfile` parameter (as the default one) and adds information about the Markdown file in the footer of every page and a link to download the original markdown file (you must [enable this feature](Settings#allowdownloading) or get rid of this link in the template).

![Sample footer from a MD file rendered with the barebones template](Images/Templates/Barebones_Footer.png)

#### Available parameters:
This template has the following parameters that you can set in `web.config` files or in each file's Front Matter:

- `title`: the page's title.
- `sitetitle`: the site's title.
- `cssfile`: to define the CSS file controlling the resulting HTML look and feel.

## :page_facing_up: ReadTheDocs
A template that mimics the look and feel of the old [Read The Docs](https://readthedocs.org/) site. Reused from the [MKDocs project](http://www.mkdocs.org/) and extended with extra functionality.

![Old ReadTheDocs template](Images/Templates/ReadTheDocs.png)

With this template you can set up a simple documentation system in just under a minute.

### Layouts
This template offers two layouts:

- `main.html`: the main layout.
- `main_forkme.html`: the same as the previous one but with a "Fork me" icon on the right top corner that points to your GitHub user or project (see the parameters in the next section).

### Available parameters and fields:
This template has the following parameters and fields that you can set:

- **`sitetitle`**: a title for the site. Used in the upper part of the lateral navigation menu and in all the pages title before the title of the document. Commonly set globally in the root folder's `web.config` file.
- **`toc`**: points to a .md file to create the side navigation menu. Commonly set globally in the root folder or sub-folder's `web.config` files. The path to the Table of Contents (ToC) file can use the `~` syntax to always point to the same file even from sub-folders. For example, setting the parameter this way will use the file `toc.md` located at the root of the site as the lateral navigation for the template in every page of the site when rendered with this template: 

```
<add key="MIIS:toc" value="{{tilde}}/toc.md" />
```

- **`description`**: the description to be used in the `<meta>` tag of the page. Commonly set in the Front Matter of the page.
- **`author`**: the name of the author of the site or page to use in the corresponding `<meta>` tag.
- **`copyright`**: a copyright message to show in the footer of every page.
- **`authorgithub`**: the part after `https://github.com/` to use in the github link of the template in the `main_forkme.html` layout. If you don't want the GitHUb icon to appear just use the `main.html` layout.
- **`gaaccount`**: the user account ID for Google Analytics. Usually has the name "UA-XXXXX-YY". This will allow you to receive usage statistics for your documentation site. Commonly set globally in the root folder's `web.config` file, since this ID is used in every single page served by MIIS fro your site.

## :page_facing_up: Material
This is a material-inspired template. It gives a clean and modern UI and allows to choose among several colors with one of its parameters.

![Material](Images/Templates/Material.png)

### Extra features:
- It highlights the link on the right side pointing to the current shown document, and collapses the second level lists of links that are not in the same group of that one (only if the first-level item is a link itself). That allows to use long tables of contents with sub-sections.
- It automatically numbers the sections and subsections so you don't need to do it in the table of contents files, adapting automatically to changes in the ToC order. You can disable this feature just by commenting the last 4 CSS rules in the `theme_extra.css` file of the template.
- Shows "Previous" and "Next" buttons in the footer, including the title of those documents, for easy navigation. It takes that information from the ToC, getting the position of the current file in it.
- It shows a small icon after links that point to external domains to highlight this fact. It makes all those external links to automatically open in a new tab.
- Automatically centers images. Adds zoom to images that don't fit in the available width (with [Zoomify.js](https://github.com/indrimuska/zoomify)).
- If you use the `<video>` tag, your users can start and stop videos just by clicking on them.
- If you include any empty link in your contents it's automatically converted into a "Go back" link (moving back one position in the browser's history) with the text that you have used. For example: `[Go to the previous page]()`.
- Printer friendly: when your users print the current page they will get a clean and readable page with the contents and without the extra elements.

With this template you can set up a full-fledged documentation system in just under a minute. It's the one used in [this documentation](https://miis.azurewebsites.net){target="_blank"}.

### Available parameters:
This template has the following parameters that you can set:

- **`sitetitle`**: a title for the site. Used in the upper part of the lateral navigation menu and in all the pages title before the title of the document.
- **`logo`**: the path, from the base folder, of the logo we want to use in the side bar. It should be squared, and the template will fit it to the available space. If it's located in the root folder, for example, the value would be simply the name of the image file.
- **`toc`**: points to a .md file to create the side navigation menu. See previous template explanation.
- **`primarycolor`**: the main color for the Material theme. It can use any of the main Material colors (see next picture). Color names for this parameter are written in lowercase, and if they have two words the space is substituted by a "-". For example, "Blue Grey" would be "blue-grey".

![Material colors](Images/templates/Material-Colors.png)

- **`accent`**: the accent for the selected color. Its used sparsely in the template (for example in the links). The available colors are:

![Material colors](Images/templates/Material-Accent-Colors.png)

- **`description`**: the description for the `<meta>` tag of the page.
- **`author`**: the name of the author of the site to use in the corresponding `<meta>` tag.
- **`copyright`**: a copyright message to show in the footer of every page.
- **`authortwitter`**: the twitter handler of the site to use in the Twitter button link.
- **`authorgithub`**: the part after `https://github.com/` to use in the github link of the template. If you don't want the github icon to appear you must delete that from the `main.html` file of the template.
- **`prevtext`**: the text to use in the "go to previous" button in the footer
- **`nexttext`**: the text to use in the "go to the next" button in the footer
- **`gaaccount`**: the user account ID for Google Analytics. Usually has the name "UA-XXXXX-YY". This will allow you to receive usage statistics for your documentation site.

More templates to come in the future!