module webapp.services.User

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open domain
open webapp

type CurrentUser =
    | CurrentUser of (unit -> User option Task)

    member this.get =
        match this with
        | CurrentUser f -> f
    member this.FirstLogin =
        match this with
        | CurrentUser f -> f

type RedirectIfAuthedWithoutUser(next: RequestDelegate) =
    // TODO: make this less slow and shitty
    member this.InvokeAsync (context: HttpContext, principal: Principal option, currentUser: CurrentUser)  =
        task {
            let! user = currentUser.get()
            
            return! if (Option.isSome principal && Option.isNone user && not(context.Request.Path.StartsWithSegments("/auth/create"))) then
                       Results.Redirect("/auth/create").ExecuteAsync(context)
                    else
                        next.Invoke(context)
            
        }
   

let register: IServiceCollection -> IServiceCollection =
    Services.registerScoped (fun s ->
        let store = s.GetRequiredService<UserStore>()
        let principal = s.GetService<Principal option>()

        // TODO cache transient
        CurrentUser(fun () -> principal |> Option.bindTask (_.auth0Id >> store.GetByAuthId)))
