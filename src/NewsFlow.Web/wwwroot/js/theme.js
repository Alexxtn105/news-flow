window.downloadFile = function (fileName, contentType, base64) {
    var bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    var blob = new Blob([bytes], { type: contentType });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.themeManager = {
    get: function () {
        return localStorage.getItem('nf-theme') || 'dark';
    },
    set: function (mode) {
        localStorage.setItem('nf-theme', mode);
        document.documentElement.setAttribute('data-theme', mode);
    }
};
