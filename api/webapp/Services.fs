module Services

open System
open Microsoft.Extensions.DependencyInjection

let registerSingleton (f: IServiceProvider -> 'a) (s: IServiceCollection) = s.AddSingleton<'a> f
let registerSingletonType<'a when 'a: not struct> (s: IServiceCollection) = s.AddSingleton<'a>()

let registerScoped (f: IServiceProvider -> 'a) (s: IServiceCollection) = s.AddScoped<'a> f
