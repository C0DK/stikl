module Stikl.Web.Pages.Modal.AddSeeds

open Microsoft.AspNetCore.Antiforgery
open domain
open Stikl.Web


let render (plant: Plant) (antiForgeryToken: AntiforgeryTokenSet) =
    Result.Html.Ok(
        Components.Htmx.modal
            $"Tilføj{Components.Common.themeGradiantSpan plant.name}frø"
            $"""
                            <form
                                hx-post="/trigger/addSeeds/"
                                hx-target="#plant-{plant.id}"
                                _="on htmx:afterRequest trigger closeModal"
                                class="p-4"
                                >
                                <div class="mb-4">
                                    <label class="block text-gray-700 text-sm font-bold mb-2" for="username">
                                        Kommentar
                                    </label>
                                    <input type="hidden" name="{antiForgeryToken.FormFieldName}" value="{antiForgeryToken.RequestToken}"/>
                                    <input type="hidden" name="plantId" value="{plant.id}"/>
                                    <input
                                        class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
                                        id="comment"
                                        name="comment"
                                        type="text" placeholder="Kommentarer til potentielle interesserede">
                                </div>
                                <div class="mb-4">
                                    <label class="block text-gray-700 text-sm font-bold mb-2" for="seedKind">
                                        Type
                                    </label>
                                    <div class="relative">
                                        <select
                                            id="seedKind"
                                            name="seedKind"
                                            class="block appearance-none w-full bg-gray-200 border border-gray-200 text-gray-700 py-3 px-4 pr-8 rounded leading-tight focus:outline-none focus:bg-white focus:border-gray-500"
                                            >
                                            <option value="Seed">Frø</option>
                                            <option value="Seedling">frøplante</option>
                                            <option value="Cutting">Stikling</option>
                                            <option value="WholePlant">Komplet plante</option>
                                        </select>
                                        <div class="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2 text-gray-700">
                                            <i class="fa-solid fa-chevron-down"></i>
                                        </div>
                                    </div>
                                </div>
                                <button 
                                    type="submit" 
                                    class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-xs font-bold text-lime-600 transition hover:scale-105" 
                                    >Gem</button>
                            </form>
                            """
    )
