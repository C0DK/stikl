module webapp.routes.Auth

open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Identity
open type TypedResults
open webapp
open webapp.Page

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
                        LoginAuthenticationPropertiesBuilder().WithRedirectUri(returnUrl).Build()

                    do! req.context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties)
                })

        endpoints {
            // TODO require auth
            requireAuthorization

            // TODO: show page or something and confirm?
            get "/logout" (fun (req: {| context: HttpContext |}) ->
                task {
                    let authenticationProperties =
                        LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build()

                    do! req.context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties)
                    do! req.context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                    do! req.context.SignOutAsync(IdentityConstants.ApplicationScheme)
                })

            get
                "/profile"
                (fun
                    (req:
                        {| pageBuilder: PageBuilder
                           userService: UserService |}) ->
                    let user = req.userService.Get()

                    req.pageBuilder.ToPage
                        $"""
                        <h1 class="font-bold italic text-xl font-sans">
                            Hi, {user.username}!
                        </h1>
                        <p>
                        Her burde der nok være settings. men nah.
                        </p>
                        <a
                            class="transform rounded-lg border-2 px-3 py-1 border-red-900 font-sans text-sm font-bold text-red-900 transition hover:scale-105"
                            href="/logout"
                        >
                            Log Out
                        </a>
                    """)

        }
    }
