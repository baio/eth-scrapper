namespace ScrapperModels.Scrapper

open System.Threading.Tasks
open ScrapperModels

type IScrapperActor =
  abstract Scrap: data: ScrapperRequest -> Task<bool>
