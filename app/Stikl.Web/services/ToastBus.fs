namespace Stikl.Web.services

open Microsoft.AspNetCore.Http
open domain

type ToastBus(contextAccessor: IHttpContextAccessor) =
    let mutable flushed = false
    member this.session = contextAccessor.HttpContext.Session

    member this.push(alert: Toast) =
        if flushed then
            failwith "Already flushed"

        Session.push this.session "toasts" alert

    member this.flush() =
        flushed <- true
        Session.readStack this.session "toasts"
