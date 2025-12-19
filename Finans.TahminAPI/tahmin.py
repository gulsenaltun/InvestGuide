import sys
import joblib
import pandas as pd
import datetime
import os
import warnings

# Uyarıları sustur
warnings.filterwarnings("ignore")

# Parametre kontrolü
if len(sys.argv) < 3:
    print("Hata: Eksik parametre.")
    sys.exit(1)

sembol = sys.argv[1].upper()
try:
    guncel_fiyat = float(sys.argv[2])
except:
    print("Hata: Fiyat sayı olmalı.")
    sys.exit(1)

# Tarih Hesapla
bugun = datetime.date.today()
yarin = bugun + datetime.timedelta(days=1)
yil = yarin.year
ay = yarin.month
gun = yarin.day
haftanin_gunu = yarin.weekday()

data = {}

# --- KRİTİK DÜZELTME BURADA ---
# USD ve EUR modelleri eğitimde "Yıl" (Türkçe ı) kullanmış.
if sembol == 'USD' or sembol == 'EUR':
    data = {
        'Yıl': [yil],          # DÜZELTİLDİ: Yil -> Yıl
        'Ay': [ay],
        'Gun': [gun],
        'Onceki_Gun_Fiyat': [guncel_fiyat],
        'Haftalik_Ortalama': [guncel_fiyat] 
    }
    model_file = f'{sembol}_Model.pkl'

# BTC ve XAU modelleri eğitimde "Yil" (İngilizce i) kullanmış.
elif sembol == 'BTC':
    data = {
        'Yil': [yil],          # Burası İngilizce kalıyor
        'Ay': [ay],
        'Gun': [gun],
        'HaftaninGunu': [haftanin_gunu],
        'Dunku_Deger': [guncel_fiyat]
    }
    model_file = 'BTC_Model.pkl'

elif sembol in ['XAU', 'GOLD', 'GA']:
    data = {
        'Acilis': [guncel_fiyat],
        'Yil': [yil],          
        'Ay': [ay],
        'Gun': [gun],
        'HaftaninGunu': [haftanin_gunu],
        'Dunku_Kapanis': [guncel_fiyat]
    }
    model_file = 'XAU_Model.pkl'

else:
    print(f"Hata: {sembol} bilinmiyor.")
    sys.exit(1)

# Tahmin Et
try:
    df_input = pd.DataFrame(data)
    model_path = os.path.join(os.path.dirname(__file__), model_file)
    
    # Model yükle
    model = joblib.load(model_path)
    
    # Tahmin
    sonuc = model.predict(df_input)
    print(round(sonuc[0], 2))

except Exception as e:
    print(f"Python Hatası: {str(e)}")
    sys.exit(1)