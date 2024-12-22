module webapp.Htmx

open System.Text
open Microsoft.AspNetCore.Http

open domain


type Block =
    { class_: string
      innerHtml: string
      tag: string }

let block (def: Block) =
    $"<{def.tag} class=\"{def.class_}\">{def.innerHtml}</{def.tag}>"

type AContent = { href: string; label: string }

let a (content: AContent) =
    $"<a href='{content.href}' class=\"hover:text-lime-00 cursor-pointer text-sm text-lime-600 underline\">{content.label}</a>"

let curryBlock tag class_ innerHtml =
    block
        { tag = tag
          class_ = class_
          innerHtml = innerHtml }

let p = curryBlock "p"

let h1 = curryBlock "h1"
let divWithClass = curryBlock "h1"

let PageHeader = h1 "font-sans text-3xl"

let head =
    """
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1" />
	<script src="https://unpkg.com/htmx.org@2.0.4" integrity="sha384-HGfztofotfshcF7+8n44JQL2oJmowVChPTg48S+jvZoztPfvwD79OC/LTtG6dMp+" crossorigin="anonymous"></script>
	<title>Stikl.dk</title>
	<script src="https://cdn.tailwindcss.com"></script>
</head>
"""

let toResult (html: string) =
    Results.Text(html, "text/html", Encoding.UTF8, 200)

let header =
    """
<header class="bg-lime-30 flex justify-between p-2">
	<a
		class="rounded-lg bg-gradient-to-br from-lime-600 to-amber-600 px-3 py-1 text-left font-sans text-xl font-semibold text-white hover:underline"
		href="/">Stikl.dk</a
	>
	<div class="flex justify-between gap-5">
		<a
			class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-sm font-bold text-lime-600 transition hover:scale-105"
			href="/login"
		>
			Log ind
		</a>
	</div>
</header>
"""

let page content =
    $"""
	<!doctype html>
    <html lang="en">
      {head}
      <body>
        <div class="container mx-auto flex min-h-screen flex-col">
		  {header}
          <main class="container mx-auto mt-10 flex flex-grow flex-col items-center space-y-8 p-2">
            {content}
          </main>
          <footer class="bg-lime flex w-full items-center justify-between p-4 text-slate-400">
            <p class="text-sm">© 2024 Stikling.io. All rights reserved.</p>
          </footer>
        </div>
      </body>
    </html>
"""
    |> toResult
