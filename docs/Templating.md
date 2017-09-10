# How to create your own templates

>Please, take the time to look any of the included templates to see how the following placeholders are used.

Creating your own templates is really straightforward. You simply need to create an HTML file with the CSS and JavaScript that you want and include some placeholders:

- **`{content}`**: This is the only one that is **mandatory**. This placeholder is substituted at runtime with the HTML contents obtained from the Markdown file. Every template must have this one or an error will be raised.
- **`{title}`**: The title for the page. It's automatically substituted by the guessed title for the Markdown file. This title is extracted from the first title expressed with a `#` symbol in the Markdown file: `# Title`. Other titles such as the ones created with underscores are not detected at this moment (known issue), just the ones with `#`. If no title is found then the name of the file will be used.
- **`{filename}`**: The name of the markdown file from which the final HTML is extracted. It includes the file extension.
- **`{datecreated}`**: The creation date and time for the Markdown file. It's shown in the default format for the current user language.
- **`{datemodified}`**: The date and time when the Markdown file was last modified. It's shown in the default format for the current user language.
- **`{isauthenticated}`**: boolean that shows if the current user is authenticated or not. Useful with scripts.
- **`{authtype}`**: the type of authentication used.
- **`{username}`**: the name of the current authenticated user.
- **`{basefolder}`**: The base folder of current web app in IIS. This will translate at runtime in the path, relative to the root of the domain, where the base folder of the site is hosted. If your site is hosted at the root of the domain (ie.: `http://www.mydomain.com`), it will be simply `/`. However if your site is hosted at a virtual folder or a virtual app (ie.: `http://www.mydomain.com/Docs/`) then this parameter will be translated into `/Docs/`. This is something a good designed template should take into account since you don't know it it will be always hosted at the root folder or not. This parameter is only calculated once for the site, so it's very fast to use.
- **`{templatebasefolder}`**: This one is very important since it will always point to the relative path of the folder where your template is located. This means that you can use relative paths to all the related files such as CSS, JavaScript or Image files very easily. It take into account the same considerations used with the previous parameter. This way if your template is located under `/templates/MyGreatTemplate/` this will be used to substitute this placeholder, and you can then add the rest of the path to pint to your related resources. It's only calculated once per site too, so it's very fast to use.

## Custom Placeholders
The previous placeholders are managed by MIIS and you can' re-define them or change their behavior. However, you can create as many custom placeholders as you want in your templates, and define their values in the `web.config` file.

For example, imagine you want to create a copyright notice that will appear at the bottom of your template. Just include a `{ copyright }` placeholder wherever you want in your template HTML, and then set a value for it in the `web.config` file:

```
<add key="copyright" value="© campusMVP.es 2017"/>
```

Now, every time this placeholder appears in your template (they can be used more than once) it's value will be replaced with the value in the configuration.

>**IMPORTANT**: If the value assigned to the placeholder ends with `.md` it will be considered **a pointer to a Markdown file**. Then, that markdown file contents will be read and transformed into HTML before replacing the placeholder. This means that **you can insert content from files in your template**. That allows you to insert tables of contents or any other secondary content with just a placeholder. Take into account that you can use the `~` symbol to point to your root folder, as you do in ASP.NET. In that way you can always be sure that the file you're pointing to is correctly defined even if you're inside any sub-folder or even if the site is hosted under a virtual folder. For a working example take a look at any of the custom templates that include at least a table of contents.

Those custom parameters are really powerful and allow you to fine-tune your templates in an easy way.

>**Note**: Remember: ASP.NET has a hierarchical configuration system. So you can add a different `web.config` for every sub-folder and have all the parameters customized differently per sub-folder.

## Common Template JavaScript Helpers
Located in the root folder of the templates there are two helper files used by some of the included templates. You can take advantage of them for your current templates too.

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
- It automatically numbers tha entries in the table of contents, with 1.-, 1.1.-, 2.- and so on. If you don't want this to happen just redefine the last four CSS selectors with counters in the CSS of your own template.

>It uses the font called "font-awesome", located in the '_common_fonts' sub-folder. If you use that font in your template you don't need to include it.