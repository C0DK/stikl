module Stikl.Web.Components.Common

let grid (content: string seq) =
    let innerHtml = content |> String.concat "\n"

    $"""
<div class="grid grid-cols-3 gap-4">
    {innerHtml}
</div>
"""


let SectionHeader content =
    $"""
    <h1 class="font-sans text-3xl mt-4 mb-2 fold-bold text-center md:text-left">
        {content}
    </h1>
    """
