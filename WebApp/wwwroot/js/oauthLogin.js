window.oauthLogin = {
    async begin() {
        const encode = bytes => btoa(String.fromCharCode(...bytes))
            .replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
        const verifier = encode(crypto.getRandomValues(new Uint8Array(32)));
        const challenge = encode(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier))));
        sessionStorage.setItem("googleLoginVerifier", verifier);
        return challenge;
    },
    takeVerifier() {
        const verifier = sessionStorage.getItem("googleLoginVerifier");
        sessionStorage.removeItem("googleLoginVerifier");
        return verifier;
    }
};
