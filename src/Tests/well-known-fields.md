---
Title: Well-Known Fields
Description: This file shows all the well-known fields and tests different spaces and case combinations for field delimiters:
---

## {{title}}

{{description}}

- Title of the page: `{{title}}`
- File name: `{{ FileName}}`
- File name without extension: `{{   FileNameNoExt   }}`
- File extension: `{{ fileext }}`
- Current folder: `{{dir}}`
- Date created: `{{datecreated }}` <small>_(you can format dates using the liquid `date` filter)_</small>
- Date modified: `{{    DATEmodified    }}`
- Date: `{{date}}`
- Is current user authenticated?: `{{ isAuthenticated }}`
- Authentication Type: `{{authTYPE | default: "Non authenticated"}}`
- User name: `{{username | default: "Anonymous" }}`
- Domain: `{{DOMAIN}}`
- Base URL: `{{baseurl}}`
- User IP: `{{ClientIP}}`
- Current date and time: `{{NOW}}`
- Current time: `{{time}}`
- Current URL: `{{url}}`
- Current URL without file extension: `{{UrlNoExt}}`
- Current Template in use: `{{TemplateName}}`
- Current layout in use: `{{layout}}`