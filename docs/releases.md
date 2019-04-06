---
title: Release history
field: {{field}}
prefield: {{
postfield: }}
---
# MIIS Release History
MIIS uses [semantic versioning](https://semver.org){target="_blank"}. That means it only changes the major version when there're breaking changes. A change in the minor version means new features. A change in the third number means no new features, just bug fixes.

>You can check all the releases, including the minor ones, at [Github](https://github.com/jmalarcon/MIIS/releases){target="_blank"}.

## Version 2.3.0

Released, April 6th 2019

- Removed the `allowDownloading` option and the corresponding "download" query string parameter.
- Now you can disable the current template in a page by specifying `TemplateName: none´ in the Front-Matter. This will use the default HTML5 basic template that only includes de basic tags, a CSS file and the content. This is useful for testing purposes or to create some basic pages for special purposes.
- If you specify `TemplateName: raw´ in the Front-Matter, then **no template will be used** and you'll get the final raw HTML for the requested page. useful for debugging and to return raw information.
- When you use the "raw" template to get raw contents from a file, you can also add a new `{{prefield}}mime{{postfield}}` parameter **in the page** (it's a Front-Mater only field) that allows you to specify the MIME type of the raw content that is going to be returned from the current page. This parameter can be useful to return formatted text that gets generated from a raw `.mdh` file.
- 6 new [standard fields](/Templating#standard-fields) available to use in templates and content pages:
    - `{{prefield}}Url{{postfield}}`
    - `{{prefield}}NoExtUrl{{postfield}}`
    - `{{prefield}}Domain{{postfield}}`
    - `{{prefield}}BaseUrl{{postfield}}`
    - `{{prefield}}Now{{postfield}}`
    - `{{prefield}}Time{{postfield}}`
- Fixed extra new line added at the begnining of the content because of Front-Matter removed. No extra new line is added now.

## Version 2.2.0

Released, August 20th 2018

- Added the **new ["HttpStatusCode" property](https://miis.azurewebsites.net/Settings#httpstatuscode)** that allows pages to send an specific status code to the client, such as 404, 410, 500, etc... in order to create special pages with specific purposes.


## Version 2.1.0

Released, March 24th 2018

- Added the **new ["Published" property](https://miis.azurewebsites.net/Settings#published)** that allows you to prevent certain files or entire folders to be rendered when requested.
- Squashed a bug with templates' caching preventing some files to invalidate their cache

## Version 2.0

Released, March 2018

## - Breaking changes
- **New app DLL name**: `MIISHandler.dll` instead of the old one `IISMarkdownHandler.dll`. Make sure you delete the old one before updating! You must change the handlers section in your `web.config` file. Just copy it from the downloaded MISS released files.
- **Placeholders now use double curly-braces** (`{{prefield}}field{{postfield}}`) instead of just one. Upgrade your templates before updating.
- **The `Markdown-Template` parameter doesn't exist anymore**. Now it uses two independent parameters for the template location and the layout, being much more flexible. See [configuration/settings](Settings).
- the `BaseFolder` field is deprecated. Use `{{tilde}}/` instead.
- The `BaseFolder` and  `TemplateBaseFolder` fields do not include a slash `/` at the end. This makes the paths more readable.
- The `UseMDCaching` and `AllowEmojis` parameter's value are `"1"` by default (enabled), so you don't need to specifcy them most of the time.

## - What's new
- **Includes in templates**. Now you can reuse any part of your template to define different layouts using the same base code. The include files can include other files too (no circular-references allowed).
- **"Fragments"**. This powerful new concept allows you to define several contents to be located in the same layout to form a single page, even with optional parts. 
- **Front-Matter**: Define any parameter in the Front-Matter of a file and use it inside the content or in the current template. Re-define any global parameter or field to be applied differently in any page. For example, you can define something as simple as the title or author of the page, or even change the layout it uses to render itself. Front-Matter values take precedence over values defined globally.
- **`MIIS:` prefix to define global parameters & fields in `web.config` files**. To avoid conflict with parameters of the same name in other software you may be using. The use of this prefix is optional but recommended. It'll take precedence over the non-prefixed parameters with the same name in case of conflict.

## Version 1.1

Released September 2017

- **Added support for plain HTML content** through the special `.mdh` extension. If you access a file with the `.mdh` extension it will be used with the current template and/or styles un-transformed. In this way you can use plain-old HTML with your current template for special pages that need a finer control of the contents. Useful for front pages, for example.
- **New fields for templates**:
    - **`{isauthenticated}`**: boolean that shows if the current user is authenticated or not. Useful with scripts.
    - **`{authtype}`**: the type of authentication used.
    - **`{username}`**: the name of the current authenticated user.


## Version 1.0

Released March 2017
