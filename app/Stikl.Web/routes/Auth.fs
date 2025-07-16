module Stikl.Web.routes.Auth

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open Stikl.Web.Components
open Stikl.Web.Pages
open type TypedResults
open domain
open Stikl.Web
open Stikl.Web.services.User

type CreateUserParms =
    { layout: Layout.Builder
      antiForgery: IAntiforgery
      [<FromForm>]
      username: string
      [<FromForm>]
      firstName: string
      [<FromForm>]
      lastName: string
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
                    req.layout.render (Pages.Auth.Create.render antiForgeryToken None))

            post "/create" (fun (req: CreateUserParms) ->
                let authId =
                    match req.identity with
                    | NewUser authId -> authId
                    | _ -> failwith "User not new?"
                // localization on errors
                // TODO: validate username unique

                let validateNonEmpty v =
                    if String.IsNullOrWhiteSpace v then
                        [ "Dette felt skal udfyldes" ]
                    else
                        []

                let validateAlphaNumericUnderscores v =
                    if v |> Seq.exists ((fun c -> Char.IsLetterOrDigit c && Char.IsLower c) >> not) then
                        [ "Må kun indeholde små bogstaver og tal" ]
                    else
                        []

                let form =
                    { username =
                        (Pages.Auth.Create.Field.create
                            (req.username.ToLowerInvariant().Trim())
                            [ validateNonEmpty; validateAlphaNumericUnderscores ])
                      firstName = (Pages.Auth.Create.Field.create req.firstName [ validateNonEmpty ])
                      lastName = (Pages.Auth.Create.Field.create req.lastName [ validateNonEmpty ]) }
                    : Pages.Auth.Create.Form

                if form.isValid then
                    CreateUser
                        { username = Username form.username.value   
                          firstName = form.firstName.value
                          lastName = form.lastName.value
                          authId = authId }
                    |> req.eventHandler.handle
                    |> Task.map (
                        Result.map (fun _ -> Results.Redirect("/", preserveMethod = false))
                        // TODO: push success message.
                        >> Message.errorToResult
                    )
                else
                    let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)

                    Pages.Auth.Create.render antiForgeryToken (Some form)
                    |> req.layout.render
                    |> Task.FromResult)

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
