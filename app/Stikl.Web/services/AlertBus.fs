namespace Stikl.Web.services

open System.Threading
open System.Threading.Channels
open FSharp.Control
open domain

// Messages to push to the user at the end of a request
type AlertBus() =
    let channel = Channel.CreateUnbounded<Alert>(options = UnboundedChannelOptions(SingleReader=true))

    member this.push(alert: Alert) =
        while not(channel.Writer.TryWrite alert) do
            // TODO save in session!
            ()

    member this.flush(cancellationToken: CancellationToken) =
        channel.Writer.Complete()
       
        channel.Reader.ReadAllAsync(cancellationToken) |> TaskSeq.toList
           

