<?php
require 'config.php';

$users = [
    ['u' => 'player1', 'p' => '1234'],
    ['u' => 'player2', 'p' => '1234'],
    ['u' => 'player3', 'p' => '1234'],
    ['u' => 'player4', 'p' => '1234'],
];

foreach ($users as $user) {
    $hash = password_hash($user['p'], PASSWORD_BCRYPT);
    $stmt = $pdo->prepare("INSERT IGNORE INTO accounts (username, password_hash) VALUES (:u, :h)");
    $stmt->execute([':u' => $user['u'], ':h' => $hash]);
}

echo "Usuarios creados.";
