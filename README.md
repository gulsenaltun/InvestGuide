# InvestGuide

# 📈 Multi-Asset Price Prediction with Machine Learning

Bu proje; Dolar (USD), Euro, Altın (XAU) ve Bitcoin (BTC) fiyatlarını geçmiş verilere dayanarak tahmin eden uçtan uca bir makine öğrenmesi boru hattıdır (pipeline).

## 🚀 Öne Çıkan Özellikler
- **Web Scraping:** Selenium ve Requests kullanılarak Merkez Bankası ve döviz.com gibi farklı kaynaklardan 5 yıllık dinamik veri çekimi.
- **Data Pipeline:** `IterativeImputer` ile veri sızıntısı (data leakage) olmadan eksik veri tamamlama.
- **Feature Engineering:** Gecikmeli veriler (lag features), hareketli ortalamalar ve zaman serisi analizi.
- **Model Optimization:** GridSearch kullanılarak hiper-parametre optimizasyonu yapılmış 4 farklı regresyon modeli.

## 🛠️ Teknoloji Yığını
- **Dil:** Python
- **Kütüphaneler:** Scikit-learn, Pandas, NumPy, Selenium, Matplotlib, Seaborn
- **Modelleme:** Linear Regression, Ridge, Bayesian Ridge

## 📊 Sonuçlar
Model, test verisi üzerinde yüksek doğruluk oranlarına ulaşmıştır:
- **Altın (XAU):** %99 R² Score
- **Bitcoin (BTC):** %95 R² Score
- **Dolar/Euro:** %97-98 R² Score

# 💰 SmartInvest: Full-Stack Portfolio Management System

Yatırımcıların varlıklarını takip edebildiği, toplulukla etkileşime geçebildiği ve ML destekli fiyat tahminleri alabildiği bir web uygulamasıdır.

## 🏗️ Mimari Yapı
- **Backend:** C# / ASP.NET MVC
- **Service Architecture:** SOAP (User Services, Forgot Password) & gRPC entegrasyonu.
- **Database:** Hybrid Architecture (MS SQL Server for Relational data, MongoDB for Logs/Forum).

## 🔌 Entegrasyon Akışı
1. Kullanıcı ASP.NET arayüzünden bir tahmin isteğinde bulunur.
2. ASP.NET, bu isteği Node.js API katmanına iletir.
3. Node.js, ilgili parametrelerle Python ML modelini tetikler.
4. Python modelinden dönen tahmin sonucu (JSON), Node.js üzerinden ASP.NET'e ve oradan kullanıcıya sunulur.

## 🌟 Ana Özellikler
- **Gerçek Zamanlı Takip:** Döviz ve altın fiyatlarının otomatik güncellenmesi.
- **Forum Sistemi:** Kullanıcılar arası etkileşim ve yatırım tartışmaları.
- **Tahmin Modülü:** Eğitilen ML modelleri sayesinde gelecek günlerin fiyat öngörüleri.

## 🔧 Proje Yapısı
- `/WebUI`: ASP.NET MVC kullanıcı arayüzü.
- `/ML_API`: Modelin servis edildiği Flask API.
- `/Services`: SOAP tabanlı iletişim servisleri.

## ⚙️ Kurulum ve Çalıştırma

Projeyi yerel ortamınızda çalıştırmak için aşağıdaki adımları sırasıyla izleyin.

### 1. Gerekli Paketlerin Kurulumu
Öncelikle proje dizinindeyken gerekli Python, Node.js ve .NET bağımlılıklarını yükleyin:

# Python kütüphanelerini yükleyin
pip install -r requirements.txt

# Node.js modüllerini yükleyin
npm install

cd Finans.GrpcServer
dotnet run

cd PasswordRecovery.Mac
dotnet run

cd FinansUygulmasi
dotnet run

Tüm servisler "Now listening on..." mesajını verdikten sonra tarayıcınızdan uygulamaya erişebilirsiniz.


