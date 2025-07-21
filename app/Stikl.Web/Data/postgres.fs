module Stikl.Web.Data.postgres


open System
open System.Data.Common
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks
open Microsoft.FSharp.Reflection
open Npgsql
open FSharp.Control
open NpgsqlTypes
open domain

// 1. Either create the serializer options from the F# options...
let jsonSerializerOptions =
    JsonFSharpOptions
        .Default()
        // Add any .WithXXX() calls here to customize the format
        .ToJsonSerializerOptions()

type PostgresUserRepository(db: NpgsqlDataSource) =
    let serialize (payload: UserEventPayload) : string =
        JsonSerializer.Serialize<UserEventPayload>(payload, jsonSerializerOptions)

    let deserialize (payload: string) : UserEventPayload =
        // this probably maybe doesnt work
        JsonSerializer.Deserialize<UserEventPayload>(payload, jsonSerializerOptions)

    let ReadUserEvents (readerT: DbDataReader Task) (cancellationToken: CancellationToken) : UserEvent TaskSeq =
        taskSeq {
            let! reader = readerT

            while! reader.ReadAsync(cancellationToken) do
                let username = reader.GetString(0)
                let timestamp = reader.GetDateTime(1)
                let eventType = reader.GetString(2)
                let payload = reader.GetString(3)

                yield
                    { user = Username username
                      timestamp = timestamp |> DateTimeOffset
                      payload = deserialize payload }
        }

    let getEventsOfUser (username: Username) (cancellationToken: CancellationToken) : UserEvent TaskSeq =
        taskSeq {
            use connection = db.CreateConnection()
            do! connection.OpenAsync(cancellationToken)

            use command =
                new NpgsqlCommand(
                    //language=postgresql
                    """
                    SELECT
                        username,
                        timestamp,
                        event_type,
                        payload
                    FROM user_events
                    WHERE username = @username
                    ORDER BY timestamp
                    """,
                    connection
                )

            command.Parameters.Add(NpgsqlParameter("@username", username.value)) |> ignore

            for event in ReadUserEvents (command.ExecuteReaderAsync()) cancellationToken do
                yield event
        }

    let getAllEvents (cancellationToken: CancellationToken) : UserEvent TaskSeq =
        taskSeq {
            use! connection = db.OpenConnectionAsync()

            use command =
                new NpgsqlCommand(
                    //language=postgresql
                    """
                    SELECT
                        username,
                        timestamp,
                        event_type,
                        payload
                    FROM user_events
                    ORDER BY timestamp
                    """,
                    connection
                )

            for event in ReadUserEvents (command.ExecuteReaderAsync()) cancellationToken do
                yield event

        }

    let writeEvent (event: UserEvent) (cancellationToken: CancellationToken) : Result<UserEvent, string> Task =
        task {
            use! connection = db.OpenConnectionAsync()

            use command =
                new NpgsqlCommand(
                    //language=postgresql
                    """
                    INSERT INTO user_events (username,
                                             timestamp,
                                             event_type,
                                             payload)
                    VALUES (@username, 
                            @timestamp,
                            @event_type,
                            @payload)
                    """,
                    connection
                )

            command.Parameters.Add(NpgsqlParameter("@username", event.user.value)) |> ignore
            command.Parameters.Add(NpgsqlParameter("@timestamp", event.timestamp)) |> ignore

            let eventType =
                match FSharpValue.GetUnionFields(event.payload, typeof<UserEventPayload>) with
                | case, _ -> case.Name

            command.Parameters.Add(NpgsqlParameter("@event_type", eventType)) |> ignore
            let mutable payloadParam = NpgsqlParameter("@payload", serialize event.payload)
            payloadParam.NpgsqlDbType <- NpgsqlDbType.Jsonb
            command.Parameters.Add(payloadParam) |> ignore

            return! command.ExecuteNonQueryAsync(cancellationToken) |> Task.map (fun _ -> Ok event)
        }

    let fold user event =

        match user with
        | Some user -> user |> apply event.payload
        | None ->
            match event.payload with
            | CreateUser payload -> User.create payload
            | wrongEvent -> failwith $"First event of user {event.user.value} was a {wrongEvent.ToString()}"

    interface UserStore with
        member this.Get(username: Username) : User option Task =
            // TODO cancellationtoken?
            getEventsOfUser username CancellationToken.None
            |> TaskSeq.fold
                // how to compose??
                (fold >> (fun f v -> Some(f v)))
                None


        member this.GetAll() : User list Task =
            let events = getAllEvents (CancellationToken.None)

            events
            |> TaskSeq.fold
                (fun (users: Map<Username, User>) event ->
                    users |> Map.add event.user (fold (users.TryFind event.user) event))
                Map.empty
            |> Task.map (fun map -> map.Values |> Seq.toList)


        member this.GetByAuthId(authId: string) : User option Task =
            (this :> UserStore).GetAll()
            |> Task.map (Seq.tryFind (fun u -> u.authId = authId))

        member this.Query(query: string) : User list Task =
            let isMatch (v: string) =
                v.ToLowerInvariant().Contains(query.ToLowerInvariant())

            (this :> UserStore).GetAll()
            |> Task.map (
                List.filter (fun user ->
                    isMatch user.username.value
                    || user.firstName |> isMatch
                    || user.lastName |> isMatch)
            )


        member this.ApplyEvent(event: UserEvent) : Result<UserEvent, string> Task =

            writeEvent event CancellationToken.None
