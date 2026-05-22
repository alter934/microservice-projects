const express = require('express');
const cors = require('cors');
const { Pool } = require('pg');
const app = express();
const PORT = 5002;

app.use(cors());
app.use(express.json());

const pool = new Pool({
    host: process.env.DB_HOST,
    user: process.env.DB_USER,
    password: process.env.DB_PASSWORD,
    database: process.env.DB_NAME,
    port: process.env.DB_PORT,
});

app.get('/api/stoklar', async (req, res) => {
    try {
        console.log("Postgres'ten stok listesi talep ediliyor...");
        const result = await pool.query('SELECT * FROM stocks');
        
        // Vue ön yüzümüz nesne formatı beklediği için [{urun_id:1, stok_miktari:15}] verisini
        // {"1": 15} formatına indirgiyoruz (Dönüşüm)
        const stokMap = {};
        result.rows.forEach(row => {
            stokMap[row.urun_id] = row.stok_miktari;
        });
        
        res.json(stokMap);
    } catch (err) {
        console.error(err);
        res.status(500).send("Veritabanı hatası!");
    }
});

app.listen(PORT, () => {
    console.log(`Stok Mikroservisi veritabanı bağlantısı ile ayakta! 📦`);
});