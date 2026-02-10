(() => {
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