use serde::{Serialize};
use quickcheck::{Arbitrary, Gen};
use std::collections::HashSet;

#[derive(Debug, PartialEq, Eq, Hash, Clone, Serialize)]
pub enum PlantAttribute {
    Shrub,
    FloweringPlant,
    Herbaceous,
    Fruit,
    Berry,
    Tree,
    Herb,
    Perennials,
    Climbers,
    Annuals,
}

#[derive(Debug, PartialEq, Eq, Clone, Serialize)]
//#[readonly::make]
pub struct Plant {
    pub name: String,
    pub scientific_name: String,
    pub aliases: HashSet<String>,
    pub attributes: HashSet<PlantAttribute>,
    pub kind_of: Option<Box<Plant>>,
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

impl Plant {
    fn is_kind_of(&self, other: &Self) -> bool {
        self == other
            || match &self.kind_of {
                None => false,
                Some(plant) => plant.is_kind_of(other),
            }
    }
}
#[cfg(test)]
impl Arbitrary for Plant {
    fn arbitrary(g: &mut Gen) -> Self {
        Plant {
            name: Arbitrary::arbitrary(g),
            scientific_name: Arbitrary::arbitrary(g),
            aliases: HashSet::<String>::arbitrary(g),
            attributes: HashSet::<PlantAttribute>::arbitrary(g),
            kind_of: Option::<Box<Plant>>::arbitrary(g),
        }
    }
}

#[cfg(test)]
mod tests {
    mod kind {
        use super::super::*;

        #[quickcheck]
        fn on_identity(x: Plant) -> bool {
            x.is_kind_of(&x)
        }

        #[quickcheck]
        fn on_direct_kind(a: Plant, b: Plant) -> bool {
            let a = Plant {
                kind_of: Some(Box::from(b.clone())),
                ..a
            };

            a.is_kind_of(&b)
        }
        #[quickcheck]
        fn on_indirect_kind(a: Plant, b: Plant, c: Plant) -> bool {
            let some_box = |v| Some(Box::from(v));
            let a = Plant {
                kind_of: some_box(Plant {
                    kind_of: some_box(c.clone()),
                    ..b
                }),
                ..a
            };

            a.is_kind_of(&c)
        }
        #[quickcheck]
        fn inverse_not_true(a: Plant, b: Plant) -> bool {
            let a = Plant {
                kind_of: Some(Box::from(b.clone())),
                ..a
            };

            !b.is_kind_of(&a)
        }
    }

    mod equality {
        use super::super::PlantAttribute::*;
        use super::super::*;
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
                    aliases: HashSet::new(),
                    kind_of: None
                },
                Plant {
                    name: "Lavender".to_owned(),
                    scientific_name: "Lavandula".to_owned(),
                    attributes: HashSet::from([Shrub]),
                    aliases: HashSet::new(),
                    kind_of: None
                }
            )
        }
    }
}
