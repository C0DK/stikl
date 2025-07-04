module webapp.Data.postgres


open System
open System.Data.Common
open webapp
open System.Threading.Tasks
open Npgsql
open FSharp.Control
open domain

type PostgresUserRepository(db: NpgsqlDataSource) =

    let deserialize (eventType : string) (payload: string) : UserEventPayload =
        failwith "TODO"
    let ReadUserEvents (readerT: DbDataReader Task) : UserEvent TaskSeq =
        taskSeq {
            let! reader = readerT
            while! (reader.ReadAsync) do
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
    let getEventsOfUser (username: Username) : UserEvent TaskSeq =
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
                    WHERE username = @username
                    ORDER BY timestamp
                    """,
                    connection
                )

            command.Parameters.Add(NpgsqlParameter("@username", username.value)) |> ignore
            
            command.ExecuteReaderAsync()
            // TODO TaskSeq collectasync.
            |> ReadUserEvents
           
    

    interface UserStore with
        member this.Get(username: Username) : User option Task =
            getEventsOfUser username
            // TODO: dont create empty user
            |> TaskSeq.fold (fun user event ->
                user
                |> Option.defaultValue (User.create username)
                |> apply event.payload
                |> Some
                )
                None 

        
        member this.GetByAuthId(authId: string) : User option Task =
            failwith "TODO"

        member this.GetAll() : User list Task =
            failwith "TODO"

        member this.Query(query: string) : User list Task =
            let isMatch (v: string) = v.Contains query

            failwith "TODO"


        member this.ApplyEvent(event: UserEvent) : Result<UserEvent, string> Task =
            failwith "TODO"
