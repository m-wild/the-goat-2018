DROP TABLE IF EXISTS events CASCADE;
DROP TABLE IF EXISTS divisions CASCADE;
DROP TABLE IF EXISTS waves CASCADE;
DROP TABLE IF EXISTS entrants CASCADE;

CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE events (
    event_id   SMALLINT NOT NULL  PRIMARY KEY,
    event_name CITEXT   NOT NULL UNIQUE,
    raw_name   CITEXT   NOT NULL UNIQUE
);

INSERT INTO events (event_id, event_name, raw_name)
VALUES (1, 'The Goat Tongariro', 'GoatT'),
       (2, 'The Big Kid', 'BigKid');

CREATE TABLE divisions (
    division_id   SMALLSERIAL NOT NULL PRIMARY KEY,
    gender        CITEXT      NOT NULL,
    min_age       SMALLINT    NOT NULL,
    max_age       SMALLINT    NOT NULL,
    division_name CITEXT      NOT NULL UNIQUE
);

INSERT INTO divisions (gender, min_age, max_age, division_name)
VALUES ('m', 0, 22, 'Youngest Goat Men (U23)'),
       ('m', 23, 39, 'Open Goat Men (23-39)'),
       ('m', 40, 49, 'Wicked Goat Men (40-49)'),
       ('m', 50, 59, 'Nifty Goat Men (50-59)'),
       ('m', 60, 99, 'Goat Legends Men (60+)'),
       ('f', 0, 22, 'Youngest Goat Women (U23)'),
       ('f', 23, 39, 'Open Goat Women (23-39)'),
       ('f', 40, 49, 'Wicked Goat Women (40-49)'),
       ('f', 50, 59, 'Nifty Goat Women (50-59)'),
       ('f', 60, 99, 'Goat Legends Women (60+)');

CREATE TABLE waves (
    wave_id   SMALLINT NOT NULL PRIMARY KEY,
    wave_name CITEXT UNIQUE
);

INSERT INTO waves (wave_id, wave_name)
VALUES (1, 'Wave 1 8088hrs'),
       (2, 'Wave 2 0805hrs'),
       (3, 'Wave 3 0810hrs'),
       (4, 'Wave 4 0815hrs'),
       (5, 'Wave 5 0820hrs'),
       (6, 'Wave 6 0825hrs'),
       (7, 'Unknown');


CREATE TABLE entrants (
    bib             SMALLINT NOT NULL,
    year            SMALLINT NOT NULL,
    PRIMARY KEY (year, bib),

    first_name      CITEXT   NOT NULL,
    last_name       CITEXT   NOT NULL,
    event_id        SMALLINT NOT NULL REFERENCES events (event_id),
    wave_id         SMALLINT NOT NULL REFERENCES waves (wave_id),
    division_id     SMALLINT NULL REFERENCES divisions (division_id),
    completions     SMALLINT NOT NULL,
    finish_position SMALLINT NULL,
    finish_time     INTERVAL NULL
);

DROP VIEW IF EXISTS results;
CREATE VIEW results AS
    SELECT e.year,
           e.bib,
           e.first_name,
           e.last_name,
           (e.first_name || ' ' || e.last_name) :: CITEXT AS full_name,
           v.event_name,
           w.wave_id,
           w.wave_name,
           d.division_name,
           d.gender,
           d.min_age,
           d.max_age,
           e.completions,
           e.finish_position,
           e.finish_time
    FROM entrants e
             LEFT JOIN divisions d USING (division_id)
             LEFT JOIN events v USING (event_id)
             LEFT JOIN waves w USING (wave_id);

