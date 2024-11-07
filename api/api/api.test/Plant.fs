module Plant

open System.Net
open Xunit
open FsCheck.FSharp
open api
open api.test

let clientWithPlants = APIClient.withPlants >> APIClient.build

[<Arbitrary.Property>]
let ``GetAll returns all entries`` plants =
    task {
        let client = clientWithPlants plants

        let! result = client |> Http.getJson<List<Dto.PlantSummary>> "/Plant/"

        let expected = plants |> List.map Dto.PlantSummary.fromDomain
        Assert.Equivalent(expected, result)
    }

[<Arbitrary.Property>]
let ``Get returns plant is in list`` plants (plant: domain.Plant) =
    let client = clientWithPlants (plant :: plants)

    client
    |> Http.getJson<Dto.Plant> $"/Plant/{plant.id}"
    |> Task.map (Assert.equal (Dto.Plant.fromDomain plant))

[<Arbitrary.Property>]
let ``Get returns 404 if not in list`` plants plant =
    not (List.contains plant plants)
    ==> (let client = clientWithPlants plants

         client
         |> Http.get $"/Plant/{plant.id}/"
         |> Assert.asyncHasStatusCode HttpStatusCode.NotFound)
