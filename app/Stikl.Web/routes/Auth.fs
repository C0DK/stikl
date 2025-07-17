module Stikl.Web.routes.Auth

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

type UpdateProfileParams =
    { layout: Layout.Builder
      antiForgery: IAntiforgery
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
                // TODO: fail later?
                if (req.identity.IsAuthedUser) then
                    failwith "Cannot create user that already exists!"

                let authId =
                    match req.identity with
                    | NewUser authId -> authId
                    | _ -> failwith "User not new?"
                // localization on errors
                // TODO: validate username unique

                // TOOO validate no injection in first name . i.e %@<>

                let form =
                    { username =
                        (TextField.create
                            (req.username.ToLowerInvariant().Trim())
                            [ TextField.validateNonEmpty; TextField.validateAlphaNumericUnderscores ])
                      firstName = (TextField.create req.firstName [ TextField.validateNonEmpty ])
                      lastName = (TextField.create req.lastName [ TextField.validateNonEmpty ]) }
                    : Pages.Auth.Create.Form

                // TODO: add message
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
                           antiForgery: IAntiforgery
                           context: HttpContext
                           user: CurrentUser |}) ->

                    let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)

                    req.user.get
                    |> Option.defaultWith (fun () -> failwith "Cannot see profile if not logged in!")
                    |> Pages.Auth.Profile.render antiForgeryToken None
                    |> req.layout.render)

            post "/profile" (fun (req: UpdateProfileParams) ->
                let form =
                    { firstName = (TextField.create req.firstName [ TextField.validateNonEmpty ])
                      lastName = (TextField.create req.lastName [ TextField.validateNonEmpty ]) }
                    : Pages.Auth.Profile.Form

                if form.isValid then
                    UpdateName(firstName = form.firstName.value, lastName = form.lastName.value)
                    |> req.eventHandler.handle
                    |> Task.map (
                        Result.map (fun _ -> Results.Redirect("", preserveMethod = false))
                        // TODO: push success message.
                        >> Message.errorToResult
                    )
                else
                    let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)

                    let user = req.identity.get |> Option.orFail

                    Pages.Auth.Profile.render antiForgeryToken (Some form) user
                    |> req.layout.render
                    |> Task.FromResult)
        }
    }
