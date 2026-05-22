<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

// Son birleştirilmiş veriyi tutacağımız reaktif dizi
const harmanlanmisUrunler = ref([])
const yukleniyor = ref(true)
const hataMesaji = ref('')

onMounted(async () => {
  try {
    // 🚀 Mikroservis senfonisi başlıyor: İki bağımsız servise AYNI ANDA istek atıyoruz
    const [urunlerResponse, stoklarResponse] = await Promise.all([
      axios.get('http://localhost:5001/api/urunler'), // Ürün Mikroservisi
      axios.get('http://localhost:5002/api/stoklar')  // Stok Mikroservisi
    ])

    const gelenUrunler = urunlerResponse.data
    const gelenStoklar = stoklarResponse.data

    // Verileri ID'lerine göre harmanlıyoruz (Merge işlemi)
    harmanlanmisUrunler.value = gelenUrunler.map(urun => {
      return {
        ...urun,
        // Eğer stok servisinde bu ID'ye ait bir bilgi varsa onu al, yoksa 0 kabul et
        stokMiktari: gelenStoklar[urun.id] !== undefined ? gelenStoklar[urun.id] : 0
      }
    })

  } catch (error) {
    console.error('Mikroservislerden veri çekilirken hata oluştu:', error)
    hataMesaji.value = 'Mikroservis köprüleri kurulamadı! Servisleri kontrol edin.'
  } finally {
    yukleniyor.value = false
  }
})
</script>

<template>
  <div class="dashboard">
    <h1>Tam Kapsamlı Mikroservis Kontrol Paneli 📊</h1>
    <p class="subtitle">Aşağıdaki veriler Docker üzerinde çalışan iki farklı bağımsız servisten alınıp ön yüzde birleştirilmiştir.</p>

    <!-- Yükleniyor Durumu -->
    <div v-if="yukleniyor" class="loading">Mikroservis verileri harmanlanıyor...</div>

    <!-- Hata Durumu -->
    <div v-else-if="hataMesaji" class="error-box">{{ hataMesaji }}</div>

    <!-- Harmanlanmış Veri Listesi -->
    <div v-else class="grid">
      <div v-for="urun in harmanlanmisUrunler" :key="urun.id" class="card">
        <div class="icon">💻</div>
        <h3>{{ urun.ad }}</h3>
        <p class="price">{{ urun.fiyat }} TL</p>
        
        <!-- Stok Durumuna Göre Dinamik Badge Gösterimi -->
        <div class="stock-status">
          <span v-if="urun.stokMiktari > 20" class="badge success">Stok Güvende ({{ urun.stokMiktari }} Adet)</span>
          <span v-else-if="urun.stokMiktari > 0" class="badge warning">Kritik Stok ({{ urun.stokMiktari }} Adet)</span>
          <span v-else class="badge danger">Stok Tükenmiş</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  max-width: 1000px;
  margin: 40px auto;
  padding: 0 20px;
  text-align: center;
}
h1 { color: #2c3e50; margin-bottom: 5px; }
.subtitle { color: #7f8c8d; margin-bottom: 40px; }

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 25px;
}
.card {
  background: white;
  border-radius: 16px;
  padding: 30px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.06);
  border: 1px solid #e2e8f0;
  transition: all 0.3s ease;
}
.card:hover { transform: translateY(-5px); box-shadow: 0 12px 25px rgba(0,0,0,0.1); }
.icon { font-size: 2.5rem; margin-bottom: 15px; }
h3 { margin: 10px 0; color: #2c3e50; font-size: 1.3rem; }
.price { font-weight: bold; color: #2c3e50; font-size: 1.4rem; margin-bottom: 15px; }

.stock-status { margin-top: 15px; }
.badge {
  padding: 6px 12px;
  border-radius: 10px;
  font-size: 0.85rem;
  font-weight: 600;
  display: inline-block;
}
.badge.success { background-color: #e6f4ea; color: #137333; }
.badge.warning { background-color: #fef7e0; color: #b06000; }
.badge.danger { background-color: #fce8e6; color: #c5221f; }

.loading { color: #42b883; font-size: 1.3rem; font-weight: bold; }
.error-box { background: #fee2e2; color: #dc2626; padding: 15px; border-radius: 8px; }
</style>