using Stikl.Web.Templates.Components;

namespace Stikl.Web.Routes;

public record Species(
    string CommonName,
    string ScientificName,
    string? Family,
    string? Genus,
    Uri? RegularImage,
    Uri? SmallImage
)
{
    public static PlantCard ToPlantCard(Species s) => s.ToPlantCard();

    public PlantCard ToPlantCard() =>
        new PlantCard(
            commonName: CommonName,
            scientificName: ScientificName,
            imageSource: SmallImage?.ToString()
                ?? "https://easydrawingguides.com/wp-content/uploads/2024/06/how-to-draw-a-plant-featured-image-1200.png",
            url: $"/plant/{CommonName.Replace(" ", "_")}", // TODO: better url
            WikiLink: "https://en.wikipedia.org/wiki/" + (ScientificName.Replace(" ", "_"))
        );
}
