namespace journey_markdown_converter

open System
open System.CommandLine
open System.CommandLine.Builder
open System.CommandLine.Invocation
open System.CommandLine.IO
open System.CommandLine.Parsing
open System.IO
open System.Reflection

type CommandLineOptions =
    { InFile: string
      OutDirectory: string
      OverrideExisting: bool
      Verbose: bool

      AdditionalTags: string array
      TagPrefix: string

      MdGithubFlavoured: bool
      MdListBulletChar: char
      MdSmartHrefHandling: bool
      MdPassThroughTags: string array
      MdUnknownTags: ReverseMarkdown.Config.UnknownTagsOption
      MdTableWithoutHeaderRowHandling: ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption
      MdWhitelistUriSchemes: string array

      FileNameTemplate: string
      CleanFromFileNameChars: string }


type ExitCodeException(message: string, innerException: Exception, exitCode: int) =
    inherit Exception(message, innerException)
    new(message: string, innerException: Exception) = ExitCodeException(message, innerException, 1)
    member x.ExitCode = exitCode


module CommandLine =

    /// Add a list of symbols to an item that
    let inline private addSymbols (seq: Symbol seq) (collection: ^T) =
        let add symbol =
            (^T: (member Add : Symbol -> unit) (collection, symbol))

        seq |> Seq.iter add
        collection

    let private dumpTemplateCommand (dumpTemplate: string -> int) =
        // keep lambda with named parameter here (fun outfile->), otherwise the binding of the value fails!
        Command(
            "dump-template",
            "Dumps the default Handlebars template to the specified file",
            Handler = CommandHandler.Create(fun outfile -> dumpTemplate outfile)
        )
        |> addSymbols [ Argument<string>("outfile", (fun () -> ""), "File to dump default template to") ]

    let createRootCommand (main: CommandLineOptions -> int) dumpTemplate =

        RootCommand(
            Description = "Converts a Journey JSON ZIP Export file into Markdown",
            Handler = CommandHandler.Create(main)
        )
        |> addSymbols (
            [ dumpTemplateCommand dumpTemplate
              Argument<string>("infile", "Journey JSON Export ZIP File")
              Option<string>(
                  aliases = [| "--out-directory"; "-o" |],
                  description =
                      "Specifies the output directory, if omitted it's next to the ZIP file with the same name"
              )
              Option<bool>(
                  aliases = [| "-f"; "--override-existing" |],
                  getDefaultValue = (fun () -> false),
                  description = "Override existing files inside output directory"
              )
              Option<bool>(
                  aliases = [| "-v"; "--verbose" |],
                  getDefaultValue = (fun () -> false),
                  description = "Show more information and more detailed error messages"
              )
              Option<string>(
                  alias = "--file-name-template",
                  getDefaultValue =
                      (fun () ->
                          """{{String.Format date_journal "yyyy-MM-dd"}} {{String.Truncate preview_text_md_oneline 100}}"""),
                  description = "File name for the created Markdown file"
              )
              Option<string array>(
                  aliases = [| "-d"; "--additional-tags" |],
                  description = "Additional tags that should be added to all files "
              )
              Option<string>(
                  aliases = [| "-p"; "--tag-prefix" |],
                  description = "Prefix that will be prepended to all tags (but not the additional tags)"
              )
              Option<string>(
                  alias = "--clean-from-filename-chars",
                  getDefaultValue = (fun () -> "[]#.,"),
                  description = "Additional characters to clean from file name, forbidden characters are always cleaned"
              )

              // md options
              Option<bool>(
                  alias = "--md-github-flavoured",
                  getDefaultValue = (fun () -> false),
                  description = "Markdown: enable GitHub flavoured"
              )
              Option<char>(
                  alias = "--md-list-bullet-char",
                  getDefaultValue = (fun () -> '-'),
                  description = "Markdown: set a different bullet character for un-ordered lists"
              )
              Option<bool>(
                  alias = "--md-smart-href-handling",
                  getDefaultValue = (fun () -> false),
                  description = "Markdown: Smart href Handling"
              )
              Option<string array>(
                  alias = "--md-pass-through-tags",
                  getDefaultValue = (fun () -> [||]),
                  description = "Markdown: pass a list of tags to pass through as is without any processing"
              )
              Option<ReverseMarkdown.Config.UnknownTagsOption>(
                  alias = "--md-unknown-tags",
                  getDefaultValue = (fun () -> ReverseMarkdown.Config.UnknownTagsOption.PassThrough),
                  description = "Markdown: pass a list of tags to pass through as is without any processing"
              )
              Option<ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption>(
                  alias = "--md-table-without-header-row-handling",
                  getDefaultValue = (fun () -> ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption.Default),
                  description = "Markdown: Table: By default, first row will be used as header row"
              )
              Option<string array>(
                  alias = "--md-whitelist-uri-schemes",
                  getDefaultValue = (fun () -> [||]),
                  description =
                      "Markdown: Specify which schemes (without trailing colon) are to be allowed for\
                                a and img; tags. Others will be bypassed. By default allows everything."
              ) ]: Symbol list
        )

    let onException (ex: exn) (context: InvocationContext) =
        let con = context.Console

        let isVerbose =
            context.ParseResult.ValueForOption<bool>("--verbose")

        let unwrapped =
            match ex with
            | :? TargetInvocationException as ti -> ti.InnerException
            | e -> e

        // todo: better/colorful console formatting
        if not (unwrapped :? OperationCanceledException) then
            if isVerbose then
                con.Error.WriteLine("Error:")
                con.Error.WriteLine(unwrapped.ToString())
            else
                con.Error.WriteLine($"Error: {unwrapped.Message}")

        context.ExitCode <-
            match unwrapped with
            | :? ExitCodeException as ae -> ae.ExitCode
            | _ -> 1

    let invoke main dumpTemplate (argv: string []) =
        let rootCommand = createRootCommand main dumpTemplate

        let parser =
            CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler(Action<_, _>(onException))
                .Build()

        parser.Invoke(argv, null)
