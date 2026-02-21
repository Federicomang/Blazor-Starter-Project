(() => {
    window.blazorCulture = {
        get: () => {
            let clientLang = localStorage.getItem("blazorCulture");
            if (clientLang) return clientLang;
            let name = window.blazorCulture.cookieName;
            if (!name) {
                name = ".AspNetCore.Culture=";
            }
            let parts = document.cookie.split(";").map(x => x.trim());
            let entry = parts.find(x => x.startsWith(name));
            if (!entry) return null;

            let value = decodeURIComponent(entry.substring(name.length)); // es: c=it-IT|uic=it-IT
            let match = value.match(/c=([^|]+)/);
            return match ? match[1] : null;
        },
        setClientLanguage: (culture) => {
            localStorage.setItem("blazorCulture", culture);
        },
        removeClientLanguage: () => {
            localStorage.removeItem("blazorCulture");
        },
    };

    window.doRequest = async (url, options, as_json=true) => {
        var response = await fetch(url, options);
        var result = as_json ? await response.json() : await response.text();
        return {
            headers: response.headers,
            statusCode: response.status,
            redirected: response.redirected,
            result: result
        }
    };
})();