module Stikl.Web.Pages.Index

open Stikl.Web.Pages

open Stikl.Web

let render (layout: Layout.Builder) =
    let stiklingerFrøOgPlanter =
        Components.Common.themeGradiantSpan "Stiklinger, frø og planter"
        
    let highlight = Components.Common.themeGradiantSpan

    let title = Components.Common.SectionHeader $"""Find {highlight "planter"} til dit {highlight "hjem"}<br>Find {highlight "hjem"} til dine {highlight "planter"}"""

    let callToAction =
        """
        <p class="mb-8 max-w-md text-center text-lg md:text-xl">
            Find gratis frø, planter og stiklinger i dit nærområde, og kom af med dine overskydende stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.
        </p>
        """

    layout.render (title + callToAction + Components.Search.Form.render)
