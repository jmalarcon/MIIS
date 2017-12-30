using System;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;

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

                //If the feature is enabled and the user requests the original file, send the original file
                if (!string.IsNullOrEmpty(ctx.Request.QueryString["download"]))
                {
                    if (Helper.GetParamValue("allowDownloading") == "1")
                    {
                        ctx.Response.ContentType = "text/markdown; charset=UTF-8";
                        ctx.Response.AppendHeader("content-disposition", "attachment; filename=" + mdFile.FileName);
                        ctx.Response.Write(mdFile.Content);
                    }
                    else
                    {
                        throw new SecurityException("Download of markdown not allowed. Change configuration.");
                    }
                }
                else
                {
                    //Render the file
                    HTMLRenderer renderer = new HTMLRenderer(ctx);
                    string html = renderer.RenderMarkdown(mdFile);

                    //Render the final HTML
                    ctx.Response.ContentType = "text/html";
                    ctx.Response.Write(html);
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
