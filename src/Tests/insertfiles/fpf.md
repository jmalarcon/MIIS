---
title: Test for external files
description: shouldn't appear because excerpt takes precedence
excerpt: "This is a description. You could have used excerpt, description or summary in that order of precedence if the three are declared"
summary: Shouldn't appear because excerpt and description take precedence
data: a,b,c,d,e
extFile: ../raw.mdh
mycomponent: component01.md
#Can't inject this file (no extension). Is used to demonstrate processing fields in injectfile
extFilePath: ../raw
---
# This is a file that includes several externally inserted files

Current URL relative to the root: {{url}}
Current URL relative to the root (no extension): {{urlnoext}}

There are 5 ways to insert the contents of another file in a MIIS file.

## 1. Using file names in FM fields (File Processing Fields, FPF)

They are inserted when you create a field with the name or the path to a .md or .mdh file in your site. You can define it in the Front-Matter or globally in the `web.config` for your site or folder.

For example, the next content is inserted from the `/raw.mdh` file at the root folder (check its contents to see how it works):

```
{{extFile}}
```

This kind of external files will process placeholders inside their content just as if you were accesing them (so, in their own context), but returning just their processed content (without any template, obviously).

This is a very simple way to render other file's content, and it's very **efficient** because it will use the caching mechanism built in MIIS (if enabled). The `InsertFile` method doesn't take advantage of the cache when it's invoqued (the final page will be cached anyway).

### Render FPF as components, with extra "wrapping" HTML

You can render a file as a component, using an specific layout in your current template that will be processed to return any "wrapping" contents you need for the data in the file, including the main content. 

Take a look for example to the `component01.md` file in this folder and the corresponding `components/testcomponent.html` file in the current template, that acts as the "wrapping" HTML. We use the HTML of the layout of the component to inject any complex structure in the current file, based on the data and contents of a file, making easy, for example, to add special sections to the current document of template.

This is an inserted component with extra HTML usng it's data (see the `mycomponent` field in the front-matter of this file:

{{mycomponent}}

The key here is the **`IsComponent`** field used in the Front-Matter of a file. If specified it indicates that the current file is a component, not a full-fledged file. It must indicate a `Layout` property that points to the component HTML file in the current template to be used for processing the file content.

The `IsComponent` field can only be specified in the Front.Matter and won't be retrieved or inherited from `web.config` file if specified there (the `Layout` property can be global, for example in the `web.config` in a folder that affects to all the files in there, that are components).

Normally this kind of component would be unpublished files, since they are designed to be part of other files, so it's highly recommended to specifiy  `Published: false` in the FM for they to become invisible from the outside.

**This `IsComponent` field only works with file processing fields**. However, you can easily use the next methods to insert any HTML file contents using any other context, so it's very easy too, although FPF components is a faster way to do it depending on your needs.

## 2. Using the `InsertFile` tag

There's am special MIIS tag named `InsertFile` (case insensitive) that you can use to insert the contents of any other valid file in your site inside the current file.

It's similar to the typical `include` tag available in some static file generators such as Jekyll, but much more powerful, since it allows you to decide the context you want to use for processing the files before being inserted.

This tag syntax is:

```liquid
{%raw%}{%insertfile filepath [context]%}{%endraw%}
```

The `filepath` parameter (mandatory), its the relative path of the file you want to insert (**no spaces allowed!!**). Only the following types of files are allowed to be inserted with this tag:

- `.md`
- `.mdh`
- `.html`
- `.htm`
- `.txt`

>A really interesting thing you can do here is using any current file's field in that path to dynamically generate that path. See an example further below.

The `context` optional parameter is the context you want to use for rendering your file. It can have 4 values, and that allows for 4 different ways to insert the files:

### 2.1. Main page context (`this`, default value)

If you don't specify the context or if you use the word `this` (case insensitive), then current file's context is used to render the inserted contents. This means that is rendered in the same context as the parent file and therefore uses the same fields and values, including block specific values (such thos inside liquid tag loops, etc.).

For example, this is the same `/raw.mdh` file inserted with `insertfile`. It shows different values because it takes the `title` and `data` fields from the **current file** where it's being inserted, not the original values in it:

```
{% insertfile ../raw.mdh %}
```

### 2.2. Own file context

If you specify `own` (case insensitive) as the value for the optional context, then the file is processed in the original context of that inserted file, using its fields and not the ones in the current file.

Check this for example:

```
{% insertfile ../raw.mdh own %}
```

Notice that this is the same effect as using a FPF (see method #1) but the main difference is that, since you can use fields to form the file path, it could be used to dynamically generate several files from an array or any other data source if needed. A very powerfull possibility indeed.

>Obviously, if you use this option, the file you're inserting should contain some front-matter with data suitable to be used as context, or at least global dfault values for that data. If not, the liquid placeholders will be removed (no data for them).

### 2.3. No context at all

You can insert the file contents disabling any context with the value `none`  as the context parameter. That will insert the file contents deleting all the liquid placholders (if any) eccept those that have a global value assigned.

Check it here:

```
{% insertfile ../raw.mdh none %}
```

### 2.4 Use the context of a third file

This one is really powerful. If you want to insert the content of a file (using it as a template) and have it rendered using the context of any other third file, you just need to indicate the path to that third file as the context:

```
{% insertfile /raw.mdh /index.md %}
```

In this case we're injecting the same file contents but all the data for the liquid placeholders comes from the `index.md` file at the root of the site, therefore has different values.

>You can use any `.md`, `.mdh`, `.yml` or `.text` file for the context, but it should have a correctly formatted front-matter with the data you want to use. The only files that must not have front-matter with the `---` delimiters are the `.yml` files (in fact, this kind of files should not have it if we want them to work correctly).

You can use this to inject anywhere, data from any other file. For example, imagine that you want to use the information of your company's team at several places in your site. You can have a folder with a file for each employee with their data in the front-matter and even use those pages as individual landing pages for each one of them. But, with this variation of `inserfile` and the fact that you can use any field for your file path (inserted file path or context file path, see below) ypu can reuse that data anywhere using any author's file as context, and render it as you want using the inserted file contents. This is really powerful for a lot of applications as soon as you discover its ins and outs.

## Dynamic file paths with `insertfile`

As mentioned, you can even use Front-Matter (or global) fields as part of the file path to load, like this one:

```
{% insertfile {{extFilePath}}.mdh %}
```

>IMPORTANT: Please, notice that you couldn't have used `extFile` or `extFile2` as parameters in the previous tag. The reason is that, since parameters are rendered **before** reading the file, they will be rendered as the file content itself (.md and .mdh values in parameters are processed this way), so not a valid filename and therefore an invalid file type exception will be shown. Try one of them to check exactly what I'm talking about.

inserfile custom tags can detect and inform about custom references