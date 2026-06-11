<template>
  <div class="home-container">
    <header class="home-header">
      <h1>🏭 MES Üretim Takip Sistemi</h1>
      <p class="subtitle">M4 Core Altyapısı ile Canlı Ürün ve Envanter İzleme Monitörü</p>
    </header>

    <div class="metrics-grid">
      <div class="metric-card product-count">
        <span class="icon">📦</span>
        <div class="info">
          <h3>Toplam Ürün Çeşidi</h3>
          <p class="value">{{ combinedProducts.length }}</p>
        </div>
      </div>
      <div class="metric-card stock-count">
        <span class="icon">🔢</span>
        <div class="info">
          <h3>Toplam Net Envanter</h3>
          <p class="value">{{ totalStockVolume }}</p>
        </div>
      </div>
    </div>

    <div class="table-card">
      <div class="table-header">
        <h2>📋 Aktif Ürün Tanımları ve Canlı Stok Durumu</h2>
        <button class="btn-refresh" @click="fetchCombinedData" :disabled="isLoading">
          {{ isLoading ? '🔄 Veriler Birleştiriliyor...' : '🔄 Anlık Veriyi Tazele' }}
        </button>
      </div>

      <div v-if="isLoading" class="loading-state">
        <p>⚡ .NET ProductApi ve StokApi verileri API Gateway üzerinden çekilip senkronize ediliyor...</p>
      </div>

      <table v-else class="mes-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Ürün Kodu</th>
            <th>Ürün Adı</th>
            <th>Ölçü Birimi</th>
            <th class="text-center">Canlı Stok Miktarı</th>
            <th>Teknik Açıklama</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="prod in combinedProducts" :key="prod.id">
            <td><span class="badge-id">#{{ prod.id }}</span></td>
            <td><strong class="code-text">{{ prod.urunKodu }}</strong></td>
            <td>{{ prod.urunAdi }}</td>
            <td><span class="badge-unit">{{ prod.birim || 'Adet' }}</span></td>
            <td class="text-center">
              <span :class="['badge-stock', getStockClass(prod.stokMiktari)]">
                {{ prod.stokMiktari }} {{ prod.birim || 'Adet' }}
              </span>
            </td>
            <td class="desc-text">{{ prod.aciklama || '---' }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button 
                @click="confirmDelete(prod.id, prod.urunAdi)" 
                class="text-red-600 hover:text-red-900 ml-4 font-semibold transition-colors duration-200"
              >
                🗑️ Sil
              </button>
            </td>
          </tr>
          <tr v-if="combinedProducts.length === 0">
            <td colspan="6" class="empty-state">Sistemde henüz tanımlı bir MES ürünü veya stok kaydı bulunamadı.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted , onUnmounted, onActivated, watch, inject} from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'


// Nginx API Gateway uç noktalarımız
const PRODUCT_API_URL = 'http://localhost:8000/api/urunler'
const STOCK_API_URL = 'http://localhost:8000/api/stoklar'



const combinedProducts = ref([])
const isLoading = ref(false)
const route = useRoute()

// 📉 Toplam envanter hacmini hesaplayan computed mülk
const totalStockVolume = computed(() => {
  return combinedProducts.value.reduce((sum, item) => sum + (item.stokMiktari || 0), 0)
})

const confirmDelete = async (id, urunAdi) => {
  // Operatör kazayla basmasın diye bir onay mekanizması
  const confirmAction = confirm(`"${urunAdi}" isimli ürünü ve tüm stok geçmişini silmek istediğinize emin misiniz?`);
  
  if (confirmAction) {
    try {
      console.log(`🗑️ Ürün silme isteği gönderiliyor... ID: ${id}`);
      const response = await axios.delete(`${PRODUCT_API_URL}/sil/${id}`);
      
      alert(response.data.message || 'Ürün başarıyla silindi!');
      
      // 🚀 KÜRESEL SİNYAL: Tabloyu anında güncellemesi için sinyal çakıyoruz!
      window.dispatchEvent(new CustomEvent('mes-data-updated'));
      
    } catch (error) {
      console.error('Silme işlemi sırasında hata:', error);
      alert(error.response?.data?.message || 'Silme işlemi başarısız oldu.');
    }
  }
}

// 🚀 API Composition: İki farklı servisin verisini çek ve hafızada birleştir
const fetchCombinedData = async () => {
  isLoading.value = true
  try {
    const [productRes, stockRes] = await Promise.all([
      axios.get(PRODUCT_API_URL),
      axios.get(STOCK_API_URL)
    ])

    const products = productRes.data
    const stocks = stockRes.data // 🚀 Doğrudan gelen sözlük nesnesini alıyoruz ({ "1": 43 })

    console.log("Gelen Ürünler:", products)
    console.log("Gelen Stok Sözlüğü:", stocks)

   // 🧠 Sayısal ve Metinsel Anahtarları Aynı Anda Eriten Nokta Atışı Birleştirme
    combinedProducts.value = products.map(product => {
      // ID alanının harf büyüklüğü ihtimaline karşı güvenli id tespiti
      const pId = product.id !== undefined ? product.id : product.Id
      
      // 🚀 BÜYÜK SİHİR: Sözlükten hem sayısal ID ile hem de metinsel ID ile okumayı deniyoruz
      let miktar = 0
      if (stocks[pId] !== undefined) {
        miktar = stocks[pId] // Eğer anahtar doğrudan sayı ise (1)
      } else if (stocks[String(pId)] !== undefined) {
        miktar = stocks[String(pId)] // Eğer anahtar metin ise ("1")
      }

      return {
        ...product,
        id: pId,
        stokMiktari: miktar
      }
    })

    console.log('✅ Dağıtık Veriler Başarıyla Birleştirildi:', combinedProducts.value)
  } catch (error) {
    console.error('❌ MES Verileri birleştirilirken hata oluştu:', error)
    alert('Canlı veriler çekilemedi.')
  } finally {
    isLoading.value = false
  }
}

