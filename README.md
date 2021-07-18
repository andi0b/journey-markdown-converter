# 🔴 Work in Progress 🔴

Some things work, others don't. There is no release yet, for now you need to build&run it by yourself. 

install .NET SDK 6.x (preview) and run
```
git clone https://github.com/andi0b/journey-markdown-converter.git
cd journey-markdown-converter/src
dotnet run -- <parameters>
```


# Convert Journey.Cloud data to Markdown

This command line tool takes your export from [Journey.Cloud](https://journey.cloud/) and converts it to a folder of markdowns. It preserves all pictures and data, and lets you customize the output.

[![.NET](https://github.com/andi0b/journey-markdown-converter/actions/workflows/dotnet.yml/badge.svg)](https://github.com/andi0b/journey-markdown-converter/actions/workflows/dotnet.yml)

# How to run
Download a binary for one of the supported platforms:
- Windows x86
- Linux x64
- macOS x64
- macOS ARM

and run it:
```
journey-markdown-converter
  Converts a Journey JSON ZIP Export file into Markdown

Usage:
  journey-markdown-converter [options] <infile> [command]

Arguments:
  <infile>  Journey JSON Export ZIP File

Options:
  -o, --out-directory <out-directory>                          Specifies the output directory, if omitted it's next to the ZIP file with the
                                                               same name
  -f, --override-existing                                      Override existing files inside output directory [default: False]
  -v, --verbose                                                Show more information and more detailed error messages [default: False]
  -n, --file-name-template <file-name-template>                File name for the created Markdown file [default: {{String.Format date_journal
                                                               "yyyy-MM-dd"}} {{String.Truncate preview_text_md_oneline 100}}]
  -b, --body-template <body-template>                          Specify a Handlebars template file for the output file. If none specified, the
                                                               default template is used.
  -d, --additional-tags <additional-tags>                      Additional tags that should be added to all files
  -p, --tag-prefix <tag-prefix>                                Prefix that will be prepended to all tags (but not the additional tags)
  -c, --clean-from-filename-chars <clean-from-filename-chars>  Additional characters to clean from file name, forbidden characters are always
                                                               cleaned [default: []#.,]
  --md-github-flavoured                                        Markdown: enable GitHub flavoured [default: False]
  --md-list-bullet-char <md-list-bullet-char>                  Markdown: set a different bullet character for un-ordered lists [default: -]
  --md-smart-href-handling                                     Markdown: Smart href Handling [default: False]
  --md-pass-through-tags <md-pass-through-tags>                Markdown: pass a list of tags to pass through as is without any processing
                                                               [default: ]
  --md-unknown-tags <Bypass|Drop|PassThrough|Raise>            Markdown: pass a list of tags to pass through as is without any processing
                                                               [default: PassThrough]
  --md-table-without-header-row-handling <Default|EmptyRow>    Markdown: Table: By default, first row will be used as header row [default:
                                                               Default]
  --md-whitelist-uri-schemes <md-whitelist-uri-schemes>        Markdown: Specify which schemes (without trailing colon) are to be allowed fora
                                                               and img; tags. Others will be bypassed. By default allows everything. [default:
                                                               ]
  --version                                                    Show version information
  -?, -h, --help                                               Show help and usage information

Commands:
  dump-template <outfile>  Dumps the default Handlebars template to the specified file [default: ]
```

# Handlebars Templates

The Markdown output and the file name can be completely customized via Handlebars templates.

- see language guide: https://handlebarsjs.com/guide/
- and all enabled helpers: https://github.com/Handlebars-Net/Handlebars.Net.Helpers/#using

## Default Template

Modify the default template to get custom MarkDown files

```handlebars
{{!
This is a handlebars template for formatting the output

It is using HandleBars.NET with all it's base extensions, see this pages for documentation
- https://github.com/Handlebars-Net/Handlebars.Net.Helpers/#using
}}
---
date_journal: {{String.Format date_journal "o"}}
modified: {{String.Format date_modified "o"}}
tags:
{{#each tags}}
- {{../tagPrefix}}{{this}}
{{/each}}
{{#each additionalTags}}
- {{this}}
{{/each}}
location:
    lat: {{lat}}
    lon: {{lon}}
    address: {{address}}
weather:
    temperature_c: {{weather.degree_c}}
    description: {{weather.description}}
---
{{! everything else is the content of the file}}
{{text_md}}

{{#if photos.length}}
# Photos
{{/if}}
{{#each photos}}
![photo]({{EscapeUri this}})
{{/each}}
```

## Availible Fields

TBD
