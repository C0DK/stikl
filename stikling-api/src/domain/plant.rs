use std::collections::HashSet;
// TOOD: Remove this when we use all PlantKind

#[derive(Debug, PartialEq, Eq, Hash)]
pub enum PlantKind {
    Shrub,
    FloweringPlant,
    Tree,
    Herb,
    Perennials,
    Climbers,
    Annuals,
}

#[derive(Debug, PartialEq, Eq)]
#[readonly::make]
pub struct Plant<'a> {
    pub name: &'a str,
    pub scientific_name: &'a str,
    pub kind: HashSet<PlantKind>,
}

#[cfg(test)]
mod tests {
    use super::PlantKind::*;
    use super::*;

    #[test]
    fn test_equal() {
        assert_eq!(
            Plant {
                name: "Lavender",
                scientific_name: "Lavandula",
                kind: HashSet::from([Shrub]),
            },
            Plant {
                name: "Lavender",
                scientific_name: "Lavandula",
                kind: HashSet::from([Shrub]),
            }
        );
    }

    #[test]
    fn test_not_equal_ie_name_differ() {
        assert_ne!(
            Plant {
                name: "Camille",
                scientific_name: "Lavandula",
                kind: HashSet::from([Shrub]),
            },
            Plant {
                name: "Lavender",
                scientific_name: "Lavandula",
                kind: HashSet::from([Shrub]),
            }
        );
    }
}
