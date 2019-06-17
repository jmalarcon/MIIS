---
Title: Sample custom Front Matter external source
#They are prefixed with !! and instantiate a class to retrieve the value for the field
Rnd: !!random_int 0 1000
Rnd2: !!random_int -1000 0
---

# This is a sample file that uses a custom value 

This is a random value: `{{rnd}}` - Should change in every execution

This is another random negative value: `{{rnd2}}` - Should change in every execution

We can use filters to change the rendered value, for example:

`{{rnd}} / 4` : `{{ rnd | divided_by: 4 }}`

Or use them in conditionals and other expressions (this fails, and I don't know why):

This doesn't work and I don't know why :confounded::

{% if rnd > 500  %}
    {{rnd}} is greater than 500
{% else %}
    {{rnd}} is less or equal than 500
{% endif %}