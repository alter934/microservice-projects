-- Eğer önceden kaldıysa eski stock_db'yi tamamen temizle ve sıfırdan aç
DROP DATABASE IF EXISTS stock_db;
CREATE DATABASE stock_db;

-- 1. ÜRÜNLER VERİTABANI İŞLEMLERİ
-- Varsayılan gelen urun_db'ye bağlanıyoruz
\c product_db;

-- Mevcut tabloyu temizle ve kurumsal dilde products tablosunu aç
DROP TABLE IF EXISTS products;
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    ad VARCHAR(255) NOT NULL,
    fiyat INT NOT NULL
);

INSERT INTO products (ad, fiyat) VALUES 
('M4 Kablosuz Mouse', 1250),
('Mekanik Klavye', 3400),
('27 inç Oyuncu Monitörü', 8900);


-- 2. STOKLAR VERİTABANI İŞLEMLERİ
-- Yeni oluşturduğumuz stok_db veritabanına geçiş yapıyoruz
\c stock_db;

DROP TABLE IF EXISTS stocks;
CREATE TABLE stocks (
    urun_id INT PRIMARY KEY,
    stok_miktari INT NOT NULL
);

INSERT INTO stocks (urun_id, stok_miktari) VALUES 
(1, 15),
(2, 42),
(3, 0);