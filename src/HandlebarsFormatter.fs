module journey_markdown_converter.HandlebarsFormatter

open System
open System.Collections.Generic
open System.CommandLine.Parsing
open System.Globalization
open System.IO
open HandlebarsDotNet
open HandlebarsDotNet.Helpers
open Newtonsoft.Json.Linq
open journey_markdown_converter
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
"""

let createHandlebars =
    let hb =
        Handlebars.Create(
            Handlebars.Configuration.Configure
                (fun c ->
                    c.NoEscape <- true
                    c.FormatProvider <- CultureInfo.InvariantCulture)
        )




    hb.RegisterHelper(
        "EscapeUri",
        HandlebarsHelper(
            fun writer context parameters ->
                let index = 0
                let uri = parameters.At<string>(&index)
                let escaped = Uri.EscapeDataString(uri)
                writer.WriteSafeString(escaped)
        )
    )


    HandlebarsHelpers.Register(hb)
    hb

let getPropertiesFromJourneyEntry (additionalProperties: (string * obj) seq) journeyEntry =

    let untypedProps =
        journeyEntry.untyped.Properties()
        |> Seq.map (fun prop -> (prop.Name, prop.Value.ToObject<Object>()))

    let typedProps =
        typeof<JourneyEntry>.GetProperties ()
        |> Seq.map (fun x -> (x.Name, x.GetValue(journeyEntry)))

    let merged = Dictionary<string, Object>()

    Seq.concat [ untypedProps
                 typedProps
                 additionalProperties ]
    |> Seq.iter (fun (name, value) -> merged.[name] <- value)

    merged

let hbCompileSafe (hb: IHandlebars) (template: string) name =
    try
        let compiled = hb.Compile(template)
        (fun (dict: Dictionary<string, obj>) -> compiled.Invoke(dict))
    with
    | e ->
        raise (
            ExitCodeException(
                $"Error parsing Handlebars template for {name}:{Environment.NewLine}\
                      {Environment.NewLine}\
                      {e.Message}",
                e
            )
        )

let createBodyFormatter hb extractProperties template =
    let compiledTemplate =
        hbCompileSafe hb template "body template"

    extractProperties >> compiledTemplate

let cleanFileName additionalChars str =
    Path.GetInvalidFileNameChars()
    |> Seq.append additionalChars
    |> Seq.fold (fun (s: string) c -> s.Replace(c, ' ')) str
    |> (fun s -> s.Trim())

let createFileNameFormatter hb extractProperties cleanFromFileNameChars template =
    let compiledTemplate =
        hbCompileSafe hb template "body template"

    extractProperties
    >> compiledTemplate
    >> cleanFileName cleanFromFileNameChars

let createFromOptions options =
    let hb = createHandlebars

    let extractProperties =
        getPropertiesFromJourneyEntry [ ("tagPrefix", options.TagPrefix :> obj)
                                        ("additionalTags", options.AdditionalTags :> obj) ]

    let bodyTemplate =
        if (String.IsNullOrWhiteSpace options.BodyTemplate) then
            defaultTemplate
        else
            File.ReadAllText options.BodyTemplate
     
    {| bodyFormatter = createBodyFormatter hb extractProperties bodyTemplate
       fileNameFormatter =
           createFileNameFormatter hb extractProperties options.CleanFromFileNameChars options.FileNameTemplate |}
