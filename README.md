# 🔴 Work in Progress 🔴

Some things work, others don't. There is no release yet, for now you need to build&run it by yourself. 

install .NET SDK 6.x (preview) and run
```
dotnet run -- <parameters>
```


# Convert Journey.Cloud data to Markdown

This command line tool takes your export from [Journey.Cloud](https://journey.cloud/) and converts it to a folder of markdowns. It preserves all pictures and data, and let's you customize the output

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
  -i, --out-directory <out-directory>                      Specifies the output directory, if omitted it's next to the ZIP
                                                           file with the same name
  -f, --override-existing                                  Override existing files inside output directory [default: False]
  --md-github-flavoured                                    Markdown: enable GitHub flavoured [default: False]
  --md-list-bullet-char <md-list-bullet-char>              Markdown: List bullet character [default: -]
  --md-smart-href-handling                                 Markdown: Smart href Handling [default: True]
  --file-name-template <file-name-template>                File name for the created Markdown file [default:
                                                           {{String.Format date_journal "yyyy-MM-dd"}} {{String.Truncate
                                                           preview_text_md_oneline 100}}]
  --clean-from-filename-chars <clean-from-filename-chars>  Additional characters to clean from file name  [default: []#.]
  --version                                                Show version information
  -?, -h, --help                                           Show help and usage information

Commands:
  dump-template <outfile>  Dumps the default Handlebars template to the specified file
```
