module webapp.routes.Auth

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
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
                    {| identity: CurrentUser
                       returnUrl: string |}) ->
                let returnUrl = if isNull req.returnUrl then "/" else req.returnUrl

                if not (req.identity.IsAnonymous) then
                    Results.Redirect(returnUrl)
                else

                    let properties = AuthenticationProperties(RedirectUri = returnUrl)

                    Challenge(
                        properties,
                        [ CookieAuthenticationDefaults.AuthenticationScheme
                          OpenIdConnectDefaults.AuthenticationScheme ]
                        |> Seq.toArray
                        :> IList<string>
                    ))

        
        endpoints {
            requireAuthorization

            get
                "/logout"
                (fun
                    (req:
                        {| identity: CurrentUser
                           returnUrl: string |}) ->
                    let returnUrl = if isNull req.returnUrl then "/" else req.returnUrl
                    let properties = AuthenticationProperties(RedirectUri = returnUrl)

                    SignOut(
                        properties,
                        [ CookieAuthenticationDefaults.AuthenticationScheme
                          OpenIdConnectDefaults.AuthenticationScheme ]
                        |> Seq.toArray
                        :> IList<string>
                    ))

            get
                "/create"
                (fun
                    (req:
                        {| context: HttpContext
                           pageBuilder: PageBuilder
                           antiForgery: IAntiforgery |}) ->
                    // TODO: redirect if user exists. mybe in middleware
                    let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)
                    req.pageBuilder.toPage (Pages.Auth.Create.render antiForgeryToken))

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
                    >> Result.defaultWith Results.BadRequest
                ))

            get
                "/profile"
                (fun
                    (req:
                        {| pageBuilder: PageBuilder
                           users: UserStore
                           user: CurrentUser |}) ->
                    req.user.get
                    |> Option.defaultWith (fun () -> failwith "Cannot see profile if not logged in!")
                    |> Pages.Auth.Profile.render
                    |> req.pageBuilder.toPage)
        }
    }
