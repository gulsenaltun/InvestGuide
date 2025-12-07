#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Created on Sun Dec  7 12:24:36 2025

@author: yitik
"""
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import Select
from bs4 import BeautifulSoup
import pandas as pd
import time
import os

print("🍏 Safari Tarayıcısı başlatılıyor...")

driver = webdriver.Safari()
driver.maximize_window()

url = "https://altin.doviz.com/gram-altin"
dosya_adi = "gram_altin_tamamen_ham.xlsx"

try:
    print(f"🌐 Siteye gidiliyor: {url}")
    driver.get(url)
    
    wait = WebDriverWait(driver, 20)
    
    # 1. TABLO AÇ
    print("🖱️ 'Tablo' düğmesine tıklanıyor...")
    tablo_tusu = wait.until(EC.element_to_be_clickable((By.XPATH, "//li[contains(text(), 'Tablo')]")))
    driver.execute_script("arguments[0].click();", tablo_tusu)
    time.sleep(2)
    
    # 2. TARİH SEÇ (Son 5 Yıl)
    print("⏳ Tarih 'Son 5 Yıl' yapılıyor...")
    select_element = wait.until(EC.presence_of_element_located((By.CLASS_NAME, "date-ranges")))
    secim_araci = Select(select_element)
    secim_araci.select_by_visible_text("Son 5 Yıl")
    time.sleep(1)
    
    # 3. VERİLERİ GETİR
    print("🖱️ Veriler yükleniyor...")
    getir_butonu = wait.until(EC.element_to_be_clickable((By.CLASS_NAME, "load-historical-data")))
    driver.execute_script("arguments[0].click();", getir_butonu)
    
    print("⏳ Tablonun güncellenmesi bekleniyor (10 saniye)...")
    time.sleep(10) 
    
    # 4. HAM VERİYİ EL İLE (MANUEL) TOPLA
    html_kaynagi = driver.page_source
    soup = BeautifulSoup(html_kaynagi, "html.parser")
    
    print("📥 Tablo metin olarak okunuyor...")
    
    tbody = soup.find("tbody")
    ham_veriler = []
    
    if tbody:
        satirlar = tbody.find_all("tr")
        
        for satir in satirlar:
            sutunlar = satir.find_all("td")
            
            if len(sutunlar) >= 3:
                # KRİTİK NOKTA: str() kullanarak veriyi zorla metin yapıyoruz Çünkü sıkıntı yaratıyo.
                tarih = str(sutunlar[0].text.strip())
                acilis = str(sutunlar[1].text.strip())  
                kapanis = str(sutunlar[2].text.strip()) 
                
                # Listeye ekle (Hepsini string olarak)
                ham_veriler.append({
                    "Tarih": tarih,
                    "Acilis": acilis,
                    "Kapanis": kapanis
                })
        
        # 5. KAYDET
        if ham_veriler:
            # dtype=str diyerek Pandas'ın da çevirmesini engelliyoruz
            df = pd.DataFrame(ham_veriler, dtype=str)
            
            print(f"✅ İŞLEM BAŞARILI! {len(df)} satır veri çekildi.")
            print("-" * 40)
            print(df.head()) 
            print("-" * 40)
            print("Veri tipleri (Hepsi 'object' olmalı):")
            print(df.dtypes)
            
            df.to_excel(dosya_adi, index=False)
            print(f"🎉 Dosya başarıyla kaydedildi: {os.getcwd()}/{dosya_adi}")
            
        else:
            print("⚠️ Tablo bulundu ama içi boş.")
    else:
        print("❌ Hata: Sayfada <tbody> bulunamadı.")

except Exception as e:
    print(f"💥 Hata: {e}")

finally:
    driver.quit()
    print("👋 Tarayıcı kapatıldı.")