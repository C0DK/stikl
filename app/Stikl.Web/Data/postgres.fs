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
open Serilog
open domain

// 1. Either create the serializer options from the F# options...
let jsonSerializerOptions =
    JsonFSharpOptions
        .Default()
        // Add any .WithXXX() calls here to customize the format
        .ToJsonSerializerOptions()

type PostgresUserRepository(db: NpgsqlDataSource, logger: ILogger) =
    let logger = logger.ForContext<PostgresUserRepository>()
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

    let writeEventOnConnection (connection: NpgsqlConnection) (transaction: NpgsqlTransaction) (cancellationToken: CancellationToken) (event: UserEvent) : Task =
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
                    connection,
                    transaction
                )

            command.Parameters.Add(NpgsqlParameter("@username", event.user.value)) |> ignore
            command.Parameters.Add(NpgsqlParameter("@timestamp", event.timestamp)) |> ignore
            command.Parameters.Add(NpgsqlParameter("@event_type", event.payload.kind)) |> ignore
            let mutable payloadParam = NpgsqlParameter("@payload", serialize event.payload)
            payloadParam.NpgsqlDbType <- NpgsqlDbType.Jsonb
            command.Parameters.Add(payloadParam) |> ignore

            command.ExecuteNonQueryAsync(cancellationToken)
            
    let writeEvents (events: UserEvent list) (cancellationToken: CancellationToken) : Task<Result<UserEvent list, string>>=
        // TODO: validate that we can / may
        task {
            use! connection = db.OpenConnectionAsync()
            use! transaction = connection.BeginTransactionAsync(cancellationToken)
            
            
            
            let write = writeEventOnConnection connection transaction cancellationToken
            
            try
                for event in events do
                    do! write event
            with
                | err ->
                    do! transaction.RollbackAsync()
                    logger.Error(err, "Could not write events")
                    // TODO: get this to return, when f# isn't a bitch
                    raise err
                    //return (Error "could not write events" : Result<UserEvent list, string>)
                    
            do! transaction.CommitAsync()
            return (Ok events: Result<UserEvent list, string>)

        }

    interface UserStore with
        member this.Get (username: Username) (cancellationToken: CancellationToken) : User option Task =
            getEventsOfUser username cancellationToken
            |> TaskSeq.fold
                // how to compose??
                (fun u e -> applyOnOptional e u |> Some)
                None


        member this.GetAll(cancellationToken: CancellationToken) : User list Task =
            let events = getAllEvents cancellationToken

            events
            |> TaskSeq.fold
                (fun (users: Map<Username, User>) event ->
                    users |> Map.add event.user (applyOnOptional event (users.TryFind event.user)))
                Map.empty
            |> Task.map (fun map -> map.Values |> Seq.toList)


        member this.GetByAuthId (authId: string) (cancellationToken: CancellationToken) : User option Task =
            (this :> UserStore).GetAll(cancellationToken)
            |> Task.map (Seq.tryFind (fun u -> u.authId = authId))

        member this.Query (query: string) (cancellationToken: CancellationToken) : User list Task =
            let isMatch (v: string) =
                v.ToLowerInvariant().Contains(query.ToLowerInvariant())

            (this :> UserStore).GetAll(cancellationToken)
            |> Task.map (
                List.filter (fun user ->
                    isMatch user.username.value
                    || user.firstName |> isMatch
                    || user.lastName |> isMatch)
            )


        member this.ApplyEvent
            (event: UserEvent)
            (cancellationToken: CancellationToken)
            : Result<UserEvent, string> Task =

            writeEvents [event] cancellationToken |> Task.map(Result.map Seq.head)
        member this.ApplyEvents
            (events: UserEvent list)
            (cancellationToken: CancellationToken)
            : Result<UserEvent list, string> Task =

            writeEvents events cancellationToken
