const express = require('express');
const cors = require('cors');
const { Pool } = require('pg');
const app = express();
const PORT = 5001;

app.use(cors());
app.use(express.json());

// Docker Compose ortam değişkenlerinden bilgileri alıyoruz
const pool = new Pool({
    host: process.env.DB_HOST,
    user: process.env.DB_USER,
    password: process.env.DB_PASSWORD,
    database: process.env.DB_NAME,
    port: process.env.DB_PORT,
});

app.get('/api/urunler', async (req, res) => {
    try {
        console.log("Postgres'ten ürün listesi talep ediliyor...");
        const result = await pool.query('SELECT * FROM products ORDER BY id ASC');
        res.json(result.rows);
    } catch (err) {
        console.error(err);
        res.status(500).send("Veritabanı hatası!");
    }
});

app.listen(PORT, () => {
    console.log(`Ürün Mikroservisi veritabanı bağlantısı ile ayakta! 🚀`);
});