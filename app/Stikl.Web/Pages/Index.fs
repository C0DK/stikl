module webapp.Pages.Index

open webapp
open webapp.Components.Htmx

let render (pageBuilder: PageBuilder) =
    let stiklingerFrøOgPlanter =
        Components.Common.themeGradiantSpan "Stiklinger, frø og planter"

    let title = Components.Common.PageHeader $"Find {stiklingerFrøOgPlanter} nær dig"

    let callToAction =
        """
        <p class="mb-8 max-w-md text-center text-lg md:text-xl">
            Deltag i ET fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.
        </p>
        """

    pageBuilder.toPage (title + callToAction + Components.Search.Form.render)
