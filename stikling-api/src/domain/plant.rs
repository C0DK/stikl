use quickcheck::{Arbitrary, Gen};
use std::collections::HashSet;

#[derive(Debug, PartialEq, Eq, Hash, Clone)]
pub enum PlantKind {
    Shrub,
    FloweringPlant,
    Tree,
    Herb,
    Perennials,
    Climbers,
    Annuals,
}

#[derive(Debug, PartialEq, Eq, Clone)]
#[readonly::make]
pub struct Plant {
    pub name: String,
    pub scientific_name: String,
    pub kind: HashSet<PlantKind>,
}

impl Arbitrary for PlantKind {
    fn arbitrary(g: &mut Gen) -> Self {
        Gen::choose::<PlantKind>(
            g,
            &[
                PlantKind::Shrub,
                PlantKind::Climbers,
                PlantKind::Annuals,
                PlantKind::Perennials,
                PlantKind::FloweringPlant,
                PlantKind::Herb,
                PlantKind::Tree,
            ],
        )
        .unwrap()
        .clone()
    }
}
#[cfg(test)]
impl Arbitrary for Plant {
    fn arbitrary(g: &mut Gen) -> Self {
        Plant {
            name: Arbitrary::arbitrary(g),
            scientific_name: Arbitrary::arbitrary(g),
            kind: HashSet::<PlantKind>::arbitrary(g),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::PlantKind::*;
    use super::*;

    #[quickcheck]
    fn equality_on_self (x: Plant) -> bool{
            x == x
    }

    #[test]
    fn test_not_equal_ie_name_differ() {
        assert_ne!(Plant {
            name: "Camille".to_owned(),
            scientific_name: "Lavandula".to_owned(),
            kind: HashSet::from([Shrub]),
        }, Plant {
            name: "Lavender".to_owned(),
            scientific_name: "Lavandula".to_owned(),
            kind: HashSet::from([Shrub]),
        })
    }
}
