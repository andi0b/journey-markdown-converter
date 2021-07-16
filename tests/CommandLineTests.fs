module tests.CommandLineTests

open System
open System.IO
open Xunit
open FsUnit.Xunit
open journey_markdown_converter
open journey_markdown_converter.CommandLine


type ``Given some command line parameters check CommandLineOptions binding``() =

    let getOptions (args: string) =
        let argv =
            args.Split(' ', StringSplitOptions.RemoveEmptyEntries)

        let mutable retval: obj Option = None

        let fakeMain options =
            retval <- Some options
            0

        invoke fakeMain (fun _ -> 0) argv |> ignore

        retval
        |> Option.map (fun o -> o :?> CommandLineOptions)


    [<Fact>]
    member x.``no parameters check for expected None``() =
        let options = getOptions ""

        options |> should equal None


    [<Fact>]
    member x.``only infile check for expected default values``() =

        let options = getOptions "infile.zip"

        let expected =
            Some(
                { InFile = "infile.zip"
                  OutDirectory = ""
                  OverrideExisting = false
                  Verbose = false

                  AdditionalTags = [||]
                  TagPrefix = ""

                  MdGithubFlavoured = false
                  MdListBulletChar = '-'
                  MdSmartHrefHandling = false
                  MdPassThroughTags = [||]
                  MdUnknownTags = ReverseMarkdown.Config.UnknownTagsOption.PassThrough
                  MdTableWithoutHeaderRowHandling = ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption.Default
                  MdWhitelistUriSchemes = [||]

                  FileNameTemplate = """{{String.Format date_journal "yyyy-MM-dd"}} {{String.Truncate preview_text_md_oneline 100}}"""
                  CleanFromFileNameChars = "[]#." }
            )
        
        options |> should equal expected
        
    [<Fact>]
    member x.``all options check for expected custom``() =
        let options = getOptions "--out-directory ../test/dir \
                                  --override-existing \
                                  --verbose \
                                  --file-name-template myTemplate \
                                  --additional-tags tag1 tag2 \
                                  --tag-prefix tagprefix \
                                  --clean-from-filename-chars abc \
                                  infile.zip"
        
        let expected =
            Some(
                { InFile = "infile.zip"
                  OutDirectory = "../test/dir"
                  OverrideExisting = true
                  Verbose = true

                  AdditionalTags = [|"tag1";"tag2"|]
                  TagPrefix = "tagprefix"

                  MdGithubFlavoured = false
                  MdListBulletChar = '-'
                  MdSmartHrefHandling = false
                  MdPassThroughTags = [||]
                  MdUnknownTags = ReverseMarkdown.Config.UnknownTagsOption.PassThrough
                  MdTableWithoutHeaderRowHandling = ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption.Default
                  MdWhitelistUriSchemes = [||]

                  FileNameTemplate = "myTemplate"
                  CleanFromFileNameChars = "abc" }
            )
        
        options |> should equal expected


