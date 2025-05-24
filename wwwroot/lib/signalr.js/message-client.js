// wwwroot/js/message-client.js
// SweetAlert2 kütüphanesinin yüklü olduğu varsayılmıştır.

document.addEventListener('DOMContentLoaded', function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/messageHub") // SignalR Hub'ın URL'si
        .withAutomaticReconnect() // Otomatik yeniden bağlanmayı dene
        .build();

    // SignalR bağlantısını başlat
    async function startConnection() {
        try {
            await connection.start();
            console.log("SignalR bağlantısı başarılı!");
            // Bağlantı başarılı olduğunda, okunmamış mesaj sayısını isteyebiliriz
            connection.invoke("UpdateUnreadCount").catch(err => console.error(err.toString()));

        } catch (err) {
            console.error("SignalR bağlantı hatası:", err);
            setTimeout(startConnection, 5000); // 5 saniye sonra tekrar dene
        }
    }

    // Sunucudan "ReceiveNotification" metodu çağrıldığında çalışacak kod
    connection.on("ReceiveNotification", (title, message, type) => {
        Swal.fire({
            title: title,
            text: message,
            icon: type, // 'success', 'error', 'warning', 'info', 'question'
            toast: true,
            position: 'top-end', // Sağ üst köşe
            showConfirmButton: false,
            timer: 5000, // 5 saniye sonra kapanacak
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer)
                toast.addEventListener('mouseleave', Swal.resumeTimer)
            }
        });
    });

    // Sunucudan "UpdateUnreadCount" metodu çağrıldığında çalışacak kod
    connection.on("UpdateUnreadCount", async () => {
        console.log("Okunmamış mesaj sayısı güncelleniyor...");
        try {
            const response = await fetch('/Messages/GetUnreadMessageCount');
            if (response.ok) {
                const count = await response.json();
                const unreadCountElement = document.getElementById('unread-message-count');
                if (unreadCountElement) {
                    if (count > 0) {
                        unreadCountElement.textContent = count;
                        unreadCountElement.style.display = 'inline-block';
                    } else {
                        unreadCountElement.style.display = 'none';
                    }
                }
            }
        } catch (error) {
            console.error("Okunmamış mesaj sayısı alınırken hata oluştu:", error);
        }
    });

    // Sayfa yüklendiğinde bağlantıyı başlat
    startConnection();

    // NOT: Mesaj gönderme formu artık doğrudan controller'a POST ediliyor.
    // SignalR üzerinden mesaj gönderme mantığı buradan kaldırıldı.
    // Eğer bir sohbet uygulaması gibi anlık mesaj göndermek istersen,
    // o zaman bu kısmı tekrar ekleyip Controller'daki veritabanı kayıt mantığını kaldırman gerekebilir.
    // Şu anki senaryoda, Controller mesajı kaydediyor ve Hub sadece bildirim gönderiyor.

    // Mesaj okundu olarak işaretleme fonksiyonu (Gelen Kutusu'nda kullanılabilir)
    window.markMessageAsRead = async function (messageId) {
        try {
            // SignalR üzerinden MarkAsRead metodunu çağır
            await connection.invoke("MarkAsRead", messageId);
            console.log(`Mesaj ${messageId} okundu olarak işaretlendi.`);
            // UI'daki ilgili mesajın durumunu güncelle (optimistik UI güncelleme)
            const messageRow = document.getElementById(`message-row-${messageId}`);
            if (messageRow) {
                messageRow.classList.remove('unread');
                const readStatusSpan = messageRow.querySelector('.read-status');
                if (readStatusSpan) {
                    readStatusSpan.textContent = 'Okundu';
                    readStatusSpan.classList.remove('badge-danger');
                    readStatusSpan.classList.add('badge-success');
                }
            }
        } catch (err) {
            console.error("Mesaj okundu olarak işaretlenirken hata:", err.toString());
        }
    };
});