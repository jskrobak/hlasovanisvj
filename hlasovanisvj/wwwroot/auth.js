export async function postLoginJson(url, payload) {
    const resp = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
        credentials: "include"
    });

    if (resp.redirected) { window.location = resp.url; return; }
    if (resp.ok) { window.location.reload(); }
    else { throw new Error("Login failed"); }
}
