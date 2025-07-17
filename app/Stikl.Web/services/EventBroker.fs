module Stikl.Web.services.EventBroker

open System

open System.Collections.Concurrent
open Microsoft.Extensions.DependencyInjection
open System.Threading.Channels
open Microsoft.Extensions.Logging
open domain
open Stikl.Web
open System.Threading
open FSharp.Control

type EventBroker(logger: EventBroker ILogger) =
    let mutable channels: ConcurrentDictionary<Guid, UserEvent Channel> =
        ConcurrentDictionary<Guid, UserEvent Channel>()

    member this.Listen(cancellationToken: CancellationToken) : UserEvent TaskSeq =
        let channel = Channel.CreateUnbounded<UserEvent>()

        let id = Guid.NewGuid()

        while (channels.TryAdd(id, channel)) do
            ()

        channel.Reader.ReadAllAsync cancellationToken

    member this.Publish (event: UserEvent) (cancellationToken: CancellationToken) =
        channels.Values
        |> Seq.map _.Writer.WriteAsync(event, cancellationToken)
        |> ValueTask.whenAll

let register: IServiceCollection -> IServiceCollection =
    Services.registerSingletonType<EventBroker>
