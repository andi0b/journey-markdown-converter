module tests.CommandLineTests

open System
open System.IO
open Xunit
open FsUnit.Xunit
open journey_markdown_converter
open journey_markdown_converter.CommandLine


type ParseResult =
    | Options of CommandLineOptions
    | DumpTemplate of string
    | None

let getParsed (args: string) =
    let argv =
        args.Split(' ', StringSplitOptions.RemoveEmptyEntries)

    let mutable retval: ParseResult = None

    let fakeMain options =
        retval <- Options options
        0

    let fakeDumpTemplate outfile =
        retval <- DumpTemplate outfile

        0

    invoke fakeMain fakeDumpTemplate argv |> ignore

    retval


type ``Given no command line parameters``() =
    let parsed = getParsed ""

    [<Fact>]
    member x.``Expect result None``() = parsed |> should equal None


type ``Given only infile parameter``() =

    let options = getParsed "infile.zip"

    [<Fact>]
    member x.``Expect default CommandLineOptions``() =
        let expected =
            Options
                { InFile = "infile.zip"
                  OutDirectory = ""
                  OverrideExisting = false
                  Verbose = false

                  AdditionalTags = [||]
                  TagPrefix = ""
                  SetFileTimes = false

                  MdGithubFlavoured = false
                  MdListBulletChar = '-'
                  MdSmartHrefHandling = false
                  MdPassThroughTags = [||]
                  MdUnknownTags = ReverseMarkdown.Config.UnknownTagsOption.PassThrough
                  MdTableWithoutHeaderRowHandling = ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption.Default
                  MdWhitelistUriSchemes = [||]

                  FileNameTemplate =
                      """{{String.Format date_journal "yyyy-MM-dd"}} {{String.Truncate preview_text_md_oneline 100}}"""
                  BodyTemplate = ""
                  CleanFromFileNameChars = "[]#.," }

        options |> should equal expected


type ``Given all options Expect custom options result``() =

    let expectedCustomOptions =
        Options
            { InFile = "infile.zip"
              OutDirectory = "../test/dir"
              OverrideExisting = true
              Verbose = true

              AdditionalTags = [| "tag1"; "tag2" |]
              TagPrefix = "tagprefix"
              SetFileTimes = true

              MdGithubFlavoured = true
              MdListBulletChar = '+'
              MdSmartHrefHandling = true
              MdPassThroughTags = [| "span"; "div" |]
              MdUnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Drop
              MdTableWithoutHeaderRowHandling = ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption.EmptyRow
              MdWhitelistUriSchemes = [| "ftp"; "gopher" |]

              FileNameTemplate = "myTemplate"
              BodyTemplate = "myBodyTemplate"
              CleanFromFileNameChars = "abc" }


    [<Fact>]
    member x.``check with long argument``() =
        let options =
            getParsed
                "--out-directory ../test/dir \
                  --override-existing \
                  --verbose \
                  --file-name-template myTemplate \
                  --body-template myBodyTemplate \
                  --additional-tags tag1 tag2 \
                  --tag-prefix tagprefix \
                  --clean-from-filename-chars abc \
                  --set-file-times \
                  --md-github-flavoured \
                  --md-list-bullet-char + \
                  --md-smart-href-handling \
                  --md-whitelist-uri-schemes ftp gopher \
                  --md-pass-through-tags span div \
                  --md-unknown-tags drop \
                  --md-table-without-header-row-handling EmptyRow \
                  infile.zip"

        options |> should equal expectedCustomOptions


    [<Fact>]
    member x.``check with shortcodes``() =
        let options =
            getParsed
                "-o ../test/dir \
                  -f \
                  -v \
                  -n myTemplate \
                  -b myBodyTemplate \
                  -d tag1 tag2 \
                  -p tagprefix \
                  -c abc \
                  -t \
                  --md-github-flavoured \
                  --md-list-bullet-char + \
                  --md-smart-href-handling \
                  --md-pass-through-tags span div \
                  --md-whitelist-uri-schemes ftp gopher \
                  --md-unknown-tags drop \
                  --md-table-without-header-row-handling EmptyRow \
                  infile.zip"

        options |> should equal expectedCustomOptions

type ``given dumpTemplate``() =
    let parsed = getParsed "dump-template path.template"

    [<Fact>]
    member x.``check dumptemplate``() =
        parsed
        |> should equal (DumpTemplate "path.template")
