module journey_markdown_converter.JourneyEntryParser

open System
open System.Collections.Generic
open System.Net.Sockets
open System.Text.Json
open System.Text.Json.Serialization
open journey_markdown_converter.JourneyZipReader

type JourneyWeather =
    { id: int
      degree_c: double
      description: string
      icon: string
      place: string }

[<CLIMutable>]
type JourneyEntry =
    { id: string
      date_modified: DateTimeOffset
      date_journal: DateTimeOffset
      timezone: string
      text: string
      preview_text: string  
      mood: int
      lat: double
      lon: double
      address: string
      label: string
      folder: string
      [<JsonExtensionData>] ExtensionData: Dictionary<string, JsonElement>  
      sentiment: int
      favourite: bool
      music_title: string
      music_artist: string
      photos: string array
      weather: JourneyWeather
      tags: string array
      [<JsonPropertyName("type")>]
      entry_type: string }


type unixTimestampDateFormatter() =
    inherit JsonConverter<DateTimeOffset>()

    override this.Read(reader, typeToConvert, options) =
        let unixTsMillis = reader.GetInt64()
        DateTimeOffset.FromUnixTimeMilliseconds(unixTsMillis)

    override this.Write(writer, value, options) =
        raise (NotSupportedException "only read supported")


let serializerOptions =
    let opts = JsonSerializerOptions()
    opts.Converters.Add(unixTimestampDateFormatter ())
    opts

let parseEntry stream =
    JsonSerializer
        .DeserializeAsync<JourneyEntry>(stream, serializerOptions)
        .AsTask()
    |> Async.AwaitTask
