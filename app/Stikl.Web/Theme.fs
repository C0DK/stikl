module Stikl.Web.Theme

let textBrandColor = "text-lime-600"
let textSecondaryColor = "text-amber-700"
let textMutedColor = "text-slate-400"
let buttonBorder = "rounded-lg border-2 border-lime-600"

let smButton =
    $"{buttonBorder} bg-white px-3 py-1 text-sm font-bold {textBrandColor} inline-block align-middle transition hover:scale-105 focus:bg-slate-200 active:bg-slate-400 active:inset-shadow-sm"
let submitButton =
    "rounded-lg shadow bg-lime-600 px-5 py-1 font-bold text-white inline-block align-middle transition hover:scale-105 hover:bg-lime-500 active:bg-lime-400 active:inset-shadow-sm"

let themeBgGradient = "bg-gradient-to-r from-lime-600 to-amber-600"
