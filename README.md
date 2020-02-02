# MIIS: A Markdown File-based CMS for IIS and Azure

![MIIS Logo](MIIS_Logo.png)

A Markdown and HTML file-based CMS system for IIS by [@jm_alarcon](https://twitter.com/jm_alarcon).

> **IMPORTANT**: in order to be able to compile the source code of this project you first need to download the code of the complementary project [MIISDotLiquid](https://github.com/jmalarcon/MIISdotliquid). You must clone it in a `MIISDotLiquid` folder inside the same folder of MIIS project:
>
> ![project-folders](project-folders.png)
>
> This is a related project for the Liquid tags processor and is referred by folder name in the project's file.

MIIS is a perfect blend between a traditional CMS (such as Wordpress) and a Static Site Generator (such as Jekyll). Get the best of both worlds:

- No backend database or special setup needed
- No need to recompile and deploy after any change
- Lightning-fast, without unneeded bloat
- Support for Liquid tags, templates with inheritance and many more features similar to Jekyll

**Set up your Markdown-based documentation system or web site in less than 30 seconds!**


## System Requirements

- Internet Information Server on **Windows Server** or an **Azure Web App**
- **.NET Framework 4.5** or later (usually already installed on the server)

## Features
- Create **full-fledged web sites** and documentation sites based on Markdown and HTML files
- Easy support for **custom templating and navigation** for the served files. You can **create a CMS** (Content Management System) directly from Markdown files in no time!
- Generate site pages on the fly from Markdown files or HTML (`.mdh` files), with navigation and all the common elements in the site
- **Super-flexible and easy template creation**, with "includes" support, Liquid tags, parameters, file enumeration...
- Custom properties that can be used in any file
- **Powerful "Fragments" and "Component" features** that allows to assemble contents dynamically from several files
- Markdown file caching and template caching for **maximum performance**
- **Customize per folder**: define different look&feel, layout, navigation, fields/properties for specific files or folders
- Several documentation site templates included out of the box
- **Great support for Markdown** and [Markdown extras](Markdown-Features) for the content

Check the **[full documentation](http://miis.azurewebsites.net/)**.
