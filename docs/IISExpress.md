# Run MIIS locally with IISExpress

While you are creating your documentation locally, in your computer, is useful to see exactly how it looks like while editing it. For this purpose, using a lightweight and temporary server is something really useful.

**IISExpress** is a stand-alone version of Internet Information Services that runs locally and non-permanently. It comes bundled with Visual Studio or you can download it from **[here](https://www.microsoft.com/en-us/download/details.aspx?id=48264)**.

To launch IISExpress and show your site locally just create a text file with the `.bat` o `.cmd` extension  and type the following command:

```
"C:\Program Files (x86)\IIS Express\iisexpress.exe" /path:"C:\Path-To-Your-MIIS-Docs-Folder" /port:8081 /clr:4.0
```

Just use the full path to the folder containing the MIIS runtime and your docs.

Now open your favorite browser and browse to:

```
http://localhost:8081
```

You'll see your documentation site right away.

You can change the port the site is served from modifying the `/port` parameter in the previous file.

>Please, notice that the site is not automatically refreshed, so if you change the contents of a document you must refresh the browser to see the changes. Also IISExpress can be caching many of your resources (images, scripts...). In that case open the Developer Tools of your browser and check the "Disable Cache" option in the Network tab, so that they are forced to load.

## Using Visual Studio Code?
If you edit your site contents using [Visual Studio Code](https://code.visualstudio.com) (and you should!), than instead of going through all the previous indications just install Warren Buckley's amazing [IIS Express extension](https://marketplace.visualstudio.com/items?itemName=warren-buckley.iis-express).

This extension will allow you to launch your MIIS site with IIS Express just by pressing `CTRL+F5`, and will even open your browser for you with the site already loaded. A must have!