// This file documents known issues discovered while writing integration tests.
//
// BUG: HasPlant is not registered as a [JsonDerivedType] on UserEventPayload.
//
//   UserEventPayload.cs has:
//     [JsonDerivedType(typeof(UserCreated), UserCreated.Kind)]
//     [JsonDerivedType(typeof(WantPlant),   WantPlant.Kind)]
//     [JsonDerivedType(typeof(UnwantPlant), UnwantPlant.Kind)]
//
//   HasPlant is missing. This means:
//     - UserEventPayload.Deserialize() will throw for any stored "has_plant" event.
//     - UserSource.Refresh() will crash if a user has ever used HasPlant.
//     - The unit test in UserEventPayloadSerializationTests.HasPlant_RoundTrip
//       will also fail at runtime.
//
//   Fix: add [JsonDerivedType(typeof(HasPlant), HasPlant.Kind)] to UserEventPayload.
//
// BUG: Missing comma in production SQL schema (app/sql/perenual_species.sql).
//
//   stikl.chat_event definition reads:
//     kind      TEXT NOT NULL
//     payload   TEXT NOT NULL   ← missing comma on the preceding line
//
//   The corrected DDL is used in IntegrationTestSetup.Schema.
