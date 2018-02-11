# MIIS -  Markdown features
The Markdown to HTML conversion is provided by [Markdig](https://github.com/lunet-io/markdig). So it's not my merit but... Why reinvent the wheel if you have such a great library already available?.

It supports:
- All the core CommonMark Mardown features (600+ tests passed from CommonMark specs)
- Auto-links for `http://`, `https://`, `ftp://`, `mailto:` and `www.xxxx.yyy`.
- Emojis: :-) :thumbsup: (You can disable them with the [`UseEmoji` setting](Settings))
- Two kinds of tables: pipes and grid tables
- [Media url support](https://talk.commonmark.org/t/embedded-audio-and-video/441) for YouTube, Vimeo, mp3, mp4...
- Extra-emphasis
- [PHP Markdown Extra](https://michelf.ca/projects/php-markdown/extra/): attributes, auto-identifiers, footnotes, abbreviations...
- Task lists
- Extra bullet lists (a. b. c...., i., ii., iii....)
- [Custom containers](https://talk.commonmark.org/t/custom-container-for-block-and-inline/2051)
- [Figures](https://talk.commonmark.org/t/image-tag-should-expand-to-figure-when-used-with-title/265/5) for several images one after another
- [Smartypants](https://daringfireball.net/projects/smartypants/)
- [Mermaid diagrams](https://knsv.github.io/mermaid/#mermaid)
- [Math/Latex extension](https://talk.commonmark.org/t/ignore-latex-like-math-mode-or-parse-it/1926)

You can check all the supported Markdown features details in the [Markdig github page](https://github.com/lunet-io/markdig){target=_blank}.