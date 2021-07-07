module journey_markdown_converter.JourneyZipReader

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

let regex =
    Regex(@"(?<date>[0-9]*)-(?<entryId>[a-zA-Z0-9]*)(-(?<attachmentId>[a-zA-Z0-9]*))?.*(?<extension>\.[a-zA-Z0-9]*)")

let parseFileName fileName =
    let match' = regex.Match(fileName)

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
    let zip = ZipFile.OpenRead fileName

    try
        let zipEntryByPrefix =
            zip.Entries
            |> Seq.map (fun zipEntry -> (zipEntry, parseFileName zipEntry.FullName))
            |> Seq.groupBy (fun (_, id) -> id.Prefix)
            |> Seq.toList

        let entries =
            /// find the main json file of this diary entry
            let getMainEntry =
                Seq.tryFind
                    (fun (_, id) ->
                        String.IsNullOrEmpty(id.AttachmentId)
                        && id.Extension = ".json")

            /// get a sequence with all attachments
            let getAttachments =
                Seq.filter (fun (_, id) -> not (String.IsNullOrEmpty(id.AttachmentId)))

            let createJourneyZipEntry (mainEntry, mainEntryId) attachments =
                { id = mainEntryId
                  zipEntry = mainEntry
                  attachments =
                      attachments
                      |> Seq.map
                          (fun (entry, id) ->
                              { id = id
                                zipEntry = entry
                                attachments = [] })
                      |> Seq.toList }

            zipEntryByPrefix
            |> Seq.map
                (fun (_, entries) ->
                    {| mainEntryOption = getMainEntry entries
                       attachments = getAttachments entries |})
            |> Seq.choose
                (fun x ->
                    x.mainEntryOption
                    |> Option.map (fun mainEntry -> createJourneyZipEntry mainEntry x.attachments))
            |> Seq.toList

        { archive = zip; entries = entries }

    with ex ->
        zip.Dispose()
        reraise ()