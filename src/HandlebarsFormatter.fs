module journey_markdown_converter.HandlebarsFormatter

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open HandlebarsDotNet
open HandlebarsDotNet
open HandlebarsDotNet.Helpers
open HandlebarsDotNet.Helpers.Enums
open Microsoft.FSharp.Reflection
open Newtonsoft.Json.Linq
open journey_markdown_converter.JourneyEntryParser

let defaultTemplate =
    """{{!
This is a handlebars template for formatting the output

It is using HandleBars.NET with all it's base extensions, see this pages for documentation
- https://github.com/Handlebars-Net/Handlebars.Net.Helpers/#using
}}
---
date_journal: {{String.Format date_journal "o"}}
modified: {{String.Format date_modified "o"}}
tags:
{{#each tags}}
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

# Photos
{{#each photos}}
![photo]({{this}})
{{/each}}
"""



let format (template: HandlebarsTemplate<obj, obj>) (typed: JourneyEntry, untyped: JObject) =

    let merged = Dictionary<string, Object>()

    untyped.Properties()
    |> Seq.iter (fun prop -> merged.[prop.Name] <- prop.Value.ToObject<Object>())

    typeof<JourneyEntry>.GetProperties ()
    |> Seq.map (fun x -> (x.Name, x.GetValue(typed)))
    |> Seq.iter (fun (name, value) -> merged.[name] <- value)

    template.Invoke(merged)

let createFromOptions options =
    let hb =
        Handlebars.Create(
            Handlebars.Configuration.Configure
                (fun c ->
                    c.NoEscape <- true
                    c.FormatProvider <- CultureInfo.InvariantCulture)
        )

    HandlebarsHelpers.Register(hb)

    let cleanFileName additionalChars str =
        Path.GetInvalidFileNameChars()
        |> Seq.append additionalChars
        |> Seq.fold (fun (s: string) c -> s.Replace(c, ' ')) str
        |> (fun s -> s.Trim())

    let hbCompileSafe (template: string) name =
        try
            hb.Compile(template)
        with
        | e ->
            raise (
                CommandLine.Exception(
                    $"Error parsing Handlebars template for {name}:{Environment.NewLine}\
                      {Environment.NewLine}\
                      {e.Message}",
                    e
                )
            )

    {| bodyFormatter = format (hbCompileSafe defaultTemplate "body template")
       fileNameFormatter =
           format (hbCompileSafe options.FileNameTemplate "file name")
           >> cleanFileName options.CleanFromFileNameChars |}
