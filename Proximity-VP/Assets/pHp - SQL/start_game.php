<?php
header('Content-Type: application/json');
require 'config.php';

$map_id        = $_POST['map_id']        ?? null;
$total_players = $_POST['total_players'] ?? 0;

$stmt = $pdo->prepare("
    INSERT INTO games (total_players, map_id)
    VALUES (:tp, :map)
");
$stmt->execute([
    ':tp'  => (int)$total_players,
    ':map' => $map_id ? (int)$map_id : null
]);

$game_id = (int)$pdo->lastInsertId();

echo json_encode(["success" => true, "game_id" => $game_id]);
