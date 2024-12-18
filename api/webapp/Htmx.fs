module webapp.Htmx

open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc


let page (content: string list) =
    let content = content |> String.concat "\n"

    $"""
    <!doctype html>
    <html lang="en">
    	<head>
    		<meta charset="utf-8" />
    		<meta name="viewport" content="width=device-width, initial-scale=1" />
			<script src="https://unpkg.com/htmx.org@2.0.4" integrity="sha384-HGfztofotfshcF7+8n44JQL2oJmowVChPTg48S+jvZoztPfvwD79OC/LTtG6dMp+" crossorigin="anonymous"></script>
			<title>Stikl.dk</title>
    	</head>
    	<body>
     		{content}
    	</body>
    </html>
"""


let block (tag: string) (content: string) = $"<{tag}>{content}</{tag}>"

type AContent = { href: string; label: string }

let a (content: AContent) =
    $"<a href='{content.href}'>{content.label}</a>"

let p = block "p"
let h1 = block "h1"
let h2 = block "h2"
let h3 = block "h3"
let h4 = block "h4"

let toResult (html:string) = Results.Text(html, "text/html",Encoding.UTF8, 200)
