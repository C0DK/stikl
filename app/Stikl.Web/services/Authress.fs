module Stikl.Web.services.Authress

open System
open System.Security.Cryptography
open System.Text

let base64UrlEncode (data: byte array) =
    Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_").TrimEnd('=')

let getChallenge (size: int) =
    // default 32
    use rng = RandomNumberGenerator.Create()
    let randomBytes = Array.zeroCreate size
    rng.GetBytes(randomBytes)
    let verifier = base64UrlEncode (randomBytes)

    let buffer = Encoding.UTF8.GetBytes(verifier)
    let hash = SHA256.Create().ComputeHash(buffer)
    let challenge = base64UrlEncode (hash)

    (challenge, verifier)

let getLoginUrl (loginUrl: string) (clientId: string) (redirectUrl: string) =
    let (challenge, verifier) = getChallenge 32
    $"https://{loginUrl}/?code_challenge_method=S256&response_mode&redirect_uri={redirectUrl}&code_challenge={challenge}&client_id={clientId}"
