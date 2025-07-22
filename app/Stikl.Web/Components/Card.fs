module Stikl.Web.Components.Card

open Stikl.Web

let render (id: string) (img: {| src: string; alt: string |}) (title: string) (content: string) (href: string) =
    $"""
    <div
        id="{id}"
        class="w-64 flex flex-col"
    >
        <a href="{href}" class="contents">
            <img
                alt="{img.alt}"
                class="mx-4 hover:transform-105 shadow-lg aspect-square {Theme.rounding} rounded-b-none border-b-2 border-gray-800 object-cover"
                src='{img.src}'
            />
        </a>
        <div class="{Theme.rounding} h-16 shadow-lg border bg-amber-50">
            <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
                {content}
            </div>
            <a href="{href}">
                <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
                    {title}
                </h4>
            </a>
        </div>
    </div>
    """
