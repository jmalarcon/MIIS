# Configuring Markdown File Extensions
By default the most usual file extensions for Markdown files are defined. They are the same ones as the officially supported by GitHub: `.md, .markdown, .mdown, .mkdn, .mkd`.

However you can remove the ones that you don't need, or add new one as easily if you want to.

Just open the `web.config` file and locate the `system.webServer/handlers` section. It looks like this in the default configuration file:

```
<system.webServer>
    <handlers>
      <add name=".mdh HTML extension" path="*.mdh" verb="GET" type="MIISHandler.MIISHandler, MIISHandler" resourceType="File" requireAccess="Read"/>
      <add name=".md extension" path="*.md" verb="GET" type=""MIISHandler.MIISHandler, MIISHandler" resourceType="File" requireAccess="Read"/>
      <add name=".markdown extension" path="*.markdown" verb="GET" type=""MIISHandler.MIISHandler, MIISHandler" resourceType="File" requireAccess="Read"/>
      <add name=".mdown extension" path="*.mdown" verb="GET" type=""MIISHandler.MIISHandler, MIISHandler" resourceType="File" requireAccess="Read"/>
      <add name=".mkdn extension" path="*.mkdn" verb="GET" type=""MIISHandler.MIISHandler, MIISHandler" resourceType="File" requireAccess="Read"/>
      <add name=".mkd extension" path="*.mkd" verb="GET" type=""MIISHandler.MIISHandler, MIISHandler" resourceType="File" requireAccess="Read"/>
    </handlers>
    <defaultDocument enabled="true">
      <files>
        <add value="index.md"/>
      </files>
    </defaultDocument>
  </system.webServer>
```

You can add or remove extensions as needed.

## The `.mdh` special extension
By default (if not changed in the previous section of the `web.config` file) MIIS supports a special file type for pure HTML contents. Anything inside a `.mdh` file will be used un-transformed within the current assigned template. This is very useful for pages that need a very specific HTML structure, such as the main front-page of a site or any other complex page. With this kind of files you'll use HTML instead of Markdown to gain control over the final HTML and can keep the indentation of the code (in Markdown indented HTML code would be interpreted as a code fragment).

If you are using this kind of files and Visual Studio Code is your editor of choice, there's a simple way to achieve that VSCode will treat this files as normal HTML files, giving you Intellisense, Emmet, and all the nice features you love.

Just open your VSCode settings and add this node to the JSON file:

```
 "files.associations": {
    "*.mdh": "html"
}
```

Form now on, you'll get the normal behavior of HTML files while editing `.mdh` files too.

## Default Documents

Notice the `defaultDocument` setting in this section. It defines the name of the default files to be served from your site. So, in the default configuration as seen above, it will serve the `index.md` file without explicitly requesting it in any folder:

```
http://www.mydomain.com/
http://www.mydomain.com/SubFolder/
```

are equivalent to:

```
http://www.mydomain.com/index.md
http://www.mydomain.com/SubFolder/index.md
```

You can change this name or add more default names to be used in the same order as they appear in this setting.

## Allow Downloading of Markdown Files
Chances are that your IIS has no entries for Markdown file extensions in its MIME Types configuration. In order to allow downloading the  files if you enable this option, you need to add the Markdown MIME type for all the Markdown file extensions. This is already taken care for you in the default `web.config` file included in the release folder for MIIS.

You can get rid of it just removing the `<staticContent>` section in the configuration file.