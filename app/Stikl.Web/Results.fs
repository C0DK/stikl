module Stikl.Web.Results

open Microsoft.AspNetCore.Http


let HTML (content: string) =
    Results.Text(content, contentType = "text/html")
let executeAsync (context: HttpContext) (result: IResult) =
    result.ExecuteAsync context
