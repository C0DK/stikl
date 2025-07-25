module Result

open System.Text
open Microsoft.AspNetCore.Http


module Html =
    let Ok (html: string) =
        Results.Text(html, "text/html", Encoding.UTF8, 200)
