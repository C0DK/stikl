module Services

open System
open Microsoft.Extensions.DependencyInjection

let registerSingleton (f : IServiceProvider -> 'a) (s : IServiceCollection) =
    s.AddSingleton<'a> f
    
let registerScoped (f: IServiceProvider -> 'a)  (s : IServiceCollection)=
    s.AddScoped<'a> f
