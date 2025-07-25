namespace Stikl.Web

open Microsoft.Extensions.DependencyInjection
open domain

type LocalizationUserDetails = { offers: string; wants: string }

type Localization =
    { username: string
      firstName: string
      lastName: string
      submit: string
      update: string
      updateProfile: string
      userDetails: LocalizationUserDetails
      logOut: string
      logIn: string
      authId: string
      areYouSure: string
      search: string
      hi: string -> string
      seeProfile: string
      location: string
      setLocation: string
      searchForLocation: string
      edit: string
      pick: string
      isNotUnique: string -> string
      required: string
      describeEvent: UserEventPayload -> string
      history: string }

module Localization =
    let da =
        { username = "Brugernavn"
          firstName = "Fornavn"
          lastName = "Efternavn"
          search = "Søg"
          submit = "Opret"
          update = "Opdatér"
          updateProfile = "Opdatér din profil"
          logOut = "Log ud"
          logIn = "Log ind"
          authId = "Login ID"
          hi = fun name -> $"Hejsa, {name}!"
          areYouSure = "Er du sikker?"
          seeProfile = "Se profil"
          history = "Historik"
          location = "Lokation"
          setLocation = "Vælg din lokation"
          searchForLocation = "Søg efter din lokation..."
          pick = "Vælg"
          edit = "Redigér"
          isNotUnique = fun username -> $"'{username}' findes allerede"
          required = "Skal udfyldes"
          userDetails =
            { wants = "Søger efter"
              offers = "Tilbyder" }
          describeEvent =
            fun (e: UserEventPayload) ->
                let rec describe e =
                    match e with
                    | CreateUser _ -> "Blev oprettet"
                    | AddedWant plant -> $"Ønsker sig {plant.name}"
                    | AddedSeeds plantOffer -> $"Tilbyder {plantOffer.plant.name}"
                    | RemovedWant plant -> $"Ønsker ikke længere {plant.name}"
                    | RemovedSeeds plant -> $"Tilbyder ikke længere {plant.name}"
                    | UpdateName(firstName, lastName) -> $"Opdateret navn til {firstName} {lastName}"
                    | SetDawaLocation dawaLocation -> $"Opdateret lokation til {dawaLocation.location.label}"
                    | MessageSent(_, receiver) -> $"Sendte besked til {receiver}"
                    | MessageReceived(_, sender) -> $"Modtog besked fra {sender}"

                describe e

        }

    let en =
        { username = "Username"
          firstName = "First Name"
          lastName = "Last Name"
          submit = "Submit"
          update = "Update"
          search = "Search"
          updateProfile = "Update your profile"
          logOut = "Log Out"
          logIn = "Log In"
          authId = "Auth ID"
          hi = fun name -> $"Hi, {name}!"
          areYouSure = "Are you sure?"
          seeProfile = "View profile"
          history = "History"
          location = "Location"
          setLocation = "Pick your location"
          searchForLocation = "Search for a location..."
          pick = "Pick"
          edit = "Edit"
          userDetails =
            { wants = "Are looking for"
              offers = "Is offering" }
          isNotUnique = fun username -> $"'{username}' is already taken"
          required = "Required"
          describeEvent =
            fun (e: UserEventPayload) ->
                let rec describe e =
                    match e with
                    | CreateUser _ -> "Was created"
                    | AddedWant plant -> $"Wants {plant.name}"
                    | AddedSeeds plantOffer -> $"Offers {plantOffer.plant.name}"
                    | RemovedWant plant -> $"No longer wants {plant.name}"
                    | RemovedSeeds plant -> $"No longer offers {plant.name}"
                    | UpdateName(firstName, lastName) -> $"Updated name to {firstName} {lastName}"
                    | SetDawaLocation dawaLocation -> $"Set location to {dawaLocation.location.label}"
                    | MessageSent(_, receiver) -> $"Sent message to {receiver}"
                    | MessageReceived(_, sender) -> $"Received message from {sender}"

                describe e }

    let ``default`` = da

    let register (s: IServiceCollection) =
        // TODO: base on request?
        s.AddScoped<Localization>(fun s -> ``default``)
