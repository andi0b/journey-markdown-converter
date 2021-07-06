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
      sentiment: int
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
      preview_text_md: string }

    member x.date_modified_utc = x.date_modified.ToUniversalTime()

    member x.date_journal_utc = x.date_journal.ToUniversalTime()

module JourneyEntry =
    let parseTz tz =
        let (|?) lhs rhs = (if lhs = null then rhs else lhs)

        DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz)
        |? DateTimeZoneProviders.Bcl.GetSystemDefault()

    let fromJourneyJsonEntry mdConverter j =

        let tz = parseTz j.timezone

        let parseDate unixMillis =
            Instant
                .FromUnixTimeMilliseconds(unixMillis)
                .InZone(tz)
                .ToDateTimeOffset()

        let toMd = mdConverter << MdConverter.convertHtml

        { id = j.id
          date_modified = j.date_modified |> parseDate
          date_journal = j.date_journal |> parseDate
          text_md = j.text |> toMd
          preview_text_md = j.preview_text |> toMd }

let private deserializeFromStream<'a> stream =
    use textReader =
        new StreamReader(stream, leaveOpen = true)

    use jsonReader = new JsonTextReader(textReader)

    JsonSerializer
        .CreateDefault()
        .Deserialize<'a>(jsonReader)

let private parseEntry stream =
    let typed =
        deserializeFromStream<JourneyJsonEntry> stream

    stream.Position <- int64 0
    let untyped = deserializeFromStream<JObject> stream

    (typed, untyped)
    
let readZipFile (entry: JourneyZipReader.JourneyZipEntry) mdConverter =
    use stream = entry.zipEntry.Open()
    let typed, untyped = parseEntry stream
    
    let jEntry = JourneyEntry.fromJourneyJsonEntry mdConverter typed
    
    (jEntry, untyped)