using System.Collections.Generic;
using System.IO;
using DotLiquid;

namespace MIISHandler.Tags
{
    /// <summary>
    /// Sample custom tag to embed a responsive YouTube video with the noocookies viewer, no border, controls, no info, no search, no related videos, no branding and allowing full screen play
    /// Usage: {% youtube XXXXX %} being XXXX the id of the YouTube video to render
    /// https://developers.google.com/youtube/player_parameters
    /// </summary>
    class YouTube : DotLiquid.Tag
    {
        private const string YouTubeEmbedTemplate =
@"<div style=""position:relative;width:100%;height:0;padding-bottom:56.25%;"">
    <iframe style=""position:absolute;left:0;top:0;width:100%;height:100%;"" src=""https://www.youtube-nocookie.com/embed/{0}?wmode=transparent&showinfo=0&showsearch=0&rel=0&modestbranding=1&theme=lightwatch"" frameborder=""0"" allow=""accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture"" allowfullscreen>
    </iframe>
</div>";
        private string YouTubeID;
        public override void Initialize(string tagName, string markup, List<string> tokens)
        {
            base.Initialize(tagName, markup, tokens);
            //The ID of the video to render
            YouTubeID = markup.Trim();
        }

        public override void Render(Context context, TextWriter result)
        {
            result.Write(string.Format(YouTubeEmbedTemplate, YouTubeID));
        }
    }
}
