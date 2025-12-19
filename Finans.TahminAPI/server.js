const express = require('express');
const { spawn } = require('child_process');
const app = express();
const port = 3000;

app.use(express.json());

// Örnek İstek: /api/predict?symbol=USD&currentPrice=34.18
app.get('/api/predict', (req, res) => {
    
    const symbol = req.query.symbol || 'USD';
    const currentPrice = req.query.currentPrice;

    if (!currentPrice) {
        return res.status(400).json({ success: false, error: "Lütfen 'currentPrice' (güncel fiyat) parametresini gönderin." });
    }

    console.log(`Tahmin İsteği Geldi -> Sembol: ${symbol}, Fiyat: ${currentPrice}`);

    // Python'u çalıştır: python3 tahmin.py USD 34.18
    const pythonProcess = spawn('python3', ['tahmin.py', symbol, currentPrice]);

    let resultData = '';
    let errorData = '';

    pythonProcess.stdout.on('data', (data) => {
        resultData += data.toString();
    });

    pythonProcess.stderr.on('data', (data) => {
        errorData += data.toString();
    });

    pythonProcess.on('close', (code) => {
        if (code !== 0) {
            console.error("Python Hatası:", errorData);
            return res.status(500).json({ 
                success: false, 
                error: "Model çalıştırılamadı. Logları kontrol edin.",
                details: errorData 
            });
        }

        try {
            // Gelen veri string olabilir (örn: "34.50\n"), temizle ve sayıya çevir
            const prediction = parseFloat(resultData.trim());
            
            // Yarının tarihini bul
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 1);

            res.json({
                success: true,
                symbol: symbol,
                date: tomorrow.toISOString().split('T')[0], // YYYY-MM-DD
                predicted_price: prediction,
                message: "Tahmin başarıyla oluşturuldu."
            });
        } catch (e) {
            res.status(500).json({ success: false, error: "Python çıktısı okunamadı." });
        }
    });
});

app.listen(port, () => {
    console.log(`🚀 Tahmin API (Node.js) çalışıyor: http://localhost:${port}`);
});