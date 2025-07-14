module webapp.routes.Auth

open System
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open Stikl.Web.Pages
open type TypedResults
open domain
open webapp
open webapp.Components.Htmx
open webapp.services
open webapp.services.User

type CreateUserParms =
    { pageBuilder: PageBuilder
      [<FromForm>]
      username: string
      [<FromForm>]
      firstName: string
      [<FromForm>]
      lastName: string
      store: UserStore
      identity: CurrentUser
      eventHandler: EventHandler
      context: HttpContext }

let routes =
    endpoints {
        group "auth"

        get
            "/login"
            (fun
                (req:
                    {| context: HttpContext
                       returnUrl: string |}) ->

                task {
                    // Indicate here where Auth0 should redirect the user after a login.
                    // Note that the resulting absolute Uri must be added to the
                    // **Allowed Callback URLs** settings for the app.
                    let returnUrl = if isNull req.returnUrl then "/" else req.returnUrl

                    let authenticationProperties =
                        LoginAuthenticationPropertiesBuilder()
                            .WithRedirectUri(returnUrl)
                            // Added here as the program part doesnt do much.
                            .WithScope("openid profile name email username")
                            .Build()

                    do! req.context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties)
                })

        endpoints {
            requireAuthorization

            get "/logout" (fun (req: {| context: HttpContext |}) ->
                task {
                    let authenticationProperties =
                        LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build()

                    do! req.context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties)
                    do! req.context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                })

            get
                "/create"
                (fun
                    (req:
                        {| context: HttpContext
                           layout: Layout.Builder
                           antiForgery: IAntiforgery |}) ->
                    // TODO: redirect if user exists. mybe in middleware
                    let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)
                    req.layout.render (Pages.Auth.Create.render antiForgeryToken))

            post "/create" (fun (req: CreateUserParms) ->
                // TODO: validate username
                let authId =
                    match req.identity with
                    | NewUser authId -> authId
                    | _ -> failwith "User not new?"
                
                let username = Username req.username

                if
                    String.IsNullOrWhiteSpace req.firstName
                    || String.IsNullOrWhiteSpace req.lastName
                then
                    failwith "firstname lastname cannot be empty"


                let event =
                    { user = username
                      timestamp = DateTimeOffset.UtcNow
                      payload =
                        CreateUser
                            { username = username
                              firstName = req.firstName
                              lastName = req.lastName
                              authId = authId } }
                // TODO: should we use the handler? currently doesn't work.
                req.store.ApplyEvent event
                |> Task.map (
                    Result.map (fun _ -> Results.Redirect("/"))
                    // TODO: The redirect seems to be broken, and only redirects the main thing.
                    >> Result.defaultWith Results.BadRequest
                ))

            get
                "/profile"
                (fun
                    (req:
                        {| layout: Layout.Builder
                           users: UserStore
                           user: CurrentUser |}) ->
                    req.user.get
                    |> Option.defaultWith (fun () -> failwith "Cannot see profile if not logged in!")
                    |> Pages.Auth.Profile.render
                    |> req.layout.render)
        }
    }
