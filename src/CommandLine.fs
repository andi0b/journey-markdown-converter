module journey_markdown_converter.CommandLine

open System
open System.CommandLine
open System.CommandLine.Invocation
open System.IO

type Options =
    { InFile: FileInfo
      OutDirectory: DirectoryInfo
      OverrideExisting: bool
      MdGithubFlavoured: bool
      MdListBulletChar: char
      MdSmartHrefHandling: bool }



/// Add a list of symbols to an item that
let inline private addSymbols (seq: Symbol seq) (collection: ^T) =
    let add symbol =
        (^T: (member Add : Symbol -> unit) (collection, symbol))

    seq |> Seq.iter add
    collection


let private dumpTemplateCommand (dumpTemplate: string -> int) =
    Command(
        "dump-template",
        "Dumps the default Handlebars template to the specified file",
        Handler = CommandHandler.Create(fun outfile -> dumpTemplate outfile)
    )
    |> addSymbols [ Argument<string>("outfile") ]

let rootCommand (main: Options -> int) dumpTemplate =

    RootCommand(
        Description = "Converts a Journey JSON ZIP Export file into Markdown",
        Handler = CommandHandler.Create(main)
    )
    |> addSymbols (
        [ dumpTemplateCommand dumpTemplate
          Argument<string>("infile", "Journey JSON Export ZIP File")
          Option<string>(
              aliases = [| "--out-directory"; "-i" |],
              description = "Specifies the output directory, if omitted it's next to the ZIP file with the same name"
          )
          Option<bool>(
              aliases = [| "-f"; "--override-existing" |],
              getDefaultValue = (fun () -> false),
              description = "Override existing files inside output directory"
          )
          Option<bool>(
              alias = "--md-github-flavoured",
              getDefaultValue = (fun () -> false),
              description = "Markdown: enable GitHub flavoured"
          )
          Option<char>(
              alias = "--md-list-bullet-char",
              getDefaultValue = (fun () -> '-'),
              description = "Markdown: List bullet character"
          )
          Option<bool>(
              alias = "--md-smart-href-handling",
              getDefaultValue = (fun () -> true),
              description = "Markdown: Smart href Handling"
          ) ]: Symbol list
    )
