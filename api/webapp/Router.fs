module webapp.Router

open System.Security.Claims
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open domain
open webapp.PageBuilder

let toPlantCards l =
    l |> List.map Components.plantCard |> String.concat "\n"

let routes =
    endpoints {
        // TODO: use pageBuilder on all endpoints.
        get "/" (fun (req: {| pageBuilder: PageBuilder |}) ->
            let stiklingerFrøOgPlanter =
                Components.themeGradiantSpan "Stiklinger, frø og planter"

            let title = Htmx.PageHeader $"Find {stiklingerFrøOgPlanter} nær dig"

            let callToAction =
                """
<p class="mb-8 max-w-md text-center text-lg md:text-xl">
    Deltag i et fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.
</p>
"""

            req.pageBuilder.ToPage(title + callToAction + Components.search))

        get
            "/login"
            (fun
                (req:
                    {| context: HttpContext
                       user: ClaimsPrincipal
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

        // TODO require auth
        // TODO: show page or something and confirm?
        get "/logout" (fun (req: {| context: HttpContext |}) ->
            task {
                let authenticationProperties =
                    LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build()

                do! req.context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties)
                do! req.context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
            })

        // TODO require auth
        // TODO: inject username in header to display whether logged in!
        get "/profile" (fun (req: {| user: ClaimsPrincipal |}) ->
            let name = req.user.Identity.Name

            match req.user.Identity.IsAuthenticated with
            | true ->
                let getClaim t =
                    req.user.Claims
                    |> Seq.tryFind (fun claim -> claim.Type = t)
                    |> Option.map (_.Value)

                let email = getClaim ClaimTypes.Email |> Option.defaultValue "??"
                let img = getClaim "picture" |> Option.defaultValue "??"

                Htmx.page
                    $"""
                    <h1 class="font-bold text-xl font-sans">
                        Hello {name} ({email})
                    </h1>
                    
                    <img src="{img}"/>
                    
            <a
                class="transform rounded-lg border-2 px-3 py-1 border-red-900 font-sans text-sm font-bold text-red-900 transition hover:scale-105"
                href="/logout"
            >
                Log Out
            </a>
                """
            | false -> Htmx.page "hold up!")

        get "/search" (fun (req: {| query: string |}) ->
            let plantCards =
                Composition.plants
                |> List.filter (_.name.ToLower().Contains(req.query.ToLower()))
                |> toPlantCards

            Htmx.toOkResult plantCards)

        get "/plant" (fun () ->
            let plantCards = Composition.plants |> toPlantCards

            Htmx.page (Components.grid plantCards))

        get "/plant/{id}" (fun (req: {| id: string |}) ->
            let plantOption =
                Composition.plants |> List.tryFind (fun p -> p.id.ToString() = req.id)

            (match plantOption with
             | Some plant ->
                 Htmx.page
                     $"""
                          
 <div class="flex w-full justify-between pl-10 pt-5">
 	<div class="flex">
 		<div class="mr-5">
 			<img
 				alt="Image of a {plant.name}"
 				class="h-32 w-32 rounded-full object-cover"
 				src={plant.image_url}
 			/>
 		</div>
 		<div class="content-center">
 			<h1 class="font-sans text-3xl font-bold text-lime-800">{plant.name}</h1>
 			<p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
    TODO beskrivelse og tags?
 			</p>
 		</div>
 	</div>
  </div>
 """
             | None ->
                 Htmx.page (
                     (Htmx.PageHeader "Plant not found!")
                     + $"""
<p class="text-center text-lg md:text-xl">
  No plant exists with id {Components.themeGradiantSpan req.id}
</p>
"""
                     + Components.search
                 )))

    }
