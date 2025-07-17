module Stikl.Web.Components.ThemeGradiantSpan

open Stikl.Web


let render innerHtml =
    // language=html
    $"""
    <span
        class="inline-block rounded-lg {Theme.themeBgGradient} bg-clip-text font-bold text-transparent hover:animate-pulse-size"
    >
        {innerHtml}
    </span>
    """

