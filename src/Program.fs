open System.IO
open journey_markdown_converter

let dumpTemplate (file: string) =
    //TODO implement!
    System.Console.WriteLine($"not implemented yet: Writing template to file: {file}")
    0

[<EntryPoint>]
let main argv =
    CommandLine.invoke Converter.convertEntriesFromOptions dumpTemplate argv