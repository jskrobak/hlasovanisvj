// Přihlášení přes POST /login (nastaví cookie). Navigaci po úspěchu řeší volající.
export async function postLoginJson(url, payload) {
    const resp = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
        credentials: "include"
    });

    if (!resp.ok) {
        throw new Error("Login failed");
    }
}
