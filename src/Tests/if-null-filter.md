---
Title: 'if_null' liquid filter usage
#image: /images/myImage.png
#og-image: /images/myOGImage.png
bg-image: /images/myBGImage.png
---

# 'if_null' filter usage

MIIS adds in 3.2.0 a custom filter called `if_null`. This filter allows you to save if-else-end Liquida tags and make your templates much more concise and readable.

For example, imagine that you have 3 possible images that you can use in a page:

- `image`: the normal if_null field used for blog posts or any other file.
- `bg-image`: a background image that your template design optionally can use as an specific image for the header's background.
- `og-image`: a specific image to be used for social sharing in the Open Graph headers for the page

In the social headers code you want to make the precedence of this images this way: 

`og-image > image > bg-image`

So, if `og-image` is not available, use `image` and if `image` is not available either, then use `bg-image`.

In normal Liquid syntax you would probably do something like this:

```liquid
{%- raw -%}
{%- if og-image -%}

{%- capture imgurl -%}{{ og-image | absolute_url }}{%- endcapture -%}

{%-elsif image -%}

{%- capture imgurl -%}{{ image | absolute_url }}{%- endcapture -%}

{%-else bg-image -%}

{%- capture imgurl -%}{{ bg-image | absolute_url }}{%- endcapture -%}

{%- endif -%}

<meta property="og:image" content="{{ imgurl }}" />
{%- endraw -%}
```

>Note: Thanks to MIIS' `absolute_url` filter you save some "ifs" inside the capture taking into account if it's an absolute, relative or relative to the root URL.

Well, with the `if_null` filter, you can now simple write:

```liquid
{%- raw - %}
<meta property="og:image" content="{{ og-image | if_null: image | if_null: bg-image | absolute_url }}" />
{%- endraw -%}
```

and you're done! Much clearer and succint.

You can check it in use in the code for this page. Note that this page has only defined the `bg-image` parameter, therefore in the next paragraph you should see the URL for that image:

`{{ og-image | if_null: image | if_null: bg-image | absolute_url }}`

You can play with it uncommenting the other images in the Front-Matter and see how it behaves.
