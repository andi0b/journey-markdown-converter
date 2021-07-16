module journey_markdown_converter.Converter

open System.IO
open journey_markdown_converter.JourneyEntryParser

let convertEntries
    (mdConverter: MdConverter)
    (bodyFormatter: JourneyEntry -> string)
    (fileNameFormatter: JourneyEntry -> string)
    (inFile: string)
    (outDirectory: string)
    (openOutFile: string -> FileStream option)
    =

    use zip = JourneyZipReader.readZip inFile

    DirectoryInfo(outDirectory).Create()

    let exportEntry entry =     
        let journeyEntry = readZipFile entry mdConverter
        let fileName = fileNameFormatter journeyEntry
        let fileExtension = ".md"

        let attachmentFileName (attachment: JourneyZipEntry) =
            $"{fileName}-{attachment.id.AttachmentId}{attachment.id.Extension}"

        let photoIds =
            query {
                for photo in journeyEntry.photos do
                    join attachment in entry.attachments on (photo = attachment.id.Id)
                    select (attachmentFileName attachment)
            }
            |> Seq.toArray

        let formatted = bodyFormatter { journeyEntry with photos = photoIds }

        let writeMd (stream: Stream) =
            use writer = new StreamWriter(stream)
            writer.Write(formatted)

        let writeAttachments () =

            let writeAttachment (attachment: JourneyZipEntry) =

                openOutFile (attachmentFileName attachment)
                |> Option.map
                    (fun stream ->
                        use stream = stream
                        attachment.zipEntry.Open().CopyTo(stream))
                |> ignore

            entry.attachments |> Seq.iter writeAttachment


        openOutFile (fileName + fileExtension)
        |> Option.map writeMd
        |> Option.map writeAttachments
        |> ignore
        
    for entry in zip.entries do
        exportEntry entry

    0


let convertEntriesFromOptions options =
    let formatters =
        HandlebarsFormatter.createFromOptions options

    let openFile fileName =
        let path =
            Path.Combine(options.OutDirectory, fileName)

        if (File.Exists(path) && not options.OverrideExisting) then
            System.Console.WriteLine($"Skipping file {path}, because it exists")
            None

        else
            System.Console.WriteLine($"Writing file {path}")
            Some(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))

    convertEntries
        (MdConverter.createFromOptions options)
        formatters.bodyFormatter
        formatters.fileNameFormatter
        options.InFile
        options.OutDirectory
        openFile
