module webapp.Pages.NotFound

open webapp

let render title message =
    (Components.Common.PageHeader title)
    + $"""
        <p class="text-center text-lg md:text-xl">
        {message}
        </p>
        """
    + Components.Search.Form.render
