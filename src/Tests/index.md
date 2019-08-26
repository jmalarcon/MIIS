---
Title: "MIIS CMS Default page"
Author: Jose M. Alarcón
arr: [ Element 1, Element 2, Element 3]
fldBool: true
CustomDate: 2019-08-23 22:50:53
# Can have used nil instead of null n the next field
nullval: null
InventedField: "This is an invented field!"
#Caching: true
---

# MIIS - A Markdown File-based CMS for IIS and Azure Web Apps
#### by [{{author}}](https://twitter.com/jm_alarcon)

**It's Working!**

{{description}}

Current URL relative to the root: {{url}}
Current URL relative to the root (no extension): {{urlnoext}}

This is a custom field that is a date: {{CustomDate}}

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