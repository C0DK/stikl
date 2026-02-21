namespace Stikl.Web.Model;

public record Species(
    SpeciesId Id,
    string CommonName,
    string ScientificName,
    string? Family,
    string? Genus,
    Uri? RegularImage,
    Uri? SmallImage
);
