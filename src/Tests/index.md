﻿---
Title: "MIIS CMS Default page"
Author: !!DataFromFile ~/_data/authors/jmalarcon.md
arr: [ Element 1, Element 2, Element 3]
fldBool: true
CustomDate: 2019-08-23 22:50:53
# Can have used nil instead of null n the next field
nullval: null
InventedField: "This is an invented field!"
data: one, two, three, four, 5
#Caching: true
---

# MIIS - A Markdown File-based CMS for IIS and Azure Web Apps
#### by [{{author.name}}](https://twitter.com/{{author.twitter}})

This bio is taken from a content file:

>{{author.bio}}

**It's Working!**

{{description}}

Current URL relative to the root: {{url}}
Current URL relative to the root (no extension): {{urlnoext}}

This is a custom field that is a date: {{CustomDate}}

These are some images using the `relative_url` filter (you should test this in lower level folders too), and you can use the `absolute_url` filter with them too, to get the full qualified URL for the resource automatically (with protocol and domain):

Absolute URL:

![This is an image]({{ "https://images.unsplash.com/photo-1586763209537-4d1fb62ef601?ixlib=rb-1.2.1&ixid=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=crop&w=1267&q=80" | relative_url }})

Relative to the root URL:

![This is an image]({{ "~/images/logo.png" | relative_url }})

Relative to this page URL (in lower levels you can use upwards paths such as `../../whatever/image.png` and the like):

![This is an image]({{ "images/logo.png" | relative_url }})

This is the Front-Matter array:
{%- for elt in arr -%}
- {{elt}}
{%- endfor -%}

The boolean field in the FM is:&nbsp;`{%- if fldbool == true -%}true{%- else -%}false{%- endif %}`

{% if nullval %}This is never shown because nullval is null{% endif %}

This is a sample field: **{{ inventedfield }}**

This is [a link to the root folder](~/)

:::{.Block}
This file was created on {{datecreated}} and is inside a div with a class name
:::

Add a video from YouTube using an image in MarkDown:

![You've been rickrolled!!](https://www.youtube.com/watch?v=dQw4w9WgXcQ)

Let's ::render:: a list:
- One
- Two
- Three
- Four

A cite{.Wisdom}:

This is a ""citation of someone""

And a quote:

>Important: this is a relevant text for you...

Bye, bye...