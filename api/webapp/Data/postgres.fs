module webapp.Data.postgres


open System
open System.Data.Common
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Npgsql
open FSharp.Control
open domain

type PostgresUserRepository(db: NpgsqlDataSource) =
    let serialize (payload: UserEventPayload) : string =
        JsonSerializer.Serialize<UserEventPayload> (payload)

    let deserialize (eventType : string) (payload: string) : UserEventPayload =
        // this probably maybe doesnt work
        JsonSerializer.Deserialize<UserEventPayload> (payload)
    let ReadUserEvents (readerT: DbDataReader Task) (cancellationToken : CancellationToken) : UserEvent TaskSeq =
        taskSeq {
            let! reader = readerT
            while! reader.ReadAsync(cancellationToken) do
                let username = reader.GetString(0)
                let timestamp = reader.GetDateTime(1)
                let eventType = reader.GetString(2)
                let payload = reader.GetString(3)
                
                yield {
                    user = Username username
                    timestamp = timestamp |> DateTimeOffset
                    payload = deserialize eventType payload
                }
            }
    let getEventsOfUser (username: Username) (cancellationToken : CancellationToken) : UserEvent TaskSeq =
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
            
    let getAllEvents  (cancellationToken : CancellationToken) : UserEvent TaskSeq =
            use connection = db.CreateConnection()
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

            ReadUserEvents (command.ExecuteReaderAsync()) cancellationToken
           
    
    let writeEvent (event: UserEvent) (cancellationToken : CancellationToken) : Result<UserEvent,string> Task =
            use connection = db.CreateConnection()
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
            command.Parameters.Add(NpgsqlParameter("@event_type", "test")) |> ignore
            command.Parameters.Add(NpgsqlParameter("@payload", serialize event.payload)) |> ignore
            
            command.ExecuteNonQueryAsync(cancellationToken) |> Task.map (fun _ -> Ok event)

    interface UserStore with
        member this.Get(username: Username) : User option Task =
            // TODO cancellationtoken?
            getEventsOfUser username CancellationToken.None
            // TODO: dont create empty user
            |> TaskSeq.fold (fun user event ->
                user
                |> Option.defaultValue (User.create username)
                |> apply event.payload
                |> Some
                )
                None 


        member this.GetAll() : User list Task =
            let events = getAllEvents (CancellationToken.None)
            
            events
            |> TaskSeq.fold (fun (users: Map<Username, User>) event ->
                users
                |> Map.add event.user (
                    users.TryFind event.user
                    |> Option.defaultValue (User.create event.user)
                    |> apply event.payload
                    )
                )
                Map.empty
            |> Task.map (fun map-> map.Values |> Seq.toList)
            

        member this.GetByAuthId(authId: string) : User option Task =
            (this :> UserStore).GetAll ()
            |> Task.map (Seq.tryFind (fun u -> u.authId = Some authId))
        member this.Query(query: string) : User list Task =
            let isMatch (v: string) = v.Contains query
            (this :> UserStore).GetAll ()
            |> Task.map (List.filter (fun user ->
                isMatch user.username.value
                || user.firstName |> Option.map isMatch |> Option.defaultValue false
                || user.fullName |> Option.map isMatch |> Option.defaultValue false)
                )


        member this.ApplyEvent(event: UserEvent) : Result<UserEvent, string> Task =
            
            writeEvent event CancellationToken.None
