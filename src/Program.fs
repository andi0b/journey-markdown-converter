open System.IO
open journey_markdown_converter

let dumpTemplate outfile =
      
    if (File.Exists outfile) then
        failwith "outfile already exists"
        
    if (System.String.IsNullOrWhiteSpace(outfile)) then
        System.Console.WriteLine(HandlebarsFormatter.defaultTemplate)
    else
        System.Console.WriteLine($"Writing template to: {outfile}")
        File.WriteAllText(outfile, HandlebarsFormatter.defaultTemplate)
        
    0

[<EntryPoint>]
let main argv =
    CommandLine.invoke Converter.convertEntriesFromOptions dumpTemplate argv