use quickcheck::{Arbitrary, Gen};
use std::collections::HashSet;

#[derive(Debug, PartialEq, Eq, Hash, Clone)]
pub enum PlantAttribute {
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
    pub attributes: HashSet<PlantAttribute>,
}

impl Arbitrary for PlantAttribute {
    fn arbitrary(g: &mut Gen) -> Self {
        use PlantAttribute::*;
        Gen::choose::<PlantAttribute>(
            g,
            &[
                Shrub,
                Climbers,
                Annuals,
                Perennials,
                FloweringPlant,
                Herb,
                Tree,
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
            attributes: HashSet::<PlantAttribute>::arbitrary(g),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::PlantAttribute::*;
    use super::*;

    #[quickcheck]
    fn equality_on_self(x: Plant) -> bool {
        x == x
    }

    #[test]
    fn test_not_equal_ie_name_differ() {
        assert_ne!(
            Plant {
                name: "Camille".to_owned(),
                scientific_name: "Lavandula".to_owned(),
                attributes: HashSet::from([Shrub]),
            },
            Plant {
                name: "Lavender".to_owned(),
                scientific_name: "Lavandula".to_owned(),
                attributes: HashSet::from([Shrub]),
            }
        )
    }
}
