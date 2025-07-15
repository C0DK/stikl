module Stikl.Web.Pages.NotFound

open Stikl.Web

let render title message =
    (Components.Common.SectionHeader title)
    + $"""
        <p class="text-center text-lg md:text-xl">
        {message}
        </p>
        """
    + Components.Search.Form.render
