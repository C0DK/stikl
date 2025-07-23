module Services

open System
open Microsoft.Extensions.DependencyInjection

let registerSingleton (f: IServiceProvider -> 'a) (s: IServiceCollection) = s.AddSingleton<'a> f
let registerSingletonType<'a when 'a: not struct> (s: IServiceCollection) = s.AddSingleton<'a>()
let registerHttpClient<'a when 'a: not struct> (s: IServiceCollection) = s.AddHttpClient<'a>().Services

let registerScoped (f: IServiceProvider -> 'a) (s: IServiceCollection) = s.AddScoped<'a> f
let registerScopedType<'a when 'a: not struct> (s: IServiceCollection) = s.AddScoped<'a>()
let registerTransient (f: IServiceProvider -> 'a) (s: IServiceCollection) = s.AddTransient<'a> f
