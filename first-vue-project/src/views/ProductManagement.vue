<template>
  <div class="mes-container">
    <h2 class="mes-title">⚙️ MES Üretim Yönetimi - Ürün Tanımları</h2>
    
    <div class="mes-card selection-card">
      <label for="product-select">Düzenlenecek Ürünü Seçin:</label>
      <select id="product-select" v-model="selectedProductId" @change="loadProductDetails" :disabled="isLoadingStock">
        <option value="" disabled>-- Lütfen Bir Ürün Seçin --</option>
        <option v-for="prod in productsList" :key="prod.id" :value="prod.id">
          [{{ prod.urunKodu }}] - {{ prod.urunAdi }}
        </option>
      </select>
      <button class="btn btn-refresh" @click="fetchProducts" :disabled="isLoadingStock">🔄 Listeyi Yenile</button>
    </div>

    <div class="mes-card form-card">
      <h3>➕ Yeni Ürün Ekle</h3>

      <div class="form-group">
        <label>Ürün Kodu *</label>
        <input type="text" v-model="insertForm.urunKodu" placeholder="Örn: PRD-1001" />
      </div>

      <div class="form-group">
        <label>Ürün Adı *</label>
        <input type="text" v-model="insertForm.urunAdi" placeholder="Örn: Paslanmaz Sac 2mm" />
      </div>

      <div class="form-group">
        <label>Ölçü Birimi *</label>
        <select v-model="insertForm.birim">
          <option value="Adet">Adet</option>
          <option value="KG">KG</option>
          <option value="Plaka">Plaka</option>
          <option value="Litre">Litre</option>
          <option value="Metre">Metre</option>
        </select>
      </div>

      <div class="form-group">
        <label>İlk Stok Miktarı *</label>
        <input type="number" v-model.number="insertForm.ilkStokMiktari" placeholder="Örn: 100" />
      </div>

      <div class="form-group">
        <label>Ürün Açıklaması</label>
        <textarea v-model="insertForm.aciklama" rows="3" placeholder="Üretime yönelik teknik detaylar..."></textarea>
      </div>

      <button class="btn btn-save" @click="submitInsert" :disabled="isSubmitting">
        {{ isSubmitting ? '➕ Ekleniyor...' : '➕ Ürünü Ekle' }}
      </button>
    </div>

    <div v-if="selectedProductId && form" class="mes-card form-card">
      <h3>✏️ Ürün Tanımı ve Stok Revizyonu (ID: {{ selectedProductId }})</h3>
      
      <div v-if="isLoadingStock" class="loading-mini">
        <p>🔄 StokApi üzerinden güncel envanter çekiliyor...</p>
      </div>

      <div v-else>
        <div class="form-group">
          <label>Ürün Kodu (Değiştirilemez)</label>
          <input type="text" :value="form.urunKodu" disabled class="input-disabled" />
        </div>

        <div class="form-group">
          <label>Ürün Adı *</label>
          <input type="text" v-model="form.urunAdi" placeholder="Örn: Paslanmaz Sac 2mm" />
        </div>

        <div class="form-group">
          <label>Ölçü Birimi *</label>
          <select v-model="form.birim">
            <option value="Adet">Adet</option>
            <option value="KG">KG</option>
            <option value="Plaka">Plaka</option>
            <option value="Litre">Litre</option>
            <option value="Metre">Metre</option>
          </select>
        </div>

        <div class="form-group">
          <label>Mevcut Stok Miktarı (MES Canlı Veri) *</label>
          <input type="number" v-model.number="form.stokMiktari" placeholder="Örn: 150" />
          <small class="help-text">⚠️ Burada yapacağınız değişiklik her iki veritabanında senkronize güncellenecektir.</small>
        </div>

        <div class="form-group">
          <label>Ürün Açıklaması</label>
          <textarea v-model="form.aciklama" rows="3" placeholder="Üretime yönelik teknik detaylar..."></textarea>
        </div>

        <button class="btn btn-save" @click="submitUpdate" :disabled="isSubmitting">
          {{ isSubmitting ? '💾 Kaydediliyor...' : '💾 Değişiklikleri Senkronize Et' }}
        </button>
      </div>
    </div>

    <div v-if="statusMessage" :class="['status-panel', statusType]">
      {{ statusMessage }}
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'

// API Kök Adresleri (Nginx Gateway üzerinden konuşuyoruz)
const API_BASE = 'http://localhost:8000/api'


// States
const productsList = ref([])
const selectedProductId = ref('')
const isSubmitting = ref(false)
const isLoadingStock = ref(false)
const statusMessage = ref('')
const statusType = ref('')

const form = ref({
  id: 0,
  urunKodu: '',
  urunAdi: '',
  birim: '',
  aciklama: '',
  stokMiktari: 0
})

const insertForm = ref({
  urunKodu: '',
  urunAdi: '',
  birim: 'Adet',
  ilkStokMiktari: 0,
  aciklama: ''
})

// 🚀 1. Ürün listesini çek
const fetchProducts = async () => {
  try {
    statusMessage.value = ''
    const response = await axios.get(`${API_BASE}/urunler`)
    productsList.value = response.data
  } catch (error) {
    showStatus('Ürün listesi çekilirken hata oluştu!', 'error')
  }
}

