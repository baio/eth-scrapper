namespace Common.DaprActor

// TODO : Once more common library added, move there

[<AutoOpen>]
module DateTime =
  let toEpoch (dateTime: System.DateTime) =
    System
      .DateTimeOffset(dateTime)
      .ToUnixTimeSeconds()

  let epoch () = System.DateTime.UtcNow |> toEpoch
