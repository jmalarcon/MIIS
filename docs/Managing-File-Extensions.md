# Configuring Markdown File Extensions
By default the most usual file extensions for Markdown files are defined. They are the same ones as the officially supported by GitHub: `.md, .markdown, .mdown, .mkdn, .mkd`.

However you can remove the ones that you don't need, or add new one as easily if you want to.

Just open the `web.config` file and locate the `system.webServer/handlers` section. It looks like this in the default configuration file:

```
<system.webServer>
    <handlers>
      <add name=".md extension" path="*.md" verb="GET" type="IISMarkdownHandler.IISMarkdownHandler, IISMarkdownHandler" resourceType="File" requireAccess="Read"/>
      <add name=".markdown extension" path="*.markdown" verb="GET" type="IISMarkdownHandler.IISMarkdownHandler, IISMarkdownHandler" resourceType="File" requireAccess="Read"/>
      <add name=".mdown extension" path="*.mdown" verb="GET" type="IISMarkdownHandler.IISMarkdownHandler, IISMarkdownHandler" resourceType="File" requireAccess="Read"/>
      <add name=".mkdn extension" path="*.mkdn" verb="GET" type="IISMarkdownHandler.IISMarkdownHandler, IISMarkdownHandler" resourceType="File" requireAccess="Read"/>
      <add name=".mkd extension" path="*.mkd" verb="GET" type="IISMarkdownHandler.IISMarkdownHandler, IISMarkdownHandler" resourceType="File" requireAccess="Read"/>
    </handlers>
    <defaultDocument enabled="true">
      <files>
        <add value="index.md"/>
      </files>
    </defaultDocument>
  </system.webServer>
```

You can add or remove extensions as needed.

## Default Documents

Notice the `defaultDocument` setting in this section. It defines the name of the default files to be served from your site. So, in the default configuration as seen above, it will serve the `index.md` file without explicitly requesting it in any folder:

```
http://www.midomain.com/
http://www.midomain.com/SubFolder/
```

are equivalent to:

```
http://www.midomain.com/index.md
http://www.midomain.com/SubFolder/index.md
```

You can change this name or add more default names to be used in the same order as they appear in this setting.

## Allow Downloading of Markdown Files
Chances are that your IIS has no entries for Markdown file extensions in its MIME Types configuration. In order to allow downloading the  files if you enable this option, you need to add the Markdown MIME type for all the Markdown file extensions. This is already taken care for you in the default `web.config` file included in the release folder for MIIS.

You can get rid of it just removing the `<staticContent>` section in the configuration file.