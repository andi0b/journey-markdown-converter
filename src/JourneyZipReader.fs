module journey_markdown_converter.JourneyZipReader

open System
open System.IO.Compression

type JourneyZipEntry =
    { id: string
      zipEntry: ZipArchiveEntry
      attachments: ZipArchiveEntry List }

type JourneyZipArchive =
    { archive: ZipArchive
      entries: JourneyZipEntry List }

    interface IDisposable with
        member x.Dispose() = x.archive.Dispose()

let readZip fileName =
    let zip = ZipFile.OpenRead fileName

    try
        let findAttachments (prefix: string) =
            zip.Entries
            |> Seq.filter (fun zipEntry -> zipEntry.FullName.StartsWith(prefix + "-"))
            |> Seq.toList

        let entries =
            zip.Entries
            |> Seq.filter
                (fun zipEntry ->
                    zipEntry
                        .FullName
                        .ToLowerInvariant()
                        .EndsWith(".json"))
            |> Seq.map
                (fun zipEntry ->
                    let fullName = zipEntry.FullName

                    let id =
                        fullName.Substring(0, fullName.Length - 5)

                    { id = id
                      zipEntry = zipEntry
                      attachments = findAttachments id })
            |> Seq.toList

        { archive = zip; entries = entries }

    with ex ->
        zip.Dispose()
        reraise ()
