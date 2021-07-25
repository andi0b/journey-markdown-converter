module journey_markdown_converter.Converter

open System
open System.IO
open System.IO
open journey_markdown_converter.JourneyEntryParser

type SetFileTimes =
    string
        -> {| CreationTimeUtc: DateTime
              LastWriteTimeUtc: DateTime |}
        -> unit

let convertEntries
    (mdConverter: MdConverter)
    (bodyFormatter: JourneyEntry -> string)
    (fileNameFormatter: JourneyEntry -> string)
    (inFile: string)
    (outDirectoryParameter: string)
    (openOutFile: string -> FileStream option)
    (setFileTimes: SetFileTimes)
    =

    use zip = JourneyZipReader.readZip inFile

    let exportEntry entry =
        let journeyEntry = readZipFile entry mdConverter
        let fileName = fileNameFormatter journeyEntry
        let fileExtension = ".md"

        let fileTimes =
            {| CreationTimeUtc = journeyEntry.date_journal_utc.UtcDateTime
               LastWriteTimeUtc = journeyEntry.date_modified_utc.UtcDateTime |}

        let attachmentFileName (attachment: JourneyZipEntry) =
            $"{fileName}-{attachment.id.AttachmentId}{attachment.id.Extension}"

        let photoIds =
            query {
                for photo in journeyEntry.photos do
                    join attachment in entry.attachments on (photo = attachment.id.Id)
                    select (attachmentFileName attachment)
            }
            |> Seq.toArray

        let formatted =
            bodyFormatter { journeyEntry with photos = photoIds }

        let writeMd (stream: Stream) =
            use writer = new StreamWriter(stream)
            writer.Write(formatted)
           
        let writeAttachments () =
           
            let writeAttachment (attachment: JourneyZipEntry) =

                let afn = attachmentFileName attachment
                
                openOutFile afn
                |> Option.map
                    (fun stream ->
                        use stream = stream
                        attachment.zipEntry.Open().CopyTo(stream))
                |> ignore
                
                setFileTimes afn fileTimes

            entry.attachments |> Seq.iter writeAttachment
            
            
        openOutFile (fileName + fileExtension)
        |> Option.map writeMd
        |> Option.map writeAttachments
        |> ignore
        
        setFileTimes (fileName + fileExtension) fileTimes


    for entry in zip.entries do
        exportEntry entry

    0


let convertEntriesFromOptions options =
    let formatters =
        HandlebarsFormatter.createFromOptions options

    let getOutDirectory inFile outDirectoryParameter =

        if (String.IsNullOrWhiteSpace outDirectoryParameter) then
            let fromInfile =
                Path.ChangeExtension(inFile, String.Empty).TrimEnd('.')

            if (fromInfile = inFile) then
                fromInfile + ".out"
            else
                fromInfile
        else
            outDirectoryParameter
        |> DirectoryInfo


    let outDirectoryInfo =
        getOutDirectory options.InFile options.OutDirectory

    outDirectoryInfo.Create()

    let openFile fileName =
        let path =
            Path.Combine(outDirectoryInfo.FullName, fileName)

        if (File.Exists(path) && not options.OverrideExisting) then
            System.Console.WriteLine($"Skipping file {path}, because it exists")
            None

        else
            System.Console.WriteLine($"Writing file {path}")
            Some(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))

    let setFileTimes
        fileName
        (times: {| CreationTimeUtc: DateTime
                   LastWriteTimeUtc: DateTime |})
        =
        if options.SetFileTimes then
            let filePath = Path.Combine(outDirectoryInfo.FullName, fileName)
            File.SetCreationTimeUtc(filePath, times.CreationTimeUtc)
            File.SetLastWriteTimeUtc(filePath, times.LastWriteTimeUtc)

    convertEntries
        (MdConverter.createFromOptions options)
        formatters.bodyFormatter
        formatters.fileNameFormatter
        options.InFile
        options.OutDirectory
        openFile
        setFileTimes