// 🚀 2. Ürün seçildiğinde detayları doldur ve StokApi'den canlı miktarı talep et
const loadProductDetails = async () => {
  const found = productsList.value.find(p => p.id === selectedProductId.value)
  if (!found) return

  // Önce temel ürün bilgilerini forma bağla
  form.value = {
    id: found.id,
    urunKodu: found.urunKodu,
    urunAdi: found.urunAdi,
    birim: found.birim || 'Adet',
    aciklama: found.aciklama || '',
    stokMiktari: 0 // Stok gelene kadar geçici sıfır
  }

  // Şimdi StokApi sözlüğünden bu ürünün gerçek miktarını sorgula
  isLoadingStock.value = true
  try {
    const stockRes = await axios.get(`${API_BASE}/stoklar`)
    const stocksDictionary = stockRes.data

    console.log("Güncelleme Ekranı İçin Çekilen Stok Sözlüğü:", stocksDictionary)

    // Tip ve harf uyuşmazlığına karşı çift korumalı okuma mantığı
    const pId = found.id
    let canlıMiktar = 0

    if (stocksDictionary[pId] !== undefined) {
      canlıMiktar = stocksDictionary[pId] // Eğer anahtar sayı ise (1)
    } else if (stocksDictionary[String(pId)] !== undefined) {
      canlıMiktar = stocksDictionary[String(pId)] // Eğer anahtar string ise ("1")
    }

    // Formu gerçek canlı stok miktarı ile mühürle!
    form.value.stokMiktari = canlıMiktar

  } catch (error) {
    console.error("Stok miktarı çekilemedi:", error)
    showStatus('⚠️ Ürünün stok bilgisi canlı olarak doğrulanamadı!', 'error')
  } finally {
    isLoadingStock.value = false
  }
}

const submitInsert = async () => {
  if (!insertForm.value.urunKodu || !insertForm.value.urunAdi || !insertForm.value.birim) {
    showStatus('Lütfen ürün kodu, adı ve birimi doldurun.', 'error')
    return
  }

  isSubmitting.value = true
  statusMessage.value = ''

  try {
    const payload = {
      urunKodu: insertForm.value.urunKodu,
      urunAdi: insertForm.value.urunAdi,
      birim: insertForm.value.birim,
      aciklama: insertForm.value.aciklama,
      ilkStokMiktari: insertForm.value.ilkStokMiktari
    }

    const response = await axios.post(`${API_BASE}/urunler/ekle`, payload)
    showStatus(response.data.message || 'Ürün başarıyla eklendi!', 'success')

    insertForm.value = {
      urunKodu: '',
      urunAdi: '',
      birim: 'Adet',
      ilkStokMiktari: 0,
      aciklama: ''
    }

    await fetchProducts()
    window.dispatchEvent(new CustomEvent('mes-data-updated'))
  } catch (error) {
    const errMsg = error.response?.data?.detail || error.response?.data?.message || 'Ürün ekleme başarısız oldu!'
    showStatus(`❌ İşlem başarısız: ${errMsg}`, 'error')
  } finally {
    isSubmitting.value = false
  }
}

// 🚀 3. Formu .NET Core Dağıtık Transaction Altyapısına Gönder
const submitUpdate = async () => {
  if (!form.value.urunAdi || !form.value.birim) {
    showStatus('Lütfen zorunlu alanları (*) doldurun.', 'error')
    return
  }

  isSubmitting.value = true
  statusMessage.value = ''

  try {
    const response = await axios.post(`${API_BASE}/urunler/guncelle`, form.value)
    showStatus(response.data.message || 'Başarıyla güncellendi!', 'success')
    
    // Listeyi ve form verilerini tekrar senkronize et
    await fetchProducts()
    await loadProductDetails()
    window.dispatchEvent(new CustomEvent('mes-data-updated'));

  } catch (error) {
    const errMsg = error.response?.data?.detail || error.response?.data?.message || 'Güncelleme başarısız oldu!'
    showStatus(`❌ Dağıtık İşlem İptal Edildi: ${errMsg}`, 'error')
  } finally {
    isSubmitting.value = false
  }
}

const showStatus = (msg, type) => {
  statusMessage.value = msg
  statusType.value = type
}

onMounted(() => {
  fetchProducts()
})
</script>

<style scoped>
.mes-container {
  max-width: 800px;
  margin: 30px auto;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  color: #333;
}
.mes-title {
  border-bottom: 2px solid #2c3e50;
  padding-bottom: 10px;
  color: #2c3e50;
}
.mes-card {
  background: #f8f9fa;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}
.form-group {
  margin-bottom: 15px;
}
.form-group label {
  display: block;
  font-weight: 600;
  margin-bottom: 5px;
  font-size: 14px;
}
input, select, textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #cbd5e1;
  border-radius: 4px;
  box-sizing: border-box;
}
.input-disabled {
  background: #e2e8f0;
  cursor: not-allowed;
}
.help-text {
  color: #64748b;
  font-size: 11px;
  display: block;
  margin-top: 4px;
}
.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  font-weight: bold;
  cursor: pointer;
  transition: background 0.2s;
}
.btn-refresh {
  background: #3498db;
  color: white;
  margin-left: 10px;
}
.btn-save {
  background: #27ae60;
  color: white;
  width: 100%;
  font-size: 16px;
}
.btn:hover { opacity: 0.9; }
.loading-mini {
  text-align: center;
  padding: 15px;
  color: #2980b9;
  font-weight: bold;
}
.status-panel {
  padding: 15px;
  border-radius: 6px;
  font-weight: bold;
  text-align: center;
}
.success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
.error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
</style>