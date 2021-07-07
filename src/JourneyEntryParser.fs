module journey_markdown_converter.JourneyEntryParser

open System
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open NodaTime

type JourneyWeather =
    { id: int
      degree_c: double
      description: string
      icon: string
      place: string }

[<CLIMutable>]
type JourneyJsonEntry =
    { id: string
      date_modified: int64
      date_journal: int64
      timezone: string
      text: string
      preview_text: string
      mood: int
      lat: double
      lon: double
      address: string
      label: string
      folder: string
      sentiment: double
      favourite: bool
      music_title: string
      music_artist: string
      photos: string array
      weather: JourneyWeather
      tags: string array }

type JourneyEntry =
    { id: string
      date_modified: DateTimeOffset
      date_journal: DateTimeOffset
      text_md: string
      preview_text_md: string
      preview_text_md_oneline:string
      photos: string array }

    member x.date_modified_utc = x.date_modified.ToUniversalTime()

    member x.date_journal_utc = x.date_journal.ToUniversalTime()

module JourneyEntry =
    let parseTz tz =
        let (|?) lhs rhs = (if lhs = null then rhs else lhs)

        DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz)
        |? DateTimeZoneProviders.Bcl.GetSystemDefault()

    let fromJourneyJsonEntry (mdConverter : MdConverter) j =

        let tz = parseTz j.timezone

        let parseDate unixMillis =
            Instant
                .FromUnixTimeMilliseconds(unixMillis)
                .InZone(tz)
                .ToDateTimeOffset()

        let toMd = mdConverter |> MdConverter.convertHtml  

        let preview_text_md = j.preview_text |> toMd
        let preview_text_md_oneline = preview_text_md.TrimStart([|'\n';'\r';' '|]).Split(Environment.NewLine, 2).[0]
        
        { id = j.id
          date_modified = j.date_modified |> parseDate
          date_journal = j.date_journal |> parseDate
          text_md = j.text |> toMd
          preview_text_md = preview_text_md
          preview_text_md_oneline = preview_text_md_oneline
          photos = j.photos }

let private deserializeFromStream<'a> stream =
    use textReader =
        new StreamReader(stream, leaveOpen = true)

    use jsonReader = new JsonTextReader(textReader)

    JsonSerializer
        .CreateDefault()
        .Deserialize<'a>(jsonReader)

let private parseEntry (openStream: unit->Stream) =
    
    use stream1 = openStream()
    let typed =
        deserializeFromStream<JourneyJsonEntry> stream1
    stream1.Close()

    use stream2 = openStream()
    let untyped = deserializeFromStream<JObject> stream2
    stream2.Close()

    (typed, untyped)
    
let readZipFile (entry: JourneyZipReader.JourneyZipEntry) mdConverter =
    let typed, untyped = parseEntry entry.zipEntry.Open
    
    let jEntry = JourneyEntry.fromJourneyJsonEntry mdConverter typed
    
    (jEntry, untyped)