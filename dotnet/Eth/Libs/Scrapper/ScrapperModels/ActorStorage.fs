namespace ScrapperModels

open System.Threading.Tasks

type ActorStore<'a> =
  { Set: 'a -> Task
    Get: unit -> Task<'a option>
    Remove: unit -> Task<bool> }
