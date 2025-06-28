module webapp.services.sse

open System
open Microsoft.AspNetCore.Http

open webapp
open FSharp.Control
open webapp.services.Htmx


let stream (response: HttpResponse) (seq: string TaskSeq) =
    task {
        response.ContentType <- "text/event-stream"
        response.Headers.Add("CacheControl", "no-cache")
        response.Headers.Add("Connection", "keep-alive")
        response.StatusCode <- 200
        do! response.StartAsync()

        Console.WriteLine "start"
        do!
            seq
            |> TaskSeq.eachAsync (fun payload ->
                task {
                    Console.WriteLine $"Got a payload:\n{payload}"
                    let safePayload = payload.ReplaceLineEndings(Environment.NewLine + "data: ")
                    do! response.WriteAsync($"data: {safePayload}\n\n")
                    do! response.Body.FlushAsync()
                })

    }

let renderPage (endpoint: string) (pageBuilder: PageBuilder) =
    pageBuilder.toPage
        // language=html
        $"""
        <div
        hx-ext="sse" sse-connect="{endpoint}" sse-swap="message"
        >
            Loading...
        </div>
        """
