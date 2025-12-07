#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Created on Sat Dec  6 23:30:51 2025

@author: yitik
"""

import requests
import xml.etree.ElementTree as ET
from datetime import date, timedelta
import pandas as pd
import time
import urllib3
import sys

# 1. SSL Hatalarını Sustur
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# --- AYARLAR ---
bitis_tarihi = date.today()
# 1000 iş günü verisi için 5 yıl (365*5) geriye git
baslangic_tarihi = bitis_tarihi - timedelta(days=365 * 5) 

dosya_adi = "tcmb_eur_verisi_1000.xlsx"

# Kamuflaj Başlıkları
headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
    "Referer": "https://www.google.com",
}

veriler = []
mevcut_tarih = baslangic_tarihi
sayac = 0

print(f"🚀 Euro (EUR) Operasyonu Başlıyor...")
print(f"📅 Aralık: {baslangic_tarihi.strftime('%d.%m.%Y')} - {bitis_tarihi.strftime('%d.%m.%Y')}")
print(f"🎯 Hedef: 1000+ satır Forex verisi")
print("-" * 60)

start_time = time.time()

while mevcut_tarih <= bitis_tarihi:
    # Cumartesi (5) ve Pazar (6) günlerini atla
    if mevcut_tarih.weekday() < 5:
        
        yil_ay = mevcut_tarih.strftime("%Y%m")
        gun_dosya = mevcut_tarih.strftime("%d%m%Y")
        url = f"https://www.tcmb.gov.tr/kurlar/{yil_ay}/{gun_dosya}.xml"
        
        try:
            response = requests.get(url, headers=headers, verify=False, timeout=5)
            
            if response.status_code == 200:
                root = ET.fromstring(response.content)
                
                bulundu = False
                for currency in root.findall('Currency'):
                    # --- DEĞİŞİKLİK: SADECE EURO (EUR) ---
                    if currency.get('CurrencyCode') == "EUR":
                        alis = currency.find('ForexBuying').text
                        satis = currency.find('ForexSelling').text
                        
                        if alis and satis:
                            veriler.append({
                                "Tarih": mevcut_tarih.strftime("%Y-%m-%d"),
                                "Yıl": mevcut_tarih.year,
                                "Ay": mevcut_tarih.month,
                                "Birim": "Euro (EUR)",
                                "Alış (Forex)": float(alis),
                                "Satış (Forex)": float(satis)
                            })
                            sayac += 1
                            bulundu = True
                            
                            # İlerleme Göstergesi
                            sys.stdout.write(f"\r✅ Toplanan: {sayac} | Tarih: {mevcut_tarih.strftime('%d.%m.%Y')} | Kur: {alis}")
                            sys.stdout.flush()
                        break
                
            else:
                pass # Tatil vs.

        except Exception as e:
            pass # Bağlantı hatası vs.

    mevcut_tarih += timedelta(days=1)

# --- KAYIT ---
print("\n" + "-" * 60)
elapsed_time = time.time() - start_time

if veriler:
    df = pd.DataFrame(veriler)
    df.to_csv(dosya_adi, index=False)
    
    print(f"🎉 İŞLEM TAMAM!")
    print(f"📂 Kaydedilen Dosya: {dosya_adi}")
    print(f"📊 Toplam Satır: {len(df)}")
    print(f"⏱️ Geçen Süre: {elapsed_time:.2f} saniye")
    print("\nÖrnek Veri:")
    print(df.tail(3))
else:
    print("😔 Hiç veri toplanamadı.")