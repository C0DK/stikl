module webapp.routes.Auth

open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Identity
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
                        {| renderPage: Htmx.PageBuilder
                           users: domain.UserStore
                           user: CurrentUser
                           principal: Principal option |}) ->
                    task {
                    let principal =
                        req.principal
                        |> Option.defaultWith (fun () -> failwith "Cannot see profile if not logged in!")

                    let! user =
                        (req.user.get ())
                        |> Task.map (Option.defaultWith (fun () -> failwith "Cannot see profile if not logged in!"))
                    let keyVaule key value =
                        $"<p><span class=\"font-bold text-lime-800 text-xs pr-2\">{key}</span>{value}</p>"

                    let claims =
                        principal.claims
                        |> List.map (fun claim -> keyVaule claim.key claim.value)
                        |> String.concat "\n"


                    return! req.renderPage.toPage
                        $"""
                        <h1 class="font-bold italic text-xl font-sans">
                            Hi, {user.username}!
                        </h1>
                        <p>
                        Her burde der nok være settings. men nah.
                        </p>
                        <div class="text-left">
                        {keyVaule "full name" (user.fullName |> Option.defaultValue "N/A")}
                        {keyVaule "firstname" (user.firstName |> Option.defaultValue "N/A")}
                        {keyVaule "username" user.username}
                        {keyVaule "Auth id" user.authId}
                        <h1 class="font-bold italic">Dine security claims</h1>
                        {claims}
                        </div>
                        <a
                            class="transform rounded-lg border-2 px-3 py-1 border-red-900 font-sans text-sm font-bold text-red-900 transition hover:scale-105"
                            href="/auth/logout"
                        >
                            Log Out
                        </a>
                    """
                    })

        }
    }