// 🚀 KRİTİK DÜZELTME: HTML tarafının görebilmesi için en dış katmanda tanımlıyoruz
const getStockClass = (amount) => {
  if (amount <= 20) return 'stock-danger'   // Stok kritik seviyede (Kırmızı)
  if (amount <= 50) return 'stock-warning'  // Stok orta seviyede (Sarı)
  return 'stock-success'                    // Stok güvenli limanda (Yeşil)
}

// 🚀 2. BÜYÜK SİHİR: Sayfanın adresini (route) izlemeye alıyoruz. 
// Operatör yönetim ekranından her ana sayfaya döndüğünde adres değişeceği için 
// bu izleyici (watch) anında uyanır ve .NET servislerine taze istek atar!
watch(
  () => route.path,
  async (newPath) => {
    if (newPath === '/') { // Ana sayfa route yolun neyse ('/' veya '/home')
      console.log("🔄 Yönetim ekranından ana sayfaya dönüldü, envanter otomatik senkronize ediliyor...")
      await fetchCombinedData()
    }
  }
)

const handleMesUpdate = async () => {
  console.log("📥 Sinyal alındı! HomeView tablosu .NET servislerinden güncel veriyi çekiyor...")
  await fetchCombinedData()
}

// Sayfa her ekranda görünür olduğunda (aktifleştiğinde) tetiklenir
onActivated(() => {
  console.log("📺 MES Ekranı odağa alındı, veriler tazeleniyor...")
  fetchCombinedData()
})

onMounted(() => {
  fetchCombinedData()
  window.addEventListener('mes-data-updated', handleMesUpdate)
})

onUnmounted(() => {
  // 🧼 Temizlik: Bileşen yok olursa dinleyiciyi kaldırıyoruz
  window.removeEventListener('mes-data-updated', handleMesUpdate)
})


</script>

<style scoped>
.home-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
  font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
}
.home-header {
  margin-bottom: 30px;
  border-bottom: 3px solid #34495e;
  padding-bottom: 15px;
}
.home-header h1 {
  color: #2c3e50;
  margin: 0;
  font-size: 28px;
}
.subtitle {
  color: #7f8c8d;
  margin: 5px 0 0 0;
  font-size: 14px;
}
.metrics-grid {
  display: flex;
  gap: 20px;
  margin-bottom: 30px;
}
.metric-card {
  background: #ffffff;
  border-radius: 6px;
  padding: 15px;
  display: flex;
  align-items: center;
  box-shadow: 0 4px 6px rgba(0,0,0,0.05);
  flex: 1;
}
.product-count { border-left: 5px solid #3498db; }
.stock-count { border-left: 5px solid #2ecc71; }
.metric-card .icon {
  font-size: 32px;
  margin-right: 15px;
}
.metric-card h3 {
  margin: 0;
  font-size: 13px;
  color: #95a5a6;
  text-transform: uppercase;
}
.metric-card .value {
  margin: 2px 0 0 0;
  font-size: 24px;
  font-weight: bold;
  color: #2c3e50;
}
.table-card {
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.08);
  border: 1px solid #e2e8f0;
  overflow: hidden;
}
.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
}
.table-header h2 {
  font-size: 18px;
  color: #1e293b;
  margin: 0;
}
.btn-refresh {
  background: #2c3e50;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}
.btn-refresh:hover { background: #1a252f; }
.mes-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}
.mes-table th {
  background: #f1f5f9;
  color: #475569;
  padding: 12px 18px;
  font-weight: 600;
  font-size: 14px;
  border-bottom: 2px solid #cbd5e1;
}
.mes-table td {
  padding: 12px 18px;
  border-bottom: 1px solid #e2e8f0;
  color: #334155;
  font-size: 14px;
}
.mes-table tbody tr:hover { background: #f8fafc; }
.text-center { text-align: center; }
.badge-id {
  background: #e2e8f0;
  color: #475569;
  padding: 2px 6px;
  border-radius: 4px;
  font-family: monospace;
}
.code-text { color: #2563eb; }
.badge-unit {
  background: #e2e8f0;
  color: #334155;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
}
/* 🎨 CANLI STOK BADGE RENKLERİ */
.badge-stock {
  padding: 4px 12px;
  border-radius: 20px;
  font-weight: bold;
  font-size: 13px;
  display: inline-block;
}
.stock-success { background: #d4edda; color: #155724; }
.stock-warning { background: #fff3cd; color: #856404; }
.stock-danger { background: #f8d7da; color: #721c24; animation: pulse 2s infinite; }
.desc-text { color: #64748b; font-style: italic; }
.loading-state { text-align: center; padding: 40px; color: #2980b9; font-weight: bold; }
</style>