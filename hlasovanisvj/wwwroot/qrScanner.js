window.qrScanner = {
    start: function (dotNetRef) {
        const qr = new Html5Qrcode("qr-reader");

        qr.start(
            { facingMode: "environment" },
            {
                fps: 10,
                qrbox: 250
            },
            (decodedText) => {
                dotNetRef.invokeMethodAsync("OnQrScanned", decodedText);
                qr.stop();
            },
            (error) => {
                // ignorujeme chyby čtení
            }
        );
    }
};