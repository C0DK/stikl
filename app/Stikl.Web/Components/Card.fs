module Stikl.Web.Components.Card

let render (id: string) (img: {| src: string; alt: string |}) (title: string) (content: string) =
    $"""
<div
    id='{id}'
    class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow"
>
    <img
        alt="{img.alt}"
        class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
        src='{img.src}'
    />
    <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
    {content}
    </div>
    <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
    {title}
    </h4>
</div>
"""
