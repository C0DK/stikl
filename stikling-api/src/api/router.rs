use std::collections::HashSet;
use axum::{
    routing::{get},
    http::StatusCode,
    Json, Router,
};
use crate::domain::plant::PlantAttribute::{Berry, FloweringPlant, Fruit, Herbaceous, Perennials};
use super::super::domain::plant::*;

pub fn get_router()  -> Router{
    Router::new()
        .route("/", get(root))
        .route("/plants", get(plant_list))
}

// basic handler that responds with a static string
async fn root() -> &'static str {
    "Hello, World!"
}

async fn plant_list(
) -> (StatusCode, Json<Vec<Plant>>) {
    use PlantAttribute;

    let lavender = Plant {
        name: "Lavender".to_string(),
        scientific_name: "Lupinus".to_string(),
        aliases: HashSet::new(),
        attributes: HashSet::from([Perennials, FloweringPlant, Herbaceous]),
        kind_of: None,
    };

    let lavender_box = || Some(Box::from(lavender.clone()));

    let plants = Vec::from([
        Plant {
            name: "Lupine".to_string(),
            scientific_name: "Lupinus".to_string(),
            attributes: HashSet::from([Perennials, FloweringPlant, Herbaceous]),
            aliases: HashSet::new(),
            kind_of: None,
        },
        lavender.clone(),
        Plant {
            name: "English lavender".to_string(),
            scientific_name: "Lavandula angustifolia".to_string(),
            attributes: HashSet::from([Perennials, FloweringPlant, Herbaceous]),
            aliases: HashSet::from(["Common Lavender".to_string()]),
            kind_of: lavender_box()
        },
        Plant {
            name: "Spanish lavender".to_string(),
            scientific_name: "Lavandula stoechas".to_string(),
            attributes: HashSet::from([Perennials, FloweringPlant, Herbaceous]),
            aliases: HashSet::from(["Topped lavender".to_string(), "French lavender".to_string()]),
            kind_of: lavender_box()
        },
        Plant {
            name: "Strawberry".to_string(),
            scientific_name: "Lavender".to_string(),
            attributes: HashSet::from([Fruit, FloweringPlant, Berry]),
            aliases: HashSet::new(),
            kind_of: None,
        }
    ]);

    (StatusCode::OK, Json(plants))
}
