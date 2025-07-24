module Stikl.Web.Components.Toast

open Microsoft.AspNetCore.Http
open Stikl.Web
open domain

let render (variant: ToastVariant) (title: string) (message: string) =
    let borderColor =
        match variant with
        | SuccessToast -> "border-lime-600"
        | ErrorToast -> "border-red-600"

    let bgColor =
        match variant with
        | SuccessToast -> "bg-lime-50"
        | ErrorToast -> "bg-red-50"

    let titleColor =
        match variant with
        | SuccessToast -> "text-lime-600"
        | ErrorToast -> "text-red-600"

    // language=HTML

    $"""
    <div 
        class="border-2 {borderColor} {bgColor} shadow-xl appearance-none rounded py-1 px-2 transition ease-in-out duration-300 hover:scale-105"
        _="init wait 8s remove me on click remove me"
    >
        <h4 class="font-bold {titleColor} text-balanced">{title}</h4>
        <p class="pl-2 text-wrap text-sm">{message}</p>
    </div>    
    """

let renderAsSwap (variant: ToastVariant) (title: string) (message: string) =
    $"""
    <div hx-swap-oob="afterbegin: #toasts">
    {render variant title message}
    </div>    
    """

let renderToast (alert: Toast) =
    render alert.variant alert.title alert.message

let errorSwap = renderAsSwap ErrorToast

let errorToResult (result: Result<IResult, string>) =
    match result with
    | Ok v -> v
    | Error message -> errorSwap "Ups!" message |> Results.HTML

let errorsToResult (result: Result<IResult, string list>) =
    match result with
    | Ok v -> v
    | Error errors -> errors |> List.map (errorSwap "Ups!") |> String.concat "\n" |> Results.HTML
