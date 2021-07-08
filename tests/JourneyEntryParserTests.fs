module tests.JourneyEntryParserTests

open System
open System.IO
open System.Text
open Xunit
open journey_markdown_converter

let testContent1 = """{
    "id": "1625494049547-4BbDhzcByTFSg6MW",
    "date_modified": 1625494100149,
    "date_journal": 1625133600000,
    "timezone": "Europe/Vienna",
    "text": "<p dir=\"auto\">Test Entry</p>\n<p dir=\"auto\"></p>\n<p dir=\"auto\"><strong>bold </strong><em>italic </em><strong><em>bold-italic</em></strong></p>",
    "preview_text": "<p dir=\"auto\">Test Entry</p>\n<p dir=\"auto\"></p>\n<p dir=\"auto\"><strong>bold </strong><em>italic </em><strong><em>bold-italic</em></strong></p>",
    "mood": 0,
    "lat": 1.7976931348623157e+308,
    "lon": 1.7976931348623157e+308,
    "address": "",
    "label": "",    
    "folder": "",
    "sentiment": 0,
    "favourite": false,
    "music_title": "",
    "music_artist": "",
    "photos": [
        "1625494049547-4BbDhzcByTFSg6MW-5bkOJACgIzl36wsU.jpg"
    ],
    "weather": {
        "id": -1,
        "degree_c": 1.7976931348623157e+308,
        "description": "",
        "icon": "",
        "place": ""
    },
    "tags": [],
    "type": "html"
}"""


[<Fact>]
let check ()=
    let openStream () = new MemoryStream(Encoding.UTF8.GetBytes(testContent1)) :> Stream
    let parsed = JourneyEntryParser.parseEntry openStream 
    ()