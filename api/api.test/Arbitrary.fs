module api.test.Arbitrary


open FsCheck.FSharp
open domain

module Gen =
    let pick l =
        l |> Array.map (fun v -> gen { return v }) |> Gen.oneof

type CustomArbitrary =

    static member genUserId =
        ArbMap.defaults
        |> ArbMap.generate<char>
        // TODO utilize domain.UserId.isValid
        |> Gen.filter UserId.isSafeChar
        |> Gen.arrayOfLength 5
        |> Gen.map (System.String >> UserId)


    static member UserId() =
        CustomArbitrary.genUserId |> Arb.fromGen

type PropertyAttribute() =
    inherit FsCheck.Xunit.PropertyAttribute(QuietOnSuccess = true, Arbitrary = [| typeof<CustomArbitrary> |])
