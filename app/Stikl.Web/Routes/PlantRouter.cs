using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;
using Stikl.Web.Templates.Components;

namespace Stikl.Web.Routes;

public static class PlantRouter
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/{id}",
            async (
                HttpContext context,
                PlantSearcher searcher,
                NpgsqlDataSource db,
                CancellationToken cancellationToken,
                SpeciesId id
            ) =>
            {
                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var species = await GetSpecies(connection, id, cancellationToken);
                if (species is null)
                    return Results.NotFound();
                return new PartialResult(
                    CreatePlantCard(
                        await users.GetFromPrincipalOrDefault(context.User, cancellationToken),
                        species
                    )
                );
            }
        // TODO: on redirect do a toast!
        );
        app.MapGet(
            "/{id}/has",
            async (
                HttpContext context,
                NpgsqlDataSource db,
                CancellationToken cancellationToken,
                SpeciesId id
            ) =>
            {
                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var species = await GetSpecies(connection, id, cancellationToken);
                if (species is null)
                    return Results.NotFound();

                // TODO create a ModalResult?
                return new ModalResult(
                    $"Add {species.CommonName}",
                    new HasPlantForm(
                        speciesId: species.Id,
                        comment: null,
                        typeOptions: Enum.GetValues<PlantOfferType>()
                            .Select(v => $"<option value='{v}'>{v}</option>")
                    )
                );
            }
        );
        app.MapPost(
                "/{id}/has",
                async (
                    HttpContext context,
                    ToastHandler toast,
                    NpgsqlDataSource db,
                    CancellationToken cancellationToken,
                    SpeciesId id
                ) =>
                {
                    await using var connection = await db.OpenConnectionAsync(cancellationToken);
                    var species = await GetSpecies(connection, id, cancellationToken);
                    if (species is null)
                        return Results.NotFound();

                    var form = context.Request.Form;

                    var comment = form.GetString("comment")?.Trim();
                    if (string.IsNullOrWhiteSpace(comment))
                        comment = null;

                    if (!Enum.TryParse<PlantOfferType>(form.GetString("type"), out var type))
                        return new ModalResult(
                            $"Add {species.CommonName}",
                            new HasPlantForm(
                                speciesId: species.Id,
                                comment: comment,
                                typeOptions: Enum.GetValues<PlantOfferType>()
                                    .Select(v => $"<option value='{v}'>{v}</option>")
                            )
                        );

                    var writer = new UserEventWriter(connection);

                    // TODO: redirect to signup if no existo?
                    var username = context.User.GetUsername();
                    var user = await writer.Write(
                        username,
                        new HasPlant(id, type, comment),
                        cancellationToken
                    );
                    toast.Add(
                        $"Added {species.CommonName} to your inventory",
                        $"Others users can now see that you have '{species.CommonName}' - and they can request it if they want it"
                    );

                    // TODO: ensure modal is hidden!
                    return new PartialResult(
                        CreatePlantCard(user, species),
                        headers: new Dictionary<string, string>()
                        {
                            ["HX-Trigger-After-Swap"] = "closeModal",
                        }
                    );
                }
            )
            .RequireAuthorization(); // require signup to be done!

        app.MapPost(
                "/{id}/unhas",
                async (
                    HttpContext context,
                    PlantSearcher searcher,
                    NpgsqlDataSource db,
                    ToastHandler toast,
                    CancellationToken cancellationToken,
                    SpeciesId id
                ) =>
                {
                    await using var connection = await db.OpenConnectionAsync(cancellationToken);
                    var species = await GetSpecies(connection, id, cancellationToken);
                    if (species is null)
                        return Results.NotFound();
                    var writer = new UserEventWriter(connection);

                    // TODO: redirect to signup if no existo?
                    var username = context.User.GetUsername();
                    var user = await writer.Write(
                        username,
                        new NoLongerHasPlant(id),
                        cancellationToken
                    );

                    toast.Add(
                        $"{species.CommonName} removed from your inventory",
                        $"{species.CommonName} is no longer on the list of plants that you have."
                    );
                    return new PartialResult(CreatePlantCard(user, species));
                }
            )
            .RequireAuthorization(); // require signup to be done!
        app.MapPost(
                "/{id}/want",
                async (
                    HttpContext context,
                    PlantSearcher searcher,
                    ToastHandler toast,
                    NpgsqlDataSource db,
                    CancellationToken cancellationToken,
                    SpeciesId id
                ) =>
                {
                    await using var connection = await db.OpenConnectionAsync(cancellationToken);
                    var species = await GetSpecies(connection, id, cancellationToken);
                    if (species is null)
                        return Results.NotFound();

                    var writer = new UserEventWriter(connection);

                    // TODO: redirect to signup if no existo?
                    var username = context.User.GetUsername();
                    var user = await writer.Write(username, new WantPlant(id), cancellationToken);
                    toast.Add(
                        "Marked as wanted",
                        $"{species.CommonName} is now marked as wanted, and we'll notify you if we find one near you!"
                    );

                    return new PartialResult(CreatePlantCard(user, species));
                }
            )
            .RequireAuthorization(); // require signup to be done!
        app.MapPost(
                "/{id}/unwant",
                async (
                    HttpContext context,
                    PlantSearcher searcher,
                    NpgsqlDataSource db,
                    ToastHandler toast,
                    CancellationToken cancellationToken,
                    SpeciesId id
                ) =>
                {
                    await using var connection = await db.OpenConnectionAsync(cancellationToken);
                    var species = await GetSpecies(connection, id, cancellationToken);
                    if (species is null)
                        return Results.NotFound();
                    var writer = new UserEventWriter(connection);

                    // TODO: redirect to signup if no existo?
                    var username = context.User.GetUsername();
                    var user = await writer.Write(username, new UnwantPlant(id), cancellationToken);

                    toast.Add(
                        "No longer marked as wanted",
                        $"{species.CommonName} is no longer marked as wanted, and we'll not notify you if we find one near you."
                    );
                    return new PartialResult(CreatePlantCard(user, species));
                }
            )
            .RequireAuthorization(); // require signup to be done!
    }

    // TODO: move to plant source.
    public static async ValueTask<Species?> GetSpecies(
        NpgsqlConnection connection,
        SpeciesId id,
        CancellationToken cancellationToken
    )
    {
        using var command = new NpgsqlCommand(
            @"
SELECT
  p.perenual_id,
  p.common_name,
  p.scientific_name,
  p.family,
  p.genus,
  w.wikipedia_page_url,
  w.description
FROM perenual_species p
LEFT JOIN wiki_species_info w ON p.perenual_id = w.perenual_id AND w.lang = 'en'
WHERE p.perenual_id = $1
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(id) },
        };

        return await command
            .ReadAllAsync(
                reader => new Species(
                    Id: new SpeciesId(reader.GetFieldValue<int>(0)),
                    CommonName: reader.GetFieldValue<string>(1),
                    ScientificName: string.Join(" ", reader.GetFieldValue<string[]>(2)),
                    Family: reader.GetStringOrNull(3),
                    Genus: reader.GetStringOrNull(4),
                    WikiPageUrl: reader.GetStringOrNull(5),
                    WikiDescription: reader.GetStringOrNull(6)
                ),
                cancellationToken
            )
            .SingleOrDefaultAsync();
    }

    public static PlantCard CreatePlantCard(User? viewer, Species species)
    {
        var url = $"/plant/{species.Id}";
        return new PlantCard(
            wantButton: viewer?.DoesWant(species.Id) is true
                ? new PlantCardUnWantButton(url)
                : new PlantCardWantButton(url),
            commonName: species.CommonName,
            scientificName: species.ScientificName,
            has: viewer?.DoesHas(species.Id) is true,
            id: species.Id,
            url: url,
            WikiLink: species.WikiPageUrl ?? "",
            hasWikiLink: species.WikiPageUrl is not null,
            description: species.WikiDescription ?? "",
            hasDescription: species.WikiDescription is not null
        );
    }
}
