module Stikl.Web.Components.Message

open Microsoft.AspNetCore.Http
open Stikl.Web

type Variant =
    | SuccessMessage
    | ErrorMessage


let render (variant: Variant) (title: string) (message: string) =
    let borderColor =
        match variant with
        | SuccessMessage -> "border-lime-600"
        | ErrorMessage -> "border-red-600"

    let bgColor =
        match variant with
        | SuccessMessage -> "bg-lime-600/20"
        | ErrorMessage -> "bg-red-600/20"

    let titleColor =
        match variant with
        | SuccessMessage -> "text-lime-600"
        | ErrorMessage -> "text-red-600"

    // language=HTML
    $"""
    <div hx-swap-oob="afterbegin: #messages">
        <div 
            class="border-2 {borderColor} {bgColor} shadow-xl appearance-none rounded py-1 px-2 transition ease-in-out duration-300 hover:scale-105"
            _="init wait 8s remove me on click remove me"
        >
            <h4 class="font-bold {titleColor} text-balanced">{title}</h4>
            <p class="pl-2 text-wrap text-sm">{message}</p>
        </div>    
    </div>    
    """

let error = render ErrorMessage

let errorToResult (result: Result<IResult, string>) =
    match result with
    | Ok v -> v
    | Error message -> error "Ups!" message |> Results.HTML
