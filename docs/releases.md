---
title: Release history
field: {{field}}
---
# MIIS Release History
MIIS uses [semantic versioning](https://semver.org){target="_blank"}. That means it only changes the major version when there're breaking changes. A change in the minor version means new features. A change in the third number means no new features, just bug fixes.

>You can check all the releases, including the minor ones, at [Github](https://github.com/jmalarcon/MIIS/releases){target="_blank"}.

## Version 2.0

Released, March 2018

## - Breaking changes
- **New app DLL name**: `MIISHandler.dll` instead of the old one `IISMarkdownHandler.dll`. Make sure you delete the old one before updating! You must change the handlers section in your `web.config` file. Just copy it from the downloaded MISS released files.
- **Placeholders now use double curly-braces** (`{{field}}`) instead of just one. Upgrade your templates before updating.
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
