CREATE TABLE accounts (
    acc_id        INT AUTO_INCREMENT PRIMARY KEY,
    username      VARCHAR(32) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    wins          INT NOT NULL DEFAULT 0,
    games_played  INT NOT NULL DEFAULT 0,
    kills         INT NOT NULL DEFAULT 0,
    deaths        INT NOT NULL DEFAULT 0,
    fav_map_id    INT NULL,
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE maps (
    map_id    INT AUTO_INCREMENT PRIMARY KEY,
    map_name  VARCHAR(64) NOT NULL
);

CREATE TABLE games (
    game_id       INT AUTO_INCREMENT PRIMARY KEY,
    total_players INT NOT NULL,
    total_kills   INT NOT NULL DEFAULT 0,
    winner_acc_id INT NULL,
    map_id        INT NULL,
    start_time    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    end_time      DATETIME NULL,
    CONSTRAINT fk_games_winner
        FOREIGN KEY (winner_acc_id) REFERENCES accounts(acc_id),
    CONSTRAINT fk_games_map
        FOREIGN KEY (map_id) REFERENCES maps(map_id)
);

CREATE TABLE games_accounts (
    ga_id      INT AUTO_INCREMENT PRIMARY KEY,
    game_id    INT NOT NULL,
    acc_id     INT NOT NULL,
    kills      INT NOT NULL DEFAULT 0,
    deaths     INT NOT NULL DEFAULT 0,
    is_host    TINYINT(1) NOT NULL DEFAULT 0,
    is_hunter  TINYINT(1) NOT NULL DEFAULT 0,
    is_prey    TINYINT(1) NOT NULL DEFAULT 0,
    CONSTRAINT fk_ga_game
        FOREIGN KEY (game_id) REFERENCES games(game_id),
    CONSTRAINT fk_ga_acc
        FOREIGN KEY (acc_id)  REFERENCES accounts(acc_id)
);

CREATE TABLE deaths (
    death_id  INT AUTO_INCREMENT PRIMARY KEY,
    game_id   INT NOT NULL,
    acc_id    INT NULL,
    x_pos     FLOAT NOT NULL,
    y_pos     FLOAT NOT NULL,
    z_pos     FLOAT NOT NULL,
    death_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_deaths_game
        FOREIGN KEY (game_id) REFERENCES games(game_id),
    CONSTRAINT fk_deaths_acc
        FOREIGN KEY (acc_id) REFERENCES accounts(acc_id)
);
