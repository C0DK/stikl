module webapp.routes.Auth

open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open webapp.services
open webapp.services.User

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
                "/profile"
                (fun
                    (req:
                        {| pageBuilder: Components.Htmx.PageBuilder
                           users: domain.UserStore
                           user: CurrentUser |}) ->
                    (req.user.get ())
                    |> Task.collect (
                        Option.defaultWith (fun () -> failwith "Cannot see profile if not logged in!")
                        >> Pages.Auth.Profile.render
                        >> req.pageBuilder.toPage
                    )

                )

        }
    }
