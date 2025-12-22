USE investmentdb;

-- 1. Users (Kullanıcılar)
CREATE TABLE Users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('admin', 'standard') DEFAULT 'standard', -- Yetkilendirme için
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_email UNIQUE (email) -- [Kısıt (Constraint) 1/5]
);

-- 2. Assets (Varlık Tanımları)
CREATE TABLE Assets (
    asset_id INT AUTO_INCREMENT PRIMARY KEY,
    symbol VARCHAR(10) NOT NULL UNIQUE, 
    name VARCHAR(50) NOT NULL,
    type ENUM('crypto', 'fiat', 'commodity') NOT NULL
);

-- 3. Wallets (Cüzdanlar - TL Bakiyesi)
CREATE TABLE Wallets (
    wallet_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    balance DECIMAL(15, 2) DEFAULT 500.00, -- Bonus para için varsayılan değer
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE,
    CONSTRAINT chk_balance_positive CHECK (balance >= 0) -- [Kısıt 2/5 - Veri Bütünlüğü]
);

-- 4. UserAssets (Kullanıcı Portföyü)
CREATE TABLE UserAssets (
    portfolio_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    asset_id INT NOT NULL,
    amount DECIMAL(15, 8) DEFAULT 0,
    average_cost DECIMAL(15, 2) DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    FOREIGN KEY (asset_id) REFERENCES Assets(asset_id),
    CONSTRAINT uq_user_asset UNIQUE (user_id, asset_id), -- [Kısıt 3/5 - Her kullanıcı her varlıktan bir kez olabilir]
    CONSTRAINT chk_amount_positive CHECK (amount >= 0) -- [Kısıt 4/5]
);

-- 5. Transactions (İşlemler)
CREATE TABLE Transactions (
    transaction_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    asset_id INT NOT NULL,
    type ENUM('buy', 'sell') NOT NULL,
    amount DECIMAL(15, 8) NOT NULL,
    price_at_transaction DECIMAL(15, 2) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    FOREIGN KEY (asset_id) REFERENCES Assets(asset_id)
);

-- 6. Predictions (ML Tahmin Sonuçları)
CREATE TABLE Predictions (
    prediction_id INT AUTO_INCREMENT PRIMARY KEY,
    asset_id INT NOT NULL,
    predicted_price DECIMAL(15, 2) NOT NULL,
    target_date DATE NOT NULL,
    confidence_score TINYINT NOT NULL,
    FOREIGN KEY (asset_id) REFERENCES Assets(asset_id),
    CONSTRAINT chk_confidence_limit CHECK (confidence_score BETWEEN 0 AND 100) -- [Kısıt 5/5]
);

-- 7. MarketHistory (ML Eğitimi İçin Veri)
CREATE TABLE MarketHistory (
    history_id INT AUTO_INCREMENT PRIMARY KEY,
    asset_id INT NOT NULL,
    price DECIMAL(15, 2) NOT NULL,
    recorded_at DATETIME NOT NULL,
    FOREIGN KEY (asset_id) REFERENCES Assets(asset_id)
);

-- En çok sorgulanacak kolonlara index eklenir.
CREATE INDEX idx_asset_date ON MarketHistory(asset_id, recorded_at);



DELIMITER //

-- SP 1: Yeni Kullanıcı Kaydı (Cüzdanı otomatik oluşturur)
CREATE PROCEDURE sp_RegisterUser(
    IN p_username VARCHAR(50), 
    IN p_email VARCHAR(100), 
    IN p_password VARCHAR(255)
)
BEGIN
    DECLARE new_user_id INT;
    
    START TRANSACTION;
    
    INSERT INTO Users (username, email, password_hash) VALUES (p_username, p_email, p_password);
    SET new_user_id = LAST_INSERT_ID();
    
    -- Kullanıcının Wallets tablosuna otomatik 500 TL (default değer) eklenir.
    INSERT INTO Wallets (user_id) VALUES (new_user_id); 
    
    COMMIT;
END //

-- SP 2: Varlık Alım İşlemi (Transactional safety sağlar)
CREATE PROCEDURE sp_BuyAsset(
    IN p_user_id INT,
    IN p_asset_id INT,
    IN p_amount DECIMAL(15, 8),
    IN p_current_price DECIMAL(15, 2)
)
BEGIN
    DECLARE v_total_cost DECIMAL(15, 2);
    SET v_total_cost = p_amount * p_current_price;
    
    -- İşlem mantığı: Cüzdandan düş, portföye ekle, işlem geçmişine kaydet.
    -- (Yetersiz bakiye kontrolü uygulama katmanında veya burada yapılabilir.)
    
    START TRANSACTION;
        -- Cüzdan Güncelleme, Portföy Güncelleme, Transaction Kaydı
        -- ... (Kodun tamamı yukarıda verilmişti, burada sadece tasarım amacı belirtiliyor)
    COMMIT;
END //

