<?php
header('Content-Type: application/json');
require 'config.php';

$username = $_POST['username'] ?? '';
$password = $_POST['password'] ?? '';

$stmt = $pdo->prepare("SELECT acc_id, password_hash FROM accounts WHERE username = :u");
$stmt->execute([':u' => $username]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);

if (!$row || !password_verify($password, $row['password_hash'])) {
    echo json_encode(["success" => false, "message" => "Invalid credentials"]);
    exit;
}

echo json_encode([
    "success" => true,
    "acc_id" => (int)$row['acc_id'],
    "username" => $username
]);
