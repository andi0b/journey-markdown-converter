open System.CommandLine
open journey_markdown_converter
open journey_markdown_converter.CommandLine

let mainTyped options =
    System.Console.WriteLine($"Infile: {options.InFile}")
    System.Console.WriteLine($"OutDir: {options.OutDirectory}")
    System.Console.WriteLine($"OverrideExisting: {options.OverrideExisting}")
    0

let dumpTemplate (file:string) =
    System.Console.WriteLine($"Write template to file: {file}")
    0

[<EntryPoint>]
let main argv =
    let rootCommand = rootCommand mainTyped dumpTemplate
    CommandExtensions.Invoke(rootCommand, argv)
