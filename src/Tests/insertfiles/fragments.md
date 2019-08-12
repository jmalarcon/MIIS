---
Title: File fragments in layouts
layout: main-with-fragment.html
InventedField: This is an invented field content
---

# {{Title}}

This file is using a different layout, created for testing purposes in the `ReadTheDocs` template and called `main-with-fragment.html`. It substitutes the {%raw%}`{{toc}}`{%endraw%} custom field with a "fragment" indicated in the layout with a {%raw%}`{{*-toc}}`{%endraw%} placeholder. This makes MIIS search for a file called as this file but with a `-toc` suffix, that is: `fragments-toc.md` or `fragments-toc.mdh`. In this example, I've chosen the former but it will try both of them giving priority to the one that has the same extension as the current file.

The side, therefore, should be showing the contents of the `fragments-toc.md` file instead of the default side menu.

Any placeholders in the fragment will be evaluated as if they were included in the original file. For example the {%raw%}`{{ InventedField }}`{%endraw%} placeholder in the Front-Matter of this file, that is shown n the side now.

Check the contents of the `main-with-fragment.html` layout file in the `Templates\readthedocs` folder and of this file and `fragments-toc.md` to see how it works.