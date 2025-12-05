<?php
$host   = "mysql-proximityvp.alwaysdata.net";
$dbname = "proximityvp_db";
$user   = "444795";
$pass   = "Proximity1234!";

try {
    $pdo = new PDO("mysql:host=$host;dbname=$dbname;charset=utf8mb4", $user, $pass);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode(["error" => "DB connection failed", "details" => $e->getMessage()]);
    exit;
}
?>
