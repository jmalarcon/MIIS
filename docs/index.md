# MIIS: A Markdown based CMS for IIS

![MIIS Logo](Images/MIIS_Logo.png)

A Markdown based CMS system for IIS by [@jm_alarcon](https://twitter.com/jm_alarcon).

**Your Markdown-based documentation system or web site in less than 30 seconds!**

## System Requirements

- Internet Information Server 6.0, 7.0, 7.5, 8.0 or later
- .NET Framework 3.5 or later (usually already installed on the server)

## Features
- Out of the box support for all **the most common markdown file extensions**: `.md, .markdown, .mdown, .mkdn, .mkd`. You can [add your own extensions](Managing-File-Extensions.md)
- Serve **plain HTML from Markdown**
- Serve **enriched HTML applying any CSS** styles (several included out of the box).
- Easy support for **custom templating and navigation** for the served files. You can **create a CMS** (Content Management System) directly from Markdown files in no time! Several site templates included.
- Template caching for **maximum performance**
- Optional Markdown file caching
- **Great support for Markdown** and [Markdown extras](Markdown-Features.md)

## Quick Start

Your Markdown-based site running in less than 30 seconds:

1. Create an IIS virtual server pointing to the folder containing your Markdown files
2. Download the latest version of **MIIS** from the ["Releases" section](https://github.com/jmalarcon/MIIS/releases) of the MIIS GitHub repository
3. Uncompress the contents of the ZIP file inside the folder from step 1
4. Navigate to your website with the browser. You're all set up!

You can mix Markdown files with HTML, ASP.NET, PHP or any other server-side resource.

## Advanced set-up

- [Configuration/Settings](Settings.md)
- [Available templates](Template-List.md)
- [Define custom templates](Templating.md)
- [Manage Markdown file extensions](Managing-File-Extensions.md)
- [How to run locally with IISExpress](IISExpress.md)
- [How to run in Azure](Azure.md)

## Known Issues
- Only "#" syntax supported for guessing the current Markdown file title.

## Source code
This is a free and Open Source software. You can check the full code and documentation on [GitHub](https://github.com/jmalarcon/MIIS).

## Contribute
You can contribute to the project with bug fixes, **new templates**, new features and **translations** of this documentation to other languages. Follow the normal flow of OSS contributions in GitHub (fork, make changes, pull request).

I plan to add a "Showcase" section in the future. **If you use MIIS** to create the documentation for your project, a document-based site, etc... please **tweet it to me** with the URL. 

And spread the word. Thanks! :simple_smile:

You can open any issues you may face, using the ["Issues" section](https://github.com/jmalarcon/MIIS/issues) for the project on GitHub.