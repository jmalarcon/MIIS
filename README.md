# MIIS: A Markdown File-based CMS for IIS and Azure

![MIIS Logo](MIIS_Logo.png)

A Markdown and HTML file-based CMS system for IIS by [@jm_alarcon](https://twitter.com/jm_alarcon).

> **IMPORTANT**: This repository uses two Git Submodules [IISHelpers](https://github.com/jmalarcon/IISHelpers) and [MIISdotliquid](https://github.com/jmalarcon/MIISdotliquid) that are needed as dependencies. They are Git submodules so, when cloning the repo for the first time, you should use: `git clone --recursive https://github.com/jmalarcon/MIIS.git` so that the source code for these dependencies is cloned into the correspondent subfolders at the root level. You can later use `git submodule update --recursive --remote` to keep them updated with the latest changes.

MIIS is a perfect blend between a traditional CMS (such as Wordpress) and a Static Site Generator (such as Jekyll). Get the best of both worlds:

- No backend database or special setup needed
- No need to recompile and deploy after any change
- Lightning-fast, without unneeded bloat
- Support for Liquid tags, templates with inheritance and many more features similar to Jekyll

**Set up your Markdown-based documentation system or web site in less than 30 seconds!**


## System Requirements

- **Internet Information Server** on Windows Server, or an **Azure Web App**
- **.NET Framework 4.5** or later (usually already installed on the server)
- **[IIS URL Rewrite extension](https://www.iis.net/downloads/microsoft/url-rewrite)** (you can install it directly with the [Web Platform Installer](https://www.microsoft.com/web/downloads/platform.aspx) too)

## Features

- Create **full-fledged web sites** and documentation sites based on Markdown and HTML files
- Easy support for **custom templating and navigation** for the served files. You can **create a CMS** (Content Management System) directly from Markdown files in no time!
- Generate site pages on the fly from Markdown files or HTML (`.mdh` files), with navigation and all the common elements in the site
- **Super-flexible and easy template creation**, with "includes" support, Liquid tags, parameters, file enumeration, inheritance...
- Custom properties that can be used in any file
- **Powerful "Fragments" and "Component" features** that allows to assemble contents dynamically from several files.
- Use query string or form parameters, cookies and server variables as part of your content (and to make decisions with DotLiquid).
- Process files in folders from Markdown or HTML: to create a blog, create multilingual sites, manage authors...
- Markdown file caching and template caching for **maximum performance**
- **Customize per folder**: define different look&feel, layout, navigation, fields/properties for specific files or folders [using different `web.config` files](https://blog.elmah.io/web-config-location-element-demystified/).
- Several documentation site templates included out of the box
- **Great support for Markdown** and [Markdown extras](Markdown-Features) for the content

Check the **[full documentation](http://miis.azurewebsites.net/)**.

## Showcase

These are some sites that run with MIIS:

- [MIIS Documentation Site](https://miis.azurewebsites.net/). Azure App.
- [campusMVP](https://www.campusmvp.es/): On-line training company. More than 300K unique users monthly. Windows Server.
- [jmalarcon.es](https://jmalarcon.es/): My personal (non-technical) blog. Azure App.
- [SELF LMS](https://www.plataformaself.com/): Site and blog for a Learning Management System software. Windows Server.
- [IISMailer](https://iismailer.com/): Form processing software for ISS website and docs. Azure App.
- [Krasis.com](https://www.krasis.com/): Basic corporate website. Windows Server.
- [Alquiler Sanxenxo](https://www.alquilersanxenxo.com/): Seaside flat rental site. Azure App.
- [Krasis Intranet](https://krasisintranet.azurewebsites.net/): private, OAuth 2.0 protected Intranet site. Azure App.

**If you use MIIS** to create a website, the documentation for your project, a document-based site, etc... please **[tweet it to me](https://twitter.com/jm_alarcon)** with the URL 😊 Thanks!
