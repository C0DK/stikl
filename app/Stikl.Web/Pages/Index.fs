module Stikl.Web.Pages.Index

open Stikl.Web.Components
open Stikl.Web.Pages

let render (layout: Layout.Builder) =
    let highlight = ThemeGradiantSpan.render

    let title =
        Common.SectionHeader
            $"""Find {highlight "planter"} til dit {highlight "hjem"}<br>Find {highlight "hjem"} til dine {highlight "planter"}"""

    layout.render (title + Search.Form.render)
