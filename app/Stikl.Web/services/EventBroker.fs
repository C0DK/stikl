module Stikl.Web.services.EventBroker

open System

open System.Collections.Concurrent
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open System.Threading.Channels
open Microsoft.Extensions.Logging
open domain
open Stikl.Web
open System.Threading
open FSharp.Control


let register: IServiceCollection -> IServiceCollection =
    Services.registerSingletonType<EventBroker>
