module Tests

open System
open Xunit
open FsUnit.Xunit

[<Fact>]
let ``My test`` () = Assert.True(true)

type ``Given something``() =
    [<Fact>]
    member x.``when I ask whether it is On it answers true.``() =
        true |> should be True
        
    [<Fact>]
    member x.``with calculating 1+1 should equal 2`` ()=
        1 - 1 |> should equal 2
