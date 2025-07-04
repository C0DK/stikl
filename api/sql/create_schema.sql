CREATE TABLE user_events
(
    pk         SERIAL PRIMARY KEY,
    timestamp  TIMESTAMP WITH TIME ZONE NOT NULL,
    username   VARCHAR                  NOT NULL,
    event_type VARCHAR                  NOT NULL,
    payload    JSONB                    NOT NULL
);

CREATE INDEX user_events_by_username on user_events (username, timestamp asc);
