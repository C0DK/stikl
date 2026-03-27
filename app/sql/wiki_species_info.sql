CREATE TABLE wiki_species_info (
  perenual_id              INTEGER PRIMARY KEY NOT NULL REFERENCES perenual_species(perenual_id),
  wikipedia_title          TEXT NULL,
  wikipedia_page_id        INTEGER NULL,
  wikidata_id              TEXT NULL,
  description              TEXT NULL,
  extract                  TEXT NULL,
  edible                   BOOLEAN NULL,
  hardiness_zones          TEXT NULL,
  conservation_status      TEXT NULL,
  parent_taxon_name        TEXT NULL,
  parent_taxon_wikidata_id TEXT NULL,
  scraped_at               TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);
