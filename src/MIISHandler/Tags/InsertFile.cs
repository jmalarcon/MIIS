using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using DotLiquid;

namespace MIISHandler.Tags
{
    /// <summary>
    /// This defines a new renderfile Tag for liquid syntax to include any .MD or .MDH file inside other
    /// rendering the fields in the same context as the parent file
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
            //Just make a note of the final parameter (presumably the name of the file to render)
            fileName = markup.Trim();
        }

        //Renders the file
        public override void Render(Context context, TextWriter result)
        {
            //Current HTTPContext
            HttpContext ctx = HttpContext.Current;
            //Current MD or MDF file
            dynamic currentMDF = context[MDFieldsResolver.INTERNAL_REFERENCE_TO_CURRENT_FILE]; //as MIISFile;

            //Process the current parameter to allow the substitution of possible values, to be able to use them as parameters for this inserFile tag
            Template paramsTemplate = Template.Parse(fileName);
            fileName = paramsTemplate.Render(RenderParameters.FromContext(context, result.FormatProvider));

            string subRenderedContent;
            CircularReferencesDetector crd = new CircularReferencesDetector(); ; //Circular references detector

            MarkdownFile mdFld = null;

            //Read the file contents if possible
            //Only .md, .mdh files allowed
            if (fileName.ToLowerInvariant().EndsWith(MarkdownFile.MARKDOWN_DEF_EXT) || fileName.ToLowerInvariant().EndsWith(MarkdownFile.HTML_EXT))
            {
                try
                {
                    string fp2File = ctx.Server.MapPath(fileName);    //Full path to the inserted file
                    
                    //Checks if current file has been referenced before or not
                    object crdObj = context[CRD_CONTEXT_VAR_NAME];    //Try to get a CR Detector from context
                    if (crdObj.GetType() == typeof(string) && crdObj.ToString() == "")
                    {
                        //If there's no detector (first insertfile) then add one to the context
                        crd.CheckCircularReference(currentMDF.FilePath);    //Add current initial file as a reference
                        context[CRD_CONTEXT_VAR_NAME] = crd;
                    }
                    else
                    {
                        crd = (CircularReferencesDetector) crdObj;
                    }

                    if (crd.CheckCircularReference(fp2File) == false)
                    {
                        mdFld = new MarkdownFile(fp2File);
                        subRenderedContent = mdFld.RawContent;  //Use the raw content. Later we'll further process it
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

            //Render the raw subfile content *in the same context as the parent file*
            //Liquid fields processing must always be done in the raw format to prevent problems with 
            //unexpected HTML inserted by HTML conversion and not present in the original file, when the liquid tags were written
            Template partial = Template.Parse(subRenderedContent);
            subRenderedContent = partial.Render(RenderParameters.FromContext(context, result.FormatProvider));
            //Further process it into HTML if its a Markdown file
            if (mdFld?.FileExt == MarkdownFile.MARKDOWN_DEF_EXT)
                subRenderedContent = Renderer.ConvertMarkdown2Html(subRenderedContent, mdFld.UseEmoji, mdFld.EnabledMDExtensions);
            result.Write(subRenderedContent);
            crd.Reset();
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

        //Resets the Circular Reference Detector once the tag has been processed (to allow to insert the same file more than once in the same content, not in nested contents)
        internal void Reset()
        {
            nestedFiles.Clear();
        }
    }
}