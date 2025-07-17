module Stikl.Web.Components.sse

open System
open System.Text
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

open Stikl.Web
open FSharp.Control


let stream (response: HttpResponse) (init: string) (seq: string TaskSeq) =
    task {
        response.ContentType <- "text/event-stream"
        response.Headers.Add("CacheControl", "no-cache")
        response.Headers.Add("Connection", "keep-alive")
        response.StatusCode <- 200
        do! response.StartAsync()

        let send (payload: string) =
            task {
                let safePayload = payload.ReplaceLineEndings(Environment.NewLine + "data: ")
                do! response.WriteAsync($"data: {safePayload}\n\n")
                do! response.Body.FlushAsync()
            }

        do! send init


        try
            do! seq |> TaskSeq.eachAsync (fun payload -> task { do! send payload })
        with :? TaskCanceledException ->
            printf "task cancelled"

    }

let streamDiv (endpoint: string) =
    // language=html
    $"""
    <div hx-ext="sse" sse-connect="{endpoint}" sse-swap="message">
        {Spinner.render}
    </div>
    """

let NotFound404 (response: HttpResponse) (cancellationToken: CancellationToken) =
    task {
        let payload = Encoding.UTF8.GetBytes("404\n\n")
        response.StatusCode <- 404
        do! response.Body.WriteAsync(payload, cancellationToken)
    }
