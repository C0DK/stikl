namespace Stikl.Web

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
          userDetails =
            { wants = "Søger efter"
              offers = "Tilbyder" } }

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
          userDetails =
            { wants = "Are looking for"
              offers = "Is offering" } }
    // TODO: inject
    let ``default`` = da
