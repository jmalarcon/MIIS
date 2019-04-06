using System;
using System.IO;
using System.Security;
using System.Web;
using IISHelpers;

namespace MIISHandler
{
    public class MIISHandler : IHttpHandler
    {
        /// <summary>
        /// This handler will take care of all requests to Markdown file requests
        /// to process the Markdown and return HTML.
        /// It also supports an especial file extension for HTML content (.mdh) to create complex layouts in specific pages
        /// </summary>

        #region IHttpHandler Members

        public bool IsReusable
        {
            get {
                return false;
            }
        }

        //Process the requests
        public void ProcessRequest(HttpContext ctx)
        {
            try
            {
                //Try to process the markdown file
                string filePath = ctx.Server.MapPath(ctx.Request.FilePath);
                MarkdownFile mdFile = new MarkdownFile(filePath);

                //Check if the File is published
                if (mdFile.IsPublished)
                {
                    //Check if is a special status code page (404, etc)
                    if (mdFile.HttpStatusCode != 200)
                        ctx.Response.StatusCode = mdFile.HttpStatusCode;

                    //Send the rendered content for the file
                    ctx.Response.ContentType = mdFile.MimeType; //text/html by default
                    ctx.Response.Write(mdFile.HTML);
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch (SecurityException)
            {
                //Access to file not allowed
                ctx.Response.StatusDescription = "Forbidden";
                ctx.Response.StatusCode = 403;
            }
            catch (FileNotFoundException)
            {
                //Normally IIS will take care, but you can disconnect it
                ctx.Response.StatusDescription = "File not found";
                ctx.Response.StatusCode = 404;
            }
            catch (Exception)
            {
                throw;
            }
            
        }

        #endregion
    }
}
