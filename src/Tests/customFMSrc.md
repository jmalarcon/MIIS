﻿---
Title: Sample custom Front Matter external source
#They are prefixed with !! and instantiate a class to retrieve the value for the field
Rnd: !!random_int 0 1000
Rnd2: !!random_int -1000 0
num: 3
---

# This is a sample file that uses custom values

This are query params for this file (add some with `?param1=1&s=Hi`). If there's no other parameter with the same name, it can take it out from the querystring (GET) or the data received from a form (POST):

Param1: {{param1}}

s: {{s}}

And you can also retrieve by name  cookies, or even [server variables](https://docs.microsoft.com/en-us/previous-versions/iis/6.0-sdk/ms524602(v=vs.90)){target=_blank} too!:

`HTTP_USER_AGENT`: {{ HTTP_USER_AGENT }}

>Please, take into account that if you use any query string, form fields, cookie data or server variables in your content, **you can't cache the rendered file**, that will be generated again on each new request. This is automatic and can't be disabled unless you're using a declared query parameter from a Front Matter source (check documentation on creating custom Front-Matter sources).

----

This is a random value: `{{rnd}}` - Should change in every execution if cache is disabled

This is another random negative value: `{{rnd2}}` - Should change in every execution

We can use filters to change the rendered value, for example:

`{{rnd}} / 4` : `{{ rnd | divided_by: 4 }}`

Or use them in conditionals and other expressions:

{% if rnd > 500  %}
{{rnd}} is greater than 500
{% else %}
{{rnd}} is less or equal than 500
{% endif %}

{% if num > 5  %}
{{num}} is greater than 5
{% else %}
{{num}} is less or equal than 5
{% endif %}