module System.DateTimeOffset

open System


let formatRelative (t: DateTimeOffset) =
    let delta = DateTimeOffset.UtcNow.Subtract(t)
    let round (v: float) = Math.Round(v,0 ) |> int
    match delta with
    // Todo localize
    | d when d.TotalMinutes < 2 -> "a few seconds ago"
    | d when d.TotalMinutes < 61 -> $"{round d.TotalMinutes} minutes ago"
    | d when d.TotalHours < 48 -> $"{round d.TotalHours} hours ago"
    // TODO: make this more correct
    | d when d.TotalDays < 7 -> $"{round d.TotalDays} days ago"
    // TODO correct timezone
    | _ -> t.ToString("s")
    
