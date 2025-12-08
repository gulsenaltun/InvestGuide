#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Created on Mon Dec  8 23:34:49 2025

@author: yitik
"""

from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import Select
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import time
import pandas as pd  # CSV kaydı için pandas kullanıyoruz

# Safari Tarayıcısını Başlat
driver = webdriver.Safari()
driver.maximize_window()

url = "https://www.doviz.com/kripto-paralar/bitcoin/tarihsel-veri"
driver.get(url)

try:
    wait = WebDriverWait(driver, 15)
    
    # 1. "Son 5 Yıl" Seçimi (Value: 1607374800)
    print("Site açıldı, 'Son 5 Yıl' seçiliyor...")
    select_element = wait.until(EC.presence_of_element_located((By.CLASS_NAME, "date-ranges")))
    select = Select(select_element)
    select.select_by_value("1607374800")

    # 2. "Verileri Getir" Butonuna Tıkla
    fetch_button = driver.find_element(By.CLASS_NAME, "load-historical-data")
    fetch_button.click()
    print("Veri getirme butonuna basıldı.")

    # 3. Tablo Yüklenene Kadar Bekle
    print("Tablo güncelleniyor, bekleniyor...")
    time.sleep(5) 

    # 4. Veriyi Ham String Olarak Çek
    rows = driver.find_elements(By.CSS_SELECTOR, ".value-table tbody tr")
    
    ham_veri_listesi = []
    
    for row in rows:
        cols = row.find_elements(By.TAG_NAME, "td")
        if len(cols) >= 2:
            # Sitedeki metni olduğu gibi alıyoruz
            tarih_str = cols[0].text 
            deger_str = cols[1].text
            
            ham_veri_listesi.append({
                "Tarih": tarih_str, 
                "Deger": deger_str
            })

    # 5. CSV Olarak Kaydet
    df = pd.DataFrame(ham_veri_listesi)
    
    # encoding='utf-8-sig' kullanıyoruz ki Excel'de açarsan Türkçe karakterler (ı, ş, ğ) bozuk görünmesin.
    dosya_adi = "bitcoin_tarihsel_ham.csv"
    df.to_csv(dosya_adi, index=False, encoding="utf-8-sig")
    
    print(f"\nİşlem tamamlandı! {len(df)} satır veri '{dosya_adi}' dosyasına kaydedildi.")
    print("İlk 5 satır önizleme:")
    print(df.head())

except Exception as e:
    print(f"Hata oluştu: {e}")

finally:
    driver.quit()