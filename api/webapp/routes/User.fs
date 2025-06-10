module webapp.routes.User

open System.Text
open System.Threading
open FSharp.Control
open Microsoft.AspNetCore.Http

open System.Threading.Tasks
open FSharp.MinimalApi.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open type TypedResults
open webapp
open domain
open webapp.services.EventBroker
open webapp.services.Htmx

let routes =
    endpoints {
        group "user"

        get
            "/"
            (fun
                (req:
                    {| pageBuilder: PageBuilder
                       users: UserStore |}) ->
                task {
                    let! users = req.users.GetAll()

                    let! cards = users |> List.map req.pageBuilder.userCard |> Task.merge |> Task.map Seq.toList

                    return! req.pageBuilder.toPage (Components.grid cards)
                })
            

        // If buttons are pressed on your OWN page, it is not refreshed with new users.
        get
            "/{username}"
            (fun
                (req:
                    {| pageBuilder: PageBuilder
                       users: UserStore
                       username: string |}) ->
                task {
                    // TODO: parse/verify username
                    let! userOption = req.users.Get(Username req.username)

                    // TODO: use result instead, and generalize 404 pages.
                    let! content =
                        match userOption with
                        | Some user ->
                            task {
                                let plantArea title plants =
                                    task {
                                        let! cardGrid =
                                            plants
                                            |> Seq.map req.pageBuilder.plantCard
                                            |> Task.merge
                                            |> Task.map (Components.grid)

                                        return
                                            $"""                           
                                         <div class="flex flex-col justify-items-center">
                                            {Components.PageHeader title}
                                            {cardGrid}
                                          </div>"""
                                    }

                                let name = user.fullName |> Option.defaultValue user.username.value

                                let! needsPlantArea = plantArea $"{name} søger:" user.wants
                                // TODO handle plant
                                let! seedsPlantArea = plantArea $"{name} har:" (user.seeds |> Seq.map _.plant)

                                let events =
                                    user.history
                                    |> Seq.map (fun e -> $"<li>{e.ToString()}</li>")
                                    |> String.concat "\n"


                                let events = $"<ul>{events}</ul>"

                                return
                                    $"""
             <div class="flex w-full justify-between pl-10 pt-5">
                <div class="flex">
                    <div class="mr-5">
                        <img
                            alt="Image of a {name}"
                            class="h-32 w-32 rounded-full object-cover"
                            src="{user.imgUrl}"
                        />
                    </div>
                    <div class="content-center">
                        <h1 class="font-sans text-3xl font-bold text-lime-800">{name}</h1>
                        <p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
                            Location etc
                        </p>
                    </div>
                    
                </div>
            </div>
            {seedsPlantArea}
            {needsPlantArea}
            <div class="flex flex-col justify-items-center">
               {Components.PageHeader "History"}
               {events}
            </div>
            """
                            }
                        | None ->
                            // TODO dedicated 404 helper?
                            Task.FromResult(
                                (Components.PageHeader "User not found!")
                                + $"""
        <p class="text-center text-lg md:text-xl">
          Vi kunne desværre ikke finde {Components.themeGradiantSpan req.username}
        </p>
        """
                                + Components.search
                            )

                    return! req.pageBuilder.toPage content
                })

        get
            "/{username}/sse"
            (fun
                (req:
                    {| pageBuilder: PageBuilder
                       ctx: HttpContext
                       resp: HttpResponse
                       eventBroker: EventBroker
                       life: IHostApplicationLifetime
                       logger: ILogger<User>
                       users: UserStore
                       username: string |}) ->
                task {
                    // TODO: parse/verify username
                    let! userOption = req.users.Get(Username req.username)
                    let cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(req.life.ApplicationStopping, req.ctx.RequestAborted)

                    let response = req.resp
                    let cancellationToken = cancellationTokenSource.Token 
                    return!
                        match userOption with
                        | Some user ->
                            task {
                                response.ContentType <- "text/event-stream"
                                response.Headers.Add("CacheControl", "no-cache");
                                response.Headers.Add("Connection", "keep-alive")
                                response.StatusCode <- 200
                                do! response.StartAsync()
                                
                                req.logger.LogInformation("SSE")
                                do! response.WriteAsync($"data: {user.username}\n\n", cancellationToken)
                                do! response.Body.FlushAsync(cancellationToken)
                                do! req.eventBroker.Listen cancellationToken
                                    |> TaskSeq.eachAsync (fun e ->
                                        task {
                                            req.logger.LogInformation(e.ToString())
                                            do! response.WriteAsync($"data: {e.ToString()}\n\n")
                                            do! response.Body.FlushAsync()
                                        }
                                    )
                            }
                        | None -> task {
                            // TODO: better
                            let payload = Encoding.UTF8.GetBytes("404\n\n")
                            response.StatusCode <- 404
                            do! response.Body.WriteAsync(payload, cancellationToken)
                        }
                })

    }
