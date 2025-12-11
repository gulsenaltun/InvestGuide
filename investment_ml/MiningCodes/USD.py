#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Created on Sat Dec  6 23:23:00 2025

@author: yitik
"""

import requests
import xml.etree.ElementTree as ET
from datetime import date, timedelta
import pandas as pd
import time
import urllib3
import sys

# 1. SSL Hatalarını Sustur (Mac/Güvenlik duvarı takılmasın)
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# --- AYARLAR ---
bitis_tarihi = date.today()
# 1000 iş günü verisi için yaklaşık 5 yıl geriye gidiyoruz
baslangic_tarihi = bitis_tarihi - timedelta(days=365 * 5) 

dosya_adi = "tcmb_usd_verisi_1000.xlsx"

# "Ben Google Chrome'um" Başlıkları
headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
    "Referer": "https://www.google.com",
}

veriler = []
mevcut_tarih = baslangic_tarihi
sayac = 0

print(f"🚀 Büyük Veri Operasyonu Başlıyor...")
print(f"📅 Aralık: {baslangic_tarihi.strftime('%d.%m.%Y')} - {bitis_tarihi.strftime('%d.%m.%Y')}")
print(f"🎯 Hedef: Yaklaşık 1000+ satır Forex verisi")
print("-" * 60)

start_time = time.time()

while mevcut_tarih <= bitis_tarihi:
    # Cumartesi (5) ve Pazar (6) günlerini atla
    if mevcut_tarih.weekday() < 5:
        
        yil_ay = mevcut_tarih.strftime("%Y%m")
        gun_dosya = mevcut_tarih.strftime("%d%m%Y")
        url = f"https://www.tcmb.gov.tr/kurlar/{yil_ay}/{gun_dosya}.xml"
        
        try:
            # verify=False: SSL sertifikasını takma
            # timeout=5: Yanıt vermezse 5 saniye sonra pas geç
            response = requests.get(url, headers=headers, verify=False, timeout=5)
            
            if response.status_code == 200:
                root = ET.fromstring(response.content)
                
                bulundu = False
                for currency in root.findall('Currency'):
                    # Sadece USD (ABD DOLARI)
                    if currency.get('CurrencyCode') == "USD":
                        alis = currency.find('ForexBuying').text
                        satis = currency.find('ForexSelling').text
                        
                        # Veriyi sayıya çevirip listeye ekle
                        if alis and satis:
                            veriler.append({
                                "Tarih": mevcut_tarih.strftime("%Y-%m-%d"), # Veritabanı dostu format
                                "Yıl": mevcut_tarih.year,
                                "Ay": mevcut_tarih.month,
                                "Alış (Forex)": float(alis),
                                "Satış (Forex)": float(satis)
                            })
                            sayac += 1
                            bulundu = True
                            
                            # İlerleme Çubuğu (Her 50 veride bir detay yaz, yoksa tek satırda güncelle)
                            sys.stdout.write(f"\r✅ Toplanan Veri: {sayac} | Son Tarih: {mevcut_tarih.strftime('%d.%m.%Y')} | Kur: {alis}")
                            sys.stdout.flush()
                        break
                
                if not bulundu:
                    # Bazen USD olsa da Forex alanı boş olabilir (Nadir)
                    pass
            
            else:
                # 404 vs. (Resmi tatiller)
                pass

        except Exception as e:
            # Bağlantı koparsa durmasın, devam etsin
            pass

    mevcut_tarih += timedelta(days=1)

# --- KAYIT İŞLEMİ ---
print("\n" + "-" * 60)
elapsed_time = time.time() - start_time

if veriler:
    df = pd.DataFrame(veriler)
    df.to_excel(dosya_adi, index=False)
    
    print(f"🎉 GÖREV BAŞARILI!")
    print(f"📂 Kaydedilen Dosya: {dosya_adi}")
    print(f"📊 Toplam Satır Sayısı: {len(df)}")
    print(f"⏱️ Geçen Süre: {elapsed_time:.2f} saniye")
    
    # İlk ve Son 3 veriyi gösterelim
    print("\nÖrnek Veri:")
    print(df.iloc[[0, 1, 2, -3, -2, -1]])
else:
    print("😔 Hiç veri toplanamadı. Bağlantı ayarlarını kontrol et.")