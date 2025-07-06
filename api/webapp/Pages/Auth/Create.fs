module webapp.Pages.Auth.Create

open Microsoft.AspNetCore.Antiforgery

let textInput label id placeholder =
    $"""
    <div class="mb-4">
        <label class="block text-gray-700 text-sm font-bold mb-2" for="{id}">
            {label}
        </label>
        <input
            class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
            id="{id}"
            name="{id}"
            type="text" placeholder="{placeholder}">
    </div>
    """

let render (antiForgeryToken: AntiforgeryTokenSet) =

    $"""
    <h1 class="font-bold italic text-xl font-sans">
        Opret bruger
    </h1>
        <form
            hx-post="/auth/create"
            class="p-4"
            >
            <input type="hidden" name="{antiForgeryToken.FormFieldName}" value="{antiForgeryToken.RequestToken}"/>
            {textInput "Brugernavn" "username" "Indsæt et unikt brugernavn" }
            {textInput "Fornavn" "firstName" "Pippi" }
            {textInput "Efternavn" "lastName" "Langstrømpe" }
            <button 
                type="submit" 
                class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-xs font-bold text-lime-600 transition hover:scale-105" 
                >Opret</button>
        </form>
    """
