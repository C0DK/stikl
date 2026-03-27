-- One row per (species, language). Language-agnostic Wikidata fields (edible,
-- conservation_status, taxon_rank, gbif_taxon_id, parent_taxon_*) are stored
-- redundantly in every language row for simplicity.
CREATE TABLE wiki_species_info (
  perenual_id              INTEGER NOT NULL REFERENCES perenual_species(perenual_id),
  lang                     TEXT NOT NULL,           -- ISO 639-1: 'en', 'da', …

  -- Wikipedia (language-specific)
  wikipedia_title          TEXT NULL,
  wikipedia_page_url       TEXT NULL,               -- human-readable URL
  wikipedia_page_id        INTEGER NULL,
  wikidata_id              TEXT NULL,               -- Wikidata Q-ID, e.g. 'Q12345'
  description              TEXT NULL,               -- Wikipedia short description
  extract                  TEXT NULL,               -- Wikipedia intro paragraph

  -- Wikidata (language-specific)
  common_name              TEXT NULL,               -- P1843 taxon common name

  -- Wikidata (language-agnostic)
  edible                   BOOLEAN NULL,            -- derived from P279/P31 type hierarchy
  hardiness_zones          TEXT NULL,               -- extracted from article text
  conservation_status      TEXT NULL,               -- P141 IUCN label
  taxon_rank               TEXT NULL,               -- P105 e.g. 'species', 'genus'
  gbif_taxon_id            TEXT NULL,               -- P846
  parent_taxon_name        TEXT NULL,               -- P171 label
  parent_taxon_wikidata_id TEXT NULL,               -- P171 Q-ID

  scraped_at               TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),

  PRIMARY KEY (perenual_id, lang)
);
