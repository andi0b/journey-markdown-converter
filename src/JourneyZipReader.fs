namespace journey_markdown_converter

open System
open System.IO.Compression
open System.Text.RegularExpressions

type JourneyZipEntryId =
    { Id: string
      Prefix: string
      Date: string
      EntryId: string
      AttachmentId: string
      Extension: string }

type JourneyZipEntry =
    { id: JourneyZipEntryId
      zipEntry: ZipArchiveEntry
      attachments: JourneyZipEntry List }

type JourneyZipArchive =
    { archive: ZipArchive
      entries: JourneyZipEntry List }

    interface IDisposable with
        member x.Dispose() = x.archive.Dispose()


module JourneyZipReader =

    let zipFileRegex =
        Regex(
            @"(?<date>[0-9]*)-(?<entryId>[a-zA-Z0-9]*)(-(?<attachmentId>[a-zA-Z0-9]*))?.*(?<extension>\.[a-zA-Z0-9]*)"
        )

    let parseFileName fileName =
        let match' = zipFileRegex.Match(fileName)

        match match'.Success with
        | true ->
            let value (x: string) = match'.Groups.[x].Value

            { Id = fileName
              Prefix = value "date" + "-" + value "entryId"
              Date = value "date"
              EntryId = value "entryId"
              AttachmentId = value "attachmentId"
              Extension = value "extension" }

        | false ->
            { Id = fileName
              Prefix = fileName
              Date = String.Empty
              EntryId = String.Empty
              AttachmentId = String.Empty
              Extension = String.Empty }

    let readZip fileName =

        let toJourneyZipEntry zipEntry =
            { zipEntry = zipEntry
              id = parseFileName zipEntry.FullName
              attachments = [] }

        // take a list of journey zip entries find the first main entry and attach all attachments to it 
        let mainEntryFromGroup group =
            let isAttachment id =
                not (String.IsNullOrEmpty(id.AttachmentId))

            let isMainEntry id =
                String.IsNullOrEmpty(id.AttachmentId)
                && id.Extension = ".json"

            let createMainEntry mainEntry =
                let attachments =
                    group
                    |> Seq.filter (fun e -> isAttachment e.id)
                    |> Seq.toList

                { mainEntry with
                      attachments = attachments }

            group
            |> Seq.tryFind (fun e -> isMainEntry e.id)
            |> Option.map createMainEntry


        let zip = ZipFile.OpenRead fileName

        try

            let entries =
                zip.Entries
                |> Seq.map toJourneyZipEntry
                |> Seq.groupBy (fun x -> x.id)
                |> Seq.map snd
                |> Seq.choose mainEntryFromGroup
                |> Seq.toList

            { archive = zip; entries = entries }

        with
        | ex ->
            zip.Dispose()
            reraise ()
