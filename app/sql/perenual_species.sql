CREATE TABLE perenual_species (
  perenual_id INTEGER PRIMARY KEY NOT NULL, 
  common_name TEXT NOT NULL,
  scientific_name TEXT[] NOT NULL,
  other_name TEXT[] NOT NULL,
  family TEXT NULL,
  cultivar TEXT NULL,
  variety TEXT NULL,
  species_epithet TEXT NULL,
  genus TEXT NULL,
  subspecies TEXT NULL,
  img_regular_url TEXT NULL,
  img_small_url TEXT NULL,
  search_vector GENERATED ALWAYS AS (
    setweight(to_tsvector('english', coalesce(common_name,'')), 'A')
    || setweight(to_tsvector('english', coalesce(scientific_name[0], '')), 'B')
    || setweight(to_tsvector('english', coalesce(scientific_name[1], '')), 'B')
    || setweight(to_tsvector('english', coalesce(other_name[0], '')), 'B')
    || setweight(to_tsvector('english', coalesce(other_name[1], '')), 'B')
    || setweight(to_tsvector('english', coalesce(family,'')), 'C')
    || setweight(to_tsvector('english', coalesce(cultivar,'')), 'C')
    || setweight(to_tsvector('english', coalesce(genus,'')), 'C')
    || setweight(to_tsvector('english', coalesce(species_epithet,'')), 'C')
    || setweight(to_tsvector('english', coalesce(subspecies,'')), 'C')
) STORED;



CREATE TABLE signin_otp (
  email TEXT NOT NULL,
  code TEXT NOT NULL,
  created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

CREATE TABLE stikl.user_event (
  username TEXT NOT NULL,
  version INTEGER NOT NULL CONSTRAINT positive_version CHECK (version > 0),
  timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
  kind TEXT NOT NULL,
  payload JSONB NOT NULL,

  PRIMARY KEY (username, version)
  -- TODO case insensitive unique?
);

CREATE TABLE stikl.readmodel_user (
  username TEXT NOT NULL,
  email TEXT NOT NULL,
  version INTEGER NOT NULL CONSTRAINT positive_version CHECK (version > 0),
  payload JSONB NOT NULL,

  PRIMARY KEY (username)
);


CREATE TABLE stikl.chat_event (
  pk INTEGER PRIMARY KEY GENERATED ALWAYS AS IDENTITY NOT NULL,
  sender TEXT NOT NULL, 
  recipient TEXT NOT NULL, 
  timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
  kind TEXT NOT NULL
  payload TEXT NOT NULL
);
