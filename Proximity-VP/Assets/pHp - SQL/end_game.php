<?php
header('Content-Type: application/json');
require 'config.php';

$data = json_decode(file_get_contents('php://input'), true);

$game_id      = $data['game_id']      ?? null;
$winner_acc_id= $data['winner_acc_id']?? null;
$players      = $data['players']      ?? []; // array de {acc_id, kills, deaths, is_host}

if (!$game_id || empty($players)) {
    echo json_encode(["success" => false, "message" => "Missing data"]);
    exit;
}

$pdo->beginTransaction();

try {
    $total_kills = 0;
    foreach ($players as $p) {
        $total_kills += (int)$p['kills'];
        $stmt = $pdo->prepare("
            INSERT INTO games_accounts (game_id, acc_id, kills, deaths, is_host)
            VALUES (:g, :a, :k, :d, :h)
        ");
        $stmt->execute([
            ':g' => $game_id,
            ':a' => (int)$p['acc_id'],
            ':k' => (int)$p['kills'],
            ':d' => (int)$p['deaths'],
            ':h' => !empty($p['is_host']) ? 1 : 0
        ]);

        // actualizar stats globales
        $stmt2 = $pdo->prepare("
            UPDATE accounts
            SET games_played = games_played + 1,
                kills        = kills + :k,
                deaths       = deaths + :d,
                wins         = wins + :w
            WHERE acc_id = :a
        ");
        $stmt2->execute([
            ':k' => (int)$p['kills'],
            ':d' => (int)$p['deaths'],
            ':w' => ($winner_acc_id == $p['acc_id']) ? 1 : 0,
            ':a' => (int)$p['acc_id']
        ]);
    }

    $stmt3 = $pdo->prepare("
        UPDATE games
        SET total_kills = :tk,
            winner_acc_id = :w,
            end_time = NOW()
        WHERE game_id = :g
    ");
    $stmt3->execute([
        ':tk' => $total_kills,
        ':w'  => $winner_acc_id,
        ':g'  => $game_id
    ]);

    $pdo->commit();
    echo json_encode(["success" => true]);
} catch (Exception $e) {
    $pdo->rollBack();
    echo json_encode(["success" => false, "message" => "DB error"]);
}
