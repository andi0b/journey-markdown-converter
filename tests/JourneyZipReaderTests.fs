module tests.JourneyZipReaderTests

open Xunit
open FsUnit.Xunit
open journey_markdown_converter



type ``Given the JourneyZipReader opens journey-multiple-1``() =

    let data =
        JourneyZipReader.readZip "testdata/journey-multiple-1.zip"

    [<Fact>]
    member x.``when I check the archive, it should not be null``() = data.archive |> should not' (be Null)

    [<Fact>]
    member x.``when I check the entries, the ids should be like``() =
        let expectedIds =
            [ "1625494049547-4BbDhzcByTFSg6MW"
              "1625494143867-82d3goucmybxlxat" ]

        data.entries
        |> List.map (fun x -> x.id)
        |> should matchList expectedIds

    [<Theory>]
    [<InlineData("1625494049547-4BbDhzcByTFSg6MW", 1)>]  
    [<InlineData("1625494143867-82d3goucmybxlxat", 0)>]
    member x.``one specific entry should have specific attachment count`` (id, attachmentCount) =
        let entryWithAttachment =
            data.entries
            |> Seq.find (fun x -> x.id = id)

        entryWithAttachment.attachments.Length
        |> should equal attachmentCount
