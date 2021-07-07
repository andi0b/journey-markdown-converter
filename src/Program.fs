open System.CommandLine
open System.IO
open System.Reflection
open System.Text
open journey_markdown_converter
open journey_markdown_converter.CommandLine
open journey_markdown_converter.JourneyEntryParser
open journey_markdown_converter.JourneyZipReader

let mainTyped options =

    let mdConverter = MdConverter.createFrmSettings options

    use zip =
        JourneyZipReader.readZip options.InFile.FullName

    let openFile fileName =
        let path =
            Path.Combine(options.OutDirectory.FullName, fileName)

        if (File.Exists(path) && not options.OverrideExisting) then
            System.Console.WriteLine($"Skipping file {path}, because it exists")
            None

        else
            System.Console.WriteLine($"Writing file {path}")
            Some(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))



    let bodyFormatter, fileNameFormatter =
        let formatter =
            HandlebarsFormatter.createFromOptions options

        ((HandlebarsFormatter.format formatter.bodyTemplate),
         (HandlebarsFormatter.format formatter.fileNameTemplate)
         >> formatter.fileNameCleaner)



    let iter entry =
        let typed, untyped = readZipFile entry mdConverter

        options.OutDirectory.Create()

        let fileName = fileNameFormatter (typed, untyped)
        let fileExtension = ".md"

        let attachmentFileName attachment =
            $"{fileName}-{attachment.id.AttachmentId}{attachment.id.Extension}"

        let photoIds =
            query {
                for photo in typed.photos do
                    join attachment in entry.attachments on (photo = attachment.id.Id)
                    select (attachmentFileName attachment)
            }
            |> Seq.toArray

        let typed2 = { typed with photos = photoIds }

        let formatted = bodyFormatter (typed2, untyped)

        let writeMd (stream: Stream) =
            use writer = new StreamWriter(stream)
            writer.Write(formatted)

        let writeAttachments () =

            let writeAttachment (attachment: JourneyZipEntry) =

                openFile (attachmentFileName attachment)
                |> Option.map
                    (fun stream ->
                        use stream = stream
                        attachment.zipEntry.Open().CopyTo(stream))
                |> ignore

            entry.attachments |> Seq.iter writeAttachment


        openFile (fileName + fileExtension)
        |> Option.map writeMd
        |> Option.map writeAttachments
        |> ignore

        ()

    zip.entries |> Seq.iter iter

    0

let dumpTemplate (file: string) =
    System.Console.WriteLine($"Write template to file: {file}")
    0

[<EntryPoint>]
let main argv =
    let rootCommand = rootCommand mainTyped dumpTemplate
    CommandExtensions.Invoke(rootCommand, argv)
