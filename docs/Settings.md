---
title: Configuring MIIS
---
# Configuration settings and behavior for MIIS

MIIS needs some basic parameters to be set in order to work. You can set any parameter globally, through `web.config` files, or locally in any file through it's Front Matter.

**Configuration in MIIS is hierarchical in three levels:** root folder, subfolders and pages. 

This means that you can define your **global parameters** in the root folder's `web.config` file, define new parameters or overwrite the global ones **in any sublevel** through the corresponding `web.config` inside any sub-folder, and you can define or overwrite any parameter **at the page level** using the file's Front Matter.

This hierarchical configuration system is **very powerful** and you can use it to define custom content, to change the navigation system, to choose the template used to render contents in specific sub-folders or files... or even to fully customize any file with specific fields or parameters.

There are some **pre-defined parameters** in MIIS, and you can create your own [**custom fields**](Templating#custom-fields).

> Parameter and field names are **case insensitive**. So, `Layout`, `layout` or `LAYOUT` are exactly the same parameter.

> **`MIIS:` parameter prefix**: any parameter or field defined in a `web.config` file can be named with a `MIIS:` prefix. This helps to avoid conflicts with parameters with the same name from other software you may be using. Using this prefix is optional, but recommended. It'll take precedence over the non-prefixed parameters with the same name in case of conflict. You can't use this prefix in your file's Front Matter. Your file's Front Matter always takes precedence over other parameters or fields defined in `web.config`.

## Out-of-the-box predefined parameters

### - TemplatesBasePath
This parameter is only available through `Web.config`, and can't be overwritten in a file's Front Matter. It defines the folder that contains the definition for the different templates available to MIIS for rendering pages.

It's default value is `{{tilde}}/Templates`.

This means that, by default, the templates are located inside a folder named "Templates" in the applications root directory. Normally you won't change this parameter at all.

>**Note**: This parameter can be only set in `web.config` files. If you set it in a file's Front Matter it will be ignored for the current purpose.

----

### - TemplateName
This is the name of the subfolder in the previous `TemplatesBasePath` folder, that contains the layout files and the rest of resources for the template we want to use in our site. 

>If this this parameter is not established, then a basic minimum HTML5 template is used. See: [Serving plain HTML from Markdown](Plain-HTML){target="_blank"}

----

### - Layout
The name of the file (including extension) in the previous folder that contains **the HTML that defines the current layout to render file contents**.

This parameter allows you to point to an HTML file (or any other text file with HTML inside) that will be used **to merge it's HTML with the HTML generated from the Markdown files** or with the HTML inside the MDH files.

There are **[several templates included](Template-List.md)** by default with MIIS, and you can **[create your own](Templating)** from scratch or to retrofit any existing website.

----
 
### - UseMDCaching
By default MIIS caches the results of rendering any page so that they can render instantly after the first request (no conversion, parameter substitution or processing in every request).

If the file (or any of the files it depends on, such as menus, fragments...) changes, then **the cache is automatically invalidated** so that the new version is read again from disk in the next request.

If for any reason you need to turn off this behavior (very low memory environments) just use this in your `web.config` file:

```
<add key="MIIS:UseMDCaching" value ="0"/>
```

>This parameters is global and cannot be set individually in the Front-Matter of a single file. You can set it only in `web.config` files, and can disable caching for entire sub-folders (for example, one with thousands of files non frequently accessed), or the application as a whole.

----

### - allowDownloading
By default you can't download Markdown files from the server (the source of your final pages). But sometimes it can be useful to allow your users to download the original Markdown files, to use them directly, or to create new versions, etc...

You can switch this feature on through this parameter:

```
<add key="MIIS:allowDownloading" value="1"/>
```

After that, any Markdown file [registered in your app](Managing-File-Extensions) can be downloaded just adding the `?download=1` query string parameter in the file request.

You can see this feature in practice if you enable this feature and use the provided "Barebones" template, that includes a link to download Markdown files in the footer of every document.

>**IMPORTANT**: in order for the Markdown files to be downloaded you need to add the corresponding MIME type to your web server. CHeck out [how to do it](Managing-File-Extensions#allow-downloading-of-markdown-files).

----

### - UseEmoji
By default MIIS will render [standard Emoji codes](https://www.webpagefx.com/tools/emoji-cheat-sheet/){target="_blank"} like the ones used in Github, Trello, Slack, Basecamp and other software. So, strings like `:smile:` or `:grin:` are rendered as the corresponding emojis: :smile: - :grin:.

You can turn this feature off if needed using this parameter:

```
<add key="MIIS:UseEmoji" value="1" />
```

----

### - Published
By default all the Markdown and MDH files are published. You can prevent any file or set of files to be rendered by using this parameter.

You can include this parameter in the Front Matter of a file to prevent it to be rendered:

```
---
Title: My draft page
Published: false
---
```

If anyone writes the path to this file in the browser they will get a 404 Status error of "File not found".

Any value different to `false`, `no` o `0` will be considered as valid to publish the file. The default value if the parameter is not defined is `true` and will render the page normally.

You can set this parameter globally or for specific folders using `web.config` and adding:

```
<add key="MIIS:Published" value="0" />
```

(any of the previously specified values are valid)

Doing this will prevent the rendering of any file in the folder where this `web.config` is located, except those ones that specifically define `Published: true` or a similar value in their Front Matter. This can be very useful under some circumstances.

----

## Standard and Custom Fields

There are some basic **[standard fields](Templating#standard-fields)** that you can use anywhere in your contents or templates.

And most of the provided content templates offer **[their own parameters](Template-List)** to customize a little more the final look and feel

Finally, you can easily **[define your own custom fields](Templating#custom-fields)** too, and use them anywhere (templates or documents).

