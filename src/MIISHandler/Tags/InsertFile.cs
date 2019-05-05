using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using DotLiquid;
using IISHelpers;

namespace MIISHandler.Tags
{
    /// <summary>
    /// This defines a new renderfile Tag for liquid syntax to include any .MD or .MDH file inside other
    /// rendering the fileds in the same context as the mother file
    /// </summary>
    public class InsertFile  : DotLiquid.Tag
    {
        //Name of the context variable that gives access to the Circular Refefences Detector
        private const string CRD_CONTEXT_VAR_NAME = "_crd";

        //The file name to render (if any), set in the Initialize method
        private string fileName = "";

        //Gets info from the tag in the file: tagname, parameters (markup) and the rest of the tokens below it
        public override void Initialize(string tagName, string markup, List<string> tokens)
        {
            base.Initialize(tagName, markup, tokens);
            //Just make a not of the current parameter (presumably the name of the file to render)
            fileName = markup.Trim();
        }

        //Renders the file
        public override void Render(Context context, TextWriter result)
        {
            //Current HTTPContext
            HttpContext ctx = HttpContext.Current;
            //Current MD or MDF file
            MIISFile currentMDF = context[MDFieldsResolver.INTERNAL_REFERENCE_TO_CURRENT_FILE] as MIISFile;

            string subRenderedContent = "";

            //Read the file contents if possible
            //Only .md, .mdh files allowed
            if (fileName.ToLower().EndsWith(MarkdownFile.MARKDOWN_DEF_EXT) || fileName.ToLower().EndsWith(MarkdownFile.HTML_EXT))
            {
                try
                {
                    string fp2File = ctx.Server.MapPath(fileName);    //Full path to the inserted file
                    
                    //Checks if current file has been referenced before or not
                    CircularReferencesDetector crd;
                    object crdObj = context[CRD_CONTEXT_VAR_NAME];    //Try to get a CR Detector from context
                    if (crdObj.GetType() == typeof(string) && crdObj.ToString() == "")
                    {
                        //If there's no detector (first inserfile) then add one to the context
                        crd = new CircularReferencesDetector();
                        crd.CheckCircularReference(currentMDF.FilePath);    //Add current initial file as a reference
                        context[CRD_CONTEXT_VAR_NAME] = crd;
                    }
                    else
                    {
                        crd = (CircularReferencesDetector) crdObj;
                    }

                    if (crd.CheckCircularReference(fp2File) == false)
                    {
                        MarkdownFile mdFld = new MarkdownFile(fp2File);
                        subRenderedContent = mdFld.RawHTML; //Use the raw HTML, not the processed HTML (this last one includes the template too)
                        //Add the processed file to the dependencies of the currently processed content file, so that the file is invalidated when the FPF changes (if caching is enabled)
                        currentMDF.AddFileDependency(fp2File);
                    }
                    else
                    {
                        throw new Exception( string.Format("Circular reference!!:<br>{0}", crd.GetNestedFilesPath()) );
                    }

                }
                catch (System.Security.SecurityException)
                {
                    subRenderedContent = string.Format("Can't access file \"{0}\"", fileName);
                }
                catch (System.IO.FileNotFoundException)
                {
                    subRenderedContent = string.Format("File not found for \"{0}\"", fileName);
                }
                catch (Exception ex)
                {
                    //This should only happen while testing, never in production, so I send the exception's message
                    subRenderedContent = string.Format("Error loading \"{0}\": {1}", fileName, ex.Message);
                }
            }
            else
            {
                subRenderedContent = string.Format("Forbidden file type: \"{0}\"", fileName);
            }

            //Render the subfile contents in the same context as the parent
            Template partial = Template.Parse(subRenderedContent);
            partial.Render(result, RenderParameters.FromContext(context, result.FormatProvider));
        }
    }

    //Class used to make a note of the files that are rendered from a single root file
    //to prevent circular references
    public class CircularReferencesDetector : Drop
    {
        //To be used to control circular references in inserted files (with the inssertfile tag)
        private readonly List<string> nestedFiles = new List<string>();
        internal bool CheckCircularReference(string file)
        {
            if (nestedFiles.IndexOf(file) < 0)
            {
                nestedFiles.Add(file);
                return false;
            }
            else
                return true;
        }

        //Returns a string with all the nested files (without paths)
        internal string GetNestedFilesPath()
        {
                return string.Join(" -> ", nestedFiles.ToArray());
        }
    }
}