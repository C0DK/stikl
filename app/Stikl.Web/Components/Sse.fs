module Stikl.Web.Components.Sse

open System
open Microsoft.AspNetCore.Http

open FSharp.Control

type SSEResult(seq: string TaskSeq) =
    interface IResult with
        member this.ExecuteAsync(context) =
            task {
                let response = context.Response
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

                do! seq |> Stikl.Utils.TaskSeq.eachAsync (fun payload -> task { do! send payload })

            }

let streamDivWithInitialValue (initial: string) (endpoint: string) =
    // language=html
    $"""
    <div hx-ext="sse" sse-connect="{endpoint}" sse-swap="message">
        {initial}
    </div>
    """

let streamDiv = streamDivWithInitialValue Spinner.render


let iresult seq = SSEResult(seq) :> IResult
