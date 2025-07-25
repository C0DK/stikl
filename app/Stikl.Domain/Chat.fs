// Not used yet... current we use the event source. TODO?
module Stikl.Domain

open System
open System.Threading
open System.Threading.Tasks
open domain

type ChatMessage =
    { sender: Username
      receiver: Username
      message: string
      timestamp: DateTimeOffset }


type Chat =
    { participants: Username * Username
      messages: ChatMessage List

    }


type ChatStore =
    // TODO: ensure ordering doesnt matter
    abstract member Get: (Username*Username) -> cancellationToken: CancellationToken -> Chat option Task
    abstract member Write: (Username*Username) -> ChatMessage -> cancellationToken: CancellationToken -> Chat option Task
module Chat =
    let sortParticipants (memberA: Username) (memberB: Username) =
        if memberA.value < memberB.value then  (memberA, memberB) else (memberB, memberA)
    let AddMessage (message: ChatMessage) (chat: Chat) =
        { chat with
            messages = message :: chat.messages }

    // TODO do we wanna do it like this or as events?
    let Create (memberA: Username) (memberB: Username) =
        { participants = sortParticipants memberA memberB
          messages = [] }
