<?php
header('Content-Type: application/json');
require 'config.php';

$username = $_POST['username'] ?? '';
$password = $_POST['password'] ?? '';

if ($username === '' || $password === '') {
    echo json_encode(["success" => false, "message" => "Missing fields"]);
    exit;
}

$hash = password_hash($password, PASSWORD_BCRYPT);

try {
    $stmt = $pdo->prepare("INSERT INTO accounts (username, password_hash) VALUES (:u, :p)");
    $stmt->execute([':u' => $username, ':p' => $hash]);
    echo json_encode(["success" => true]);
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Username already exists"]);
}
