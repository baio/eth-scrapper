namespace Common.DaprActor

module ActorResult =

  open System.Threading.Tasks

  type ActorOptionResult<'a> = { Data: 'a option }

  let actorOptionResultSome data = { Data = Some data }

  let actorOptionResultNone () = { Data = None }

  let actorOptionResult data = { Data = data }

  let toActorOptionResult<'a> (t: Task<'a option>) =
    task {
      let! result = t
      let result = { Data = result }
      return result
    }
