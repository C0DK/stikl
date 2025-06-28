module webapp.Pages.NotFound

open System.Threading.Tasks
open webapp

let render title message =
    Task.FromResult(
        (Components.PageHeader title)
        + $"""
        <p class="text-center text-lg md:text-xl">
        {message}
        </p>
        """
        + Components.search
    )
