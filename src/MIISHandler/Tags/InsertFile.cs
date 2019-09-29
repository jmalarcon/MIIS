using System;
using System.Linq;
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
        //Constants for choosing the right context to evaluate the insertion of the file
        private const string RENDER_CONTEXT_TYPE_THIS = "this";  //Default: use the parent file's context to insert the file
        private const string RENDER_CONTEXT_TYPE_OWN = "own";    //The inserted file's context. Equivalent to use a placeholder with a .md or .mdh name on it
        private const string RENDER_CONTEXT_TYPE_NONE = "none";  //No context to be used. Inserts the file contents "as is", without replacing any tags

        //The file extensions allowed to be inserted
        private readonly string[] _AllowedInsertedFileExts = { MarkdownFile.MARKDOWN_DEF_EXT, MarkdownFile.HTML_EXT, ".htm", ".html", ".txt" };
        //The file extensions allowed to be used as context (should contain simple YAML in the Front Matter or in the body in the case of .yml and .txt files)
        private readonly string[] _AllowedContextFileExts = { MarkdownFile.MARKDOWN_DEF_EXT, MarkdownFile.HTML_EXT, ".yml", ".txt" };

        //The file name to render (if any), set in the Initialize method
        private string _fileName = "";
        private string _renderContextType = RENDER_CONTEXT_TYPE_THIS;  //If we must use a third file content as a context, it'll keep the name of the file
        private RenderParameters _parentFileRenderParams = null;

        //Gets info from the tag in the file: tagname, parameters (markup) and the rest of the tokens below it
        public override void Initialize(string tagName, string markup, List<string> tokens)
        {
            base.Initialize(tagName, markup, tokens);
            //Get all the parameters into an array (lowercase)
            string[] paramValues = markup.Split(' ').Select(c => c.Trim().ToLowerInvariant()).Where(c => !string.IsNullOrEmpty(c)).ToArray<string>();

            //At least a parameter is mandatory: the file name of the file to be inserted
            if (paramValues.Length == 0)
            {
                throw new Exception("The filename is missing!");
            }
            else
            {
                //>>>>>>> File to be inserted
                //Check if the first parameter it's a valid file type to be inserted or not
                _fileName = paramValues[0];
                string _fileExt = Path.GetExtension(_fileName);
                //Check if it's an allowed file extension to be inserted
                if ( !_AllowedInsertedFileExts.Contains<string>(_fileExt) )
                {
                    throw new Exception($"Invalid file type '{_fileName}'. Allowed types are {string.Join(", ", _AllowedInsertedFileExts)}");
                }
                //>>>>>>> Context to be used for processing
                if (paramValues.Length > 1)
                    _renderContextType = paramValues[1];

                //>>>>>>> Check if it's a valid context
                //If it's a third file's context, check if it's a valid file
                if ( _renderContextType != RENDER_CONTEXT_TYPE_THIS &&
                    _renderContextType != RENDER_CONTEXT_TYPE_OWN &&
                    _renderContextType != RENDER_CONTEXT_TYPE_NONE &&
                    !_AllowedContextFileExts.Contains<string>(Path.GetExtension(_renderContextType)) )
                {
                    throw new Exception($"Invalid context for inserting the file '{_renderContextType}'. Check the documentation for valid values.");
                }

            }
        }

        //Renders the file
        public override void Render(Context context, TextWriter result)
        {
            //Current HTTPContext
            HttpContext ctx = HttpContext.Current;
            //Current MD or MDH file (needed for cache management and circular references detection)
            dynamic parentFile = context[MDFieldsResolver.INTERNAL_REFERENCE_TO_CURRENT_FILE]; //as MIISFile;

            //Process the current parameter to allow the substitution of possible values, to be able to use them as parameters for this inserFile tag
            _parentFileRenderParams = RenderParameters.FromContext(context, result.FormatProvider);
            Template paramsTemplate = Template.Parse(_fileName);
            _fileName = paramsTemplate.Render(_parentFileRenderParams);

            string subRenderedContent = "";
            MarkdownFile insertedFile = null;
            
            //Circular references detector
            CircularReferencesDetector crd = new CircularReferencesDetector();

            //Read and process the inserted file contents if possible
            //Don't need to check if it's a valid file, because this has already be done in the Initialize method
            try
            {
                string fp2File = ctx.Server.MapPath(_fileName);    //Full path to the inserted file
                    
                //Checks if current file has been referenced before or not
                object crdObj = context[CRD_CONTEXT_VAR_NAME];    //Try to get a CR Detector from context
                if (crdObj == null || crdObj.GetType() != typeof(CircularReferencesDetector))
                {
                    //If there's no detector (first insertfile) then add one to the context
                    crd.CheckCircularReference(parentFile.FilePath);    //Add current initial file as a reference
                    context[CRD_CONTEXT_VAR_NAME] = crd;
                }
                else
                {
                    crd = (CircularReferencesDetector) crdObj;
                }

                if (crd.CheckCircularReference(fp2File) == false)
                {
                    insertedFile = new MarkdownFile(fp2File, false);
                    //Use the raw content of the file. Later we'll further process it
                    //Liquid fields processing must always be done in the raw format to prevent problems with 
                    //unexpected HTML inserted by HTML conversion and not present in the original file, when the liquid tags were written
                    subRenderedContent = insertedFile.RawContent;
                    //Add the processed file to the dependencies of the currently processed content file, so that the file is invalidated when the FPF changes (if caching is enabled)
                    parentFile.AddFileDependency(fp2File);
                }
                else
                {
                    throw new Exception( string.Format("Circular reference!!:<br>{0}", crd.GetNestedFilesPath()) );
                }
            }
            catch (System.Security.SecurityException)
            {
                subRenderedContent = $"Can't access file \"{_fileName}\"";
            }
            catch (System.IO.FileNotFoundException)
            {
                subRenderedContent = $"File not found: \"{_fileName}\"";
            }
            catch (Exception ex)
            {
                //This should only happen while testing, never in production, so I send the exception's message
                subRenderedContent = $"Error loading \"{_fileName}\": {ex.Message}";
            }

            //If the raw contents of the file were read
            if (insertedFile != null)
            {
                Template partial = Template.Parse(subRenderedContent);
                //Process the file using the appropiate context
                switch (_renderContextType)
                {
                    case RENDER_CONTEXT_TYPE_THIS:
                        //Render the raw subfile content *in the same context as the parent file*
                        subRenderedContent = partial.Render(_parentFileRenderParams);
                        break;
                    case RENDER_CONTEXT_TYPE_OWN:
                        //Render the file in its own context
                        subRenderedContent = insertedFile.ProcessedContent;
                        break;
                    case RENDER_CONTEXT_TYPE_NONE:
                        //Use an empty context (placeholders will be deleted)
                        subRenderedContent = partial.Render(new Hash());
                        break;
                    default:    //It's a thrid party file's context
                        //In this case we need to use the MIISFile that represents the third file, as the context
                        MDFieldsResolver renderContext = new MarkdownFile(ctx.Server.MapPath(_renderContextType), false).FieldsResolver;
                        subRenderedContent = partial.Render(renderContext);
                        break;
                }

                //Further process the results into HTML if the injected file its a Markdown file
                if (insertedFile.FileExt == MarkdownFile.MARKDOWN_DEF_EXT)
                    subRenderedContent = Renderer.ConvertMarkdown2Html(subRenderedContent, insertedFile.UseEmoji, insertedFile.EnabledMDExtensions);

                //Sice we're inserting HTML we need to add the delimiters preventing the processing of the HTML 
                //when the final file is transformed into HTML from Markdown
                subRenderedContent = MDFieldsResolver.HTML_NO_PROCESSING_DELIMITER_BEGIN + subRenderedContent + MDFieldsResolver.HTML_NO_PROCESSING_DELIMITER_END;
            }

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