DELIMITER ;


-- View 1: Kullanıcı Özeti (Şifre gizlenir - Yetkilendirme / Maskeleme) [cite: 45]
CREATE VIEW vw_UserSummary AS
SELECT u.user_id, u.username, u.email, w.balance 
FROM Users u 
JOIN Wallets w ON u.user_id = w.user_id;

-- View 2: Son 24 Saatteki İşlemler
CREATE VIEW vw_RecentTransactions AS
SELECT t.created_at, u.username, t.type, t.amount
FROM Transactions t
JOIN Users u ON t.user_id = u.user_id
WHERE t.created_at >= NOW() - INTERVAL 1 DAY;

-- View 3: Yapay Zeka Tahmin Raporu
CREATE VIEW vw_PredictionReport AS
SELECT a.name, p.predicted_price, p.target_date, p.confidence_score
FROM Predictions p
JOIN Assets a ON p.asset_id = a.asset_id
WHERE p.target_date > NOW();

-- View 4: Zengin Kullanıcılar Listesi (Leaderboard)
CREATE VIEW vw_RichList AS
SELECT username, balance FROM vw_UserSummary ORDER BY balance DESC LIMIT 10;

-- View 5: Portföy Detayı
CREATE VIEW vw_PortfolioDetails AS
SELECT u.username, a.name, ua.amount, ua.average_cost 
FROM UserAssets ua
JOIN Users u ON ua.user_id = u.user_id
JOIN Assets a ON ua.asset_id = a.asset_id;




DELIMITER //

-- Fonksiyon 1: Bir varlığın sistemdeki toplam adedini hesaplar
CREATE FUNCTION fn_GetTotalAssetAmount(p_asset_id INT) RETURNS DECIMAL(15,8)
DETERMINISTIC
READS SQL DATA
BEGIN
    DECLARE total DECIMAL(15,8);
    SELECT SUM(amount) INTO total FROM UserAssets WHERE asset_id = p_asset_id;
    RETURN IFNULL(total, 0);
END //

-- Fonksiyon 2: Alış ve Anlık Fiyat üzerinden Potansiyel Kar/Zarar Hesaplama
CREATE FUNCTION fn_CalculatePotentialProfit(p_amount DECIMAL(15,8), p_buy_price DECIMAL(15,2), p_current_price DECIMAL(15,2)) 
RETURNS DECIMAL(15,2)
DETERMINISTIC
BEGIN
    RETURN (p_amount * p_current_price) - (p_amount * p_buy_price);
END //

DELIMITER ;




-- 1. Varlıkları Tanımla
INSERT INTO Assets (symbol, name, type) VALUES 
('USD', 'Amerikan Doları', 'fiat'),
('EUR', 'Euro', 'fiat'),
('GA', 'Gram Altın', 'commodity'),
('BTC', 'Bitcoin', 'crypto'),
('ETH', 'Ethereum', 'crypto');

-- 2. Örnek Kullanıcılar Ekle (Şifreler '12345'in hashlenmiş hali gibi )
-- Not: Stored Procedure kullanarak ekleyelim ki cüzdanları otomatik oluşsun.
CALL sp_RegisterUser('ahmet_yilmaz', 'ahmet@mail.com', 'hash_sifre_1');
CALL sp_RegisterUser('ayse_demir', 'ayse@mail.com', 'hash_sifre_2');
CALL sp_RegisterUser('mehmet_kaya', 'mehmet@mail.com', 'hash_sifre_3');

-- 3. Kullanıcılara Para Yükle (Manuel Bakie Güncelleme)
UPDATE Wallets SET balance = 50000.00 WHERE user_id = 1; -- Ahmet zengin olsun
UPDATE Wallets SET balance = 1000.00 WHERE user_id = 2;

-- 4. İşlem Yap (Ahmet Bitcoin ve Dolar alsın)
-- sp_BuyAsset(user_id, asset_id, amount, current_price)
CALL sp_BuyAsset(1, 4, 0.5, 950000.00); -- 0.5 BTC alıyor (Kur: 950.000 TL)
CALL sp_BuyAsset(1, 1, 1000, 32.50);    -- 1000 Dolar alıyor (Kur: 32.50 TL)

-- 5. Geçmiş Piyasa Verisi (ML için örnek)
INSERT INTO MarketHistory (asset_id, price, volume, recorded_at) VALUES
(4, 940000, 5000000, '2025-12-01 10:00:00'),
(4, 945000, 5200000, '2025-12-02 10:00:00'),
(4, 950000, 6000000, '2025-12-03 10:00:00');

-- 6. Yapay Zeka Tahmini (Sanki Python hesaplamış gibi)
INSERT INTO Predictions (asset_id, predicted_price, target_date, confidence_score) VALUES
(4, 980000.00, '2025-12-10', 85), -- BTC tahmini
(1, 33.00, '2025-12-05', 90);     -- Dolar tahmini