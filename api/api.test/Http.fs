module api.test.Http

open System
open System.IdentityModel.Tokens.Jwt
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Security.Claims
open System.Text
open Microsoft.IdentityModel.Tokens
open api

let get (path: string) (client: HttpClient) = client.GetAsync(path)

let getJson<'T> (path: string) (client: HttpClient) = client.GetFromJsonAsync<'T>(path)

let postJson<'T> (path: string) (payload: 'T) (client: HttpClient) =
    client.PostAsJsonAsync<'T>(path, payload)

let postEmpty<'T> (path: string) (client: HttpClient) = client.PostAsync(path, null)

let getToken (userId: domain.UserId) =
    let handler = JwtSecurityTokenHandler()

    let signingCredentials =
        SigningCredentials(Composition.Authentication.signingKey, SecurityAlgorithms.HmacSha256)

    handler.WriteToken(
        JwtSecurityToken(
            issuer = Composition.Authentication.validIssuers[0],
            audience = Composition.Authentication.validAudiences[0],
            claims = [ Claim(ClaimTypes.NameIdentifier, userId.value) ],
            expires = DateTime.Now.AddMinutes(30),
            signingCredentials = signingCredentials
        )
    )

let loginAs (userId: domain.UserId) (client: HttpClient) =
    let token = getToken userId
    client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", token)